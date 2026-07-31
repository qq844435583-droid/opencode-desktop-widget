using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Media;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OpenCodeDesktopWidget;

internal sealed class MainForm : Form
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ConfigStore _config = new();
    private readonly LicenseService _license = new();
    private readonly StoreConfig _store = StoreConfig.Load();
    private AutoLicenseClient? _licenseClient;
    private readonly OpenCodeClient _client = new();
    private readonly ScrapeService _scrapeService = new();
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly NotifyIcon _trayIcon = new();
    private readonly ContextMenuStrip _trayMenu = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly System.Windows.Forms.Timer _dockPollTimer = new() { Interval = 80 };
    private readonly System.Windows.Forms.Timer _dockHideTimer = new();
    private readonly System.Windows.Forms.Timer _moveTimer = new() { Interval = 220 };
    private readonly System.Windows.Forms.Timer _licenseTimer = new() { Interval = 60_000 };

    private UsageData? _lastUsage;
    private Task<object>? _refreshingTask;
    private LoginForm? _loginForm;
    private bool _quitting;
    private bool _internalBoundsChange;
    private bool _dockHidden;
    private bool _rendererPointerInside;
    private bool _modalDialogOpen;
    private int _compactModelRows = 1;
    private long _nextRefreshAt;
    private DateTimeOffset _nextLicenseRefreshAttempt = DateTimeOffset.MinValue;

    public MainForm()
    {
        AppText.SetLanguage(_config.Data.Settings.Language);
        Text = AppConstants.AppName;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = AppConstants.WindowBackgroundColor;
        if (File.Exists(AppConstants.IconPath)) Icon = new Icon(AppConstants.IconPath);

        if (!_license.Current.IsPro) _config.SetCompact(true);
        if (_store.HasLicenseServer) _licenseClient = new AutoLicenseClient(_store.LicenseServerUrl);

        _lastUsage = _config.Data.ActiveAccountId is { } id && _config.Data.Cache.TryGetValue(id, out var cached)
            ? ApplyModelStatuses(cached)
            : null;

        Controls.Add(_webView);
        Bounds = VisibleBounds();
        ApplyWindowShape();
        InitializeTray();
        InitializeTimers();

        Shown += async (_, _) =>
        {
            // DeviceDpi is reliable only after the native window handle is created.
            // Resize the host in physical pixels so its client area still equals the
            // CSS/DIP dimensions expected by WebView2.
            ApplyWidgetSettings();
            try { await InitializeWebViewAsync(); }
            catch (Exception error)
            {
                MessageBox.Show(this, AppText.T($"WebView2 初始化失败：{error.Message}", $"WebView2 initialization failed: {error.Message}"), AppConstants.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitApplication();
            }
        };
        Move += (_, _) => QueuePlacementSave();
        Resize += (_, _) => ApplyWindowShape();
        DpiChanged += (_, _) => BeginInvoke((Action)ApplyWidgetSettings);
        Deactivate += (_, _) => QueueDockHide(450);
        FormClosing += OnFormClosing;
        FormClosed += (_, _) => DisposeResources();
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.ShowMessage)
        {
            ShowWidget();
            return;
        }
        base.WndProc(ref message);
    }

    private async Task InitializeWebViewAsync()
    {
        var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(AppConstants.WebViewDataDirectory, "widget"));
        _webView.DefaultBackgroundColor = AppConstants.WindowBackgroundColor;
        await _webView.EnsureCoreWebView2Async(environment);
        _webView.ZoomFactor = 1.0;
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app.local",
            AppConstants.ExecutableDirectory,
            CoreWebView2HostResourceAccessKind.Allow);
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _webView.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                NativeMethods.OpenExternal(args.Uri);
        };
        _webView.CoreWebView2.NavigationCompleted += OnInitialNavigationCompleted;
        _webView.CoreWebView2.Navigate("https://app.local/src/renderer/index.html");
    }

    private async void OnInitialNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        _webView.CoreWebView2.NavigationCompleted -= OnInitialNavigationCompleted;
        if (!args.IsSuccess)
        {
            MessageBox.Show(this, AppText.T($"界面加载失败：{args.WebErrorStatus}", $"The interface failed to load: {args.WebErrorStatus}"), AppConstants.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        ApplyLaunchAtLogin(_config.Data.Settings.LaunchAtLogin);
        ApplyWidgetSettings();
        ScheduleRefresh();
        QueueDockHide(1200);
        _ = RefreshLicenseSessionAsync(false);
        if (_config.ActiveAccount() is not null)
        {
            await Task.Delay(800);
            _ = RefreshUsageAsync(true);
        }
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs eventArgs)
    {
        string? requestId = null;
        var isRequest = false;
        try
        {
            using var document = JsonDocument.Parse(eventArgs.WebMessageAsJson);
            var root = document.RootElement;
            var kind = root.TryGetProperty("kind", out var kindElement) ? kindElement.GetString() : "";
            isRequest = kind == "request";
            requestId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            var method = root.TryGetProperty("method", out var methodElement) ? methodElement.GetString() ?? "" : "";
            var arguments = root.TryGetProperty("args", out var argsElement) ? argsElement.Clone() : default;
            var result = await DispatchAsync(method, arguments);
            if (isRequest && requestId is not null) SendResponse(requestId, true, result, null);
        }
        catch (Exception error)
        {
            if (isRequest && requestId is not null) SendResponse(requestId, false, null, error.Message);
        }
    }

    private async Task<object?> DispatchAsync(string method, JsonElement arguments)
    {
        switch (method)
        {
            case "app:bootstrap":
                return PublicBootstrap();
            case "usage:refresh":
            {
                var result = await RefreshUsageAsync(false);
                ScheduleRefresh();
                return result;
            }
            case "account:login":
                OpenLogin();
                return new OkResult();
            case "account:save":
            {
                var input = GetArgument(arguments, 0).Deserialize<AccountInput>(JsonOptions) ?? new AccountInput();
                var account = _config.UpsertAccount(input);
                RebuildTrayMenu();
                return new AccountSaveResult { Account = account, State = PublicBootstrap() };
            }
            case "account:delete":
            {
                var id = GetArgument(arguments, 0).GetString() ?? "";
                _config.DeleteAccount(id);
                _lastUsage = _config.Data.ActiveAccountId is { } activeId && _config.Data.Cache.TryGetValue(activeId, out var cached)
                    ? ApplyModelStatuses(cached)
                    : null;
                RebuildTrayMenu();
                return new StateOnlyResult { State = PublicBootstrap() };
            }
            case "account:switch":
            {
                var id = GetArgument(arguments, 0).GetString() ?? "";
                _config.SetActive(id);
                _lastUsage = _config.Data.Cache.TryGetValue(id, out var cached) ? ApplyModelStatuses(cached) : null;
                RebuildTrayMenu();
                _ = RefreshAfterAccountSwitchAsync();
                return new StateOnlyResult { State = PublicBootstrap() };
            }
            case "settings:update":
            {
                var settings = _config.UpdateSettings(GetArgument(arguments, 0));
                AppText.SetLanguage(settings.Language);
                ApplyLaunchAtLogin(settings.LaunchAtLogin);
                if (_lastUsage is not null)
                {
                    ApplyModelStatuses(_lastUsage);
                    if (_config.ActiveAccount() is { } account) _config.SetCache(account.Id, _lastUsage);
                    AlertNgModels(_lastUsage);
                    SendEvent("usage:updated", new UsageUpdatePayload { Ok = true, Data = _lastUsage, NextRefreshAt = _nextRefreshAt });
                }
                _dockHidden = false;
                ScheduleRefresh();
                ApplyWidgetSettings();
                if (settings.EdgeHide) QueueDockHide();
                return new SettingsUpdateResult { Settings = settings, NextRefreshAt = _nextRefreshAt };
            }
            case "widget:toggle-compact":
            {
                var forceElement = GetArgument(arguments, 0);
                bool? force = forceElement.ValueKind is JsonValueKind.True or JsonValueKind.False ? forceElement.GetBoolean() : null;
                var requestedCompact = force ?? !_config.Data.Settings.Compact;
                if (!requestedCompact && !_license.Current.IsPro)
                {
                    OpenLicenseDialog();
                    if (!_license.Current.IsPro)
                        return new ToggleCompactResult { Ok = false, Locked = true, Compact = true, License = _license.Current };
                }
                return new ToggleCompactResult { Compact = ToggleCompact(requestedCompact), License = _license.Current };
            }
            case "license:manage":
                return OpenLicenseDialog();
            case "license:status":
                return new LicenseStatusResult { License = _license.Current, Compact = _config.Data.Settings.Compact };
            case "widget:compact-model-count":
                SetCompactModelRows(GetArgument(arguments, 0));
                return null;
            case "widget:hover":
                SetRendererHover(GetArgument(arguments, 0));
                return null;
            case "window:drag":
                if (!_config.Data.Settings.ClickThrough) NativeMethods.BeginWindowDrag(Handle);
                return null;
            case "window:close":
                Hide();
                return null;
            case "workspace:open":
                OpenWorkspace();
                return new OkResult();
            case "usage:export-csv":
                return ExportCsv();
            case "config:open-folder":
                Directory.CreateDirectory(AppConstants.AppDataDirectory);
                NativeMethods.OpenExternal(AppConstants.AppDataDirectory);
                return new OkResult();
            default:
                throw new InvalidOperationException(AppText.T("未知宿主调用：", "Unknown host call: ") + method);
        }
    }

    private async Task RefreshAfterAccountSwitchAsync()
    {
        await RefreshUsageAsync(true);
        if (!IsDisposed) ScheduleRefresh();
    }

    private BootstrapState PublicBootstrap()
    {
        return new BootstrapState
        {
            ActiveAccountId = _config.Data.ActiveAccountId,
            Accounts = _config.Data.Accounts.Select(account => new PublicAccount
            {
                Id = account.Id,
                Name = account.Name,
                WorkspaceId = account.WorkspaceId,
                HasAuth = !string.IsNullOrWhiteSpace(account.AuthSecret),
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
            }).ToList(),
            Settings = _config.Data.Settings,
            License = _license.Current,
            Store = new StoreInfo
            {
                ProductName = _store.ProductName,
                PurchaseUrlConfigured = _store.HasPurchaseUrl
            },
            Cache = _config.Data.Cache,
            Usage = _lastUsage ?? (_config.Data.ActiveAccountId is { } id && _config.Data.Cache.TryGetValue(id, out var cached) ? cached : null),
            Platform = "win32-webview2",
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "3.0.0",
            NextRefreshAt = _nextRefreshAt
        };
    }

    private static JsonElement GetArgument(JsonElement arguments, int index)
    {
        if (arguments.ValueKind != JsonValueKind.Array) return default;
        var current = 0;
        foreach (var item in arguments.EnumerateArray())
        {
            if (current++ == index) return item;
        }
        return default;
    }

    private void SendResponse(string id, bool success, object? result, string? error)
    {
        if (_webView.CoreWebView2 is null) return;
        var envelope = new HostResponseEnvelope { Id = id, Success = success, Result = result, Error = error };
        _webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(envelope, JsonOptions));
    }

    private void SendEvent(string eventName, object? payload)
    {
        if (_webView.CoreWebView2 is null) return;
        var envelope = new HostEventEnvelope { Event = eventName, Payload = payload };
        _webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(envelope, JsonOptions));
    }

    private void InitializeTimers()
    {
        _refreshTimer.Tick += async (_, _) =>
        {
            _refreshTimer.Stop();
            await RefreshUsageAsync(true);
            ScheduleRefresh();
        };
        _dockPollTimer.Tick += (_, _) => UpdateDockPointerState();
        _dockPollTimer.Start();
        _dockHideTimer.Tick += (_, _) =>
        {
            _dockHideTimer.Stop();
            HideDock();
        };
        _moveTimer.Tick += (_, _) =>
        {
            _moveTimer.Stop();
            PersistWindowPlacement();
        };
        _licenseTimer.Tick += async (_, _) =>
        {
            EnforceLicenseMode();
            if (DateTimeOffset.UtcNow >= _nextLicenseRefreshAttempt) await RefreshLicenseSessionAsync(false);
        };
        _licenseTimer.Start();
    }


    private async Task RefreshLicenseSessionAsync(bool userInitiated)
    {
        if (_licenseClient is null || (!_license.HasEntitlement && string.IsNullOrWhiteSpace(_license.SessionToken))) return;
        _nextLicenseRefreshAttempt = DateTimeOffset.UtcNow.AddHours(6);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            SessionResult result;
            if (!string.IsNullOrWhiteSpace(_license.SessionToken))
                result = await _licenseClient.RefreshAsync(_license.SessionToken, timeout.Token);
            else if (!string.IsNullOrWhiteSpace(_license.EntitlementKey))
                result = await _licenseClient.ActivateAsync(_license.EntitlementKey, _license.DeviceCode, _license.MachineFingerprint, timeout.Token);
            else return;

            _license.InstallSession(result.SessionToken);
            var status = _license.Current;
            if (status.IsPro && _config.Data.Settings.Compact && userInitiated) _config.SetCompact(false);
            ApplyWidgetSettings();
            SendEvent("license:state", new LicenseStatePayload { License = status, Compact = _config.Data.Settings.Compact, Settings = _config.Data.Settings });
        }
        catch (UpgradeRequiredException error)
        {
            _nextLicenseRefreshAttempt = DateTimeOffset.UtcNow.AddHours(1);
            ShowBalloon(AppText.T("需要升级", "Update required"),
                AppText.T($"最低支持版本为 {error.MinVersion ?? "未知"}。", $"The minimum supported version is {error.MinVersion ?? "unknown"}."), ToolTipIcon.Warning);
        }
        catch (LicenseServerException error) when ((error.Status ?? "").ToLowerInvariant() is "revoked" or "refunded" or "expired" or "deviceremoved" or "unknownlicense")
        {
            _license.ClearSession();
            _config.SetCompact(true);
            ApplyWidgetSettings();
            var status = _license.Current;
            SendEvent("license:state", new LicenseStatePayload { License = status, Compact = true, Settings = _config.Data.Settings });
            ShowBalloon(AppText.T("授权已停用", "License disabled"), AppText.T("授权已被吊销、退款或本机已解绑。", "The license was revoked, refunded, or this device was removed."), ToolTipIcon.Warning);
        }
        catch
        {
            // A network outage does not revoke Pro. The locally signed token remains valid through its grace period.
            _nextLicenseRefreshAttempt = DateTimeOffset.UtcNow.AddHours(_license.Current.NeedsRefresh ? 1 : 6);
        }
        finally
        {
            EnforceLicenseMode();
        }
    }

    private void ScheduleRefresh()
    {
        _refreshTimer.Stop();
        var seconds = Math.Clamp(_config.Data.Settings.RefreshSeconds, AppConstants.MinRefreshSeconds, AppConstants.MaxRefreshSeconds);
        _nextRefreshAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + seconds * 1000L;
        _refreshTimer.Interval = seconds * 1000;
        _refreshTimer.Start();
        SendEvent("widget:state", new WidgetStatePayload { Settings = _config.Data.Settings, NextRefreshAt = _nextRefreshAt });
    }

    private Task<object> RefreshUsageAsync(bool silent)
    {
        if (_refreshingTask is not null) return _refreshingTask;
        _refreshingTask = RefreshUsageCoreAsync(silent);
        return AwaitAndClearAsync(_refreshingTask);
    }

    private async Task<object> AwaitAndClearAsync(Task<object> task)
    {
        try { return await task; }
        finally { if (ReferenceEquals(_refreshingTask, task)) _refreshingTask = null; }
    }

    private async Task<object> RefreshUsageCoreAsync(bool silent)
    {
        AccountCredentials? account;
        try { account = _config.Credentials(); }
        catch (Exception error)
        {
            var invalid = new UsageUpdatePayload { Ok = false, Error = error.Message, Code = "CREDENTIAL_ERROR", NextRefreshAt = _nextRefreshAt };
            if (!silent) SendEvent("usage:updated", invalid);
            return invalid;
        }

        if (account is null)
        {
            var noAccount = new UsageUpdatePayload { Ok = false, Error = AppText.T("请先登录 OpenCode。", "Please sign in to OpenCode first."), Code = "NO_ACCOUNT", NextRefreshAt = _nextRefreshAt };
            if (!silent) SendEvent("usage:updated", noAccount);
            return noAccount;
        }

        SendEvent("usage:updated", new UsageUpdatePayload { Ok = true, Loading = true, AccountId = account.Id, NextRefreshAt = _nextRefreshAt });
        UsageData data;
        try
        {
            data = await _client.FetchUsageAsync(account);
        }
        catch (Exception apiError)
        {
            try
            {
                data = await _scrapeService.ScrapeAsync(account);
                data.FallbackReason = apiError.Message;
            }
            catch (Exception fallbackError)
            {
                var message = fallbackError.Message.Contains("登录", StringComparison.OrdinalIgnoreCase) ||
                              fallbackError.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                              fallbackError.Message.Contains("sign in", StringComparison.OrdinalIgnoreCase)
                    ? fallbackError.Message
                    : AppText.T($"{apiError.Message}；网页备用通道也失败：{fallbackError.Message}", $"{apiError.Message}; the web fallback also failed: {fallbackError.Message}");
                var failed = new UsageUpdatePayload { Ok = false, Error = message, Code = "REFRESH_FAILED", At = DateTime.UtcNow.ToString("O"), NextRefreshAt = _nextRefreshAt };
                SendEvent("usage:updated", failed);
                return failed;
            }
        }

        data.Records = data.Records.Take(50).ToList();
        ApplyModelStatuses(data);
        _lastUsage = data;
        _config.SetCache(account.Id, data);
        MaybeNotify(data);
        AlertNgModels(data);
        var result = new UsageUpdatePayload { Ok = true, Data = data, NextRefreshAt = _nextRefreshAt };
        SendEvent("usage:updated", result);
        return result;
    }

    private UsageData ApplyModelStatuses(UsageData data)
    {
        foreach (var record in data.Records)
            record.ModelStatus = ModelRules.Classify(record.Model, _config.Data.Settings.ModelOkRules, _config.Data.Settings.ModelNgRules);
        return data;
    }

    private void MaybeNotify(UsageData data)
    {
        var settings = _config.Data.Settings;
        if (!settings.Notifications) return;
        var candidates = new[]
        {
            (Key: "rolling", Label: AppText.T("滚动额度", "Rolling limit"), Value: data.Summary.Rolling?.RemainingPercent),
            (Key: "weekly", Label: AppText.T("每周额度", "Weekly limit"), Value: data.Summary.Weekly?.RemainingPercent),
            (Key: "monthly", Label: AppText.T("每月额度", "Monthly limit"), Value: data.Summary.Monthly?.RemainingPercent)
        };
        var low = candidates.Where(item => item.Value.HasValue && item.Value.Value <= settings.WarningThreshold)
            .OrderBy(item => item.Value).FirstOrDefault();
        if (low.Value is null) return;
        ShowBalloon(
            AppText.T($"{low.Label}仅剩 {Math.Round(low.Value.Value)}%", $"{low.Label}: {Math.Round(low.Value.Value)}% remaining"),
            AppText.T("OpenCode 用量接近设定阈值。", "OpenCode usage is approaching the configured threshold."),
            ToolTipIcon.Warning);
    }

    private List<UsageRecord> AlertNgModels(UsageData data)
    {
        var account = _config.ActiveAccount();
        var settings = _config.Data.Settings;
        if (account is null) return [];
        var signature = ModelRules.Signature([], settings.ModelNgRules);
        if (!settings.NgAlertEnabled)
        {
            _config.SetModelAlertState(account.Id, new ModelAlertState { RuleSignature = signature });
            return [];
        }
        var previous = _config.GetModelAlertState(account.Id);
        var known = previous.RuleSignature == signature ? previous.Keys : [];
        var result = ModelRules.FindNewNgRecords(data.Records, settings.ModelOkRules, settings.ModelNgRules, known);
        _config.SetModelAlertState(account.Id, new ModelAlertState { RuleSignature = signature, Keys = result.AlertedKeys });
        if (result.NewRecords.Count == 0) return result.NewRecords;

        var models = result.NewRecords.Select(item => string.IsNullOrWhiteSpace(item.Model) ? "unknown" : item.Model).Distinct().ToList();
        var shown = models.Take(3).ToList();
        var extra = models.Count > shown.Count
            ? AppText.T($" 等 {models.Count} 个模型", $" and {models.Count - shown.Count} more")
            : "";
        SystemSounds.Exclamation.Play();
        ShowBalloon(
            result.NewRecords.Count > 1
                ? AppText.T($"检测到 NG 模型（{result.NewRecords.Count} 条）", $"NG models detected ({result.NewRecords.Count} records)")
                : AppText.T("检测到 NG 模型", "NG model detected"),
            string.Join(AppText.IsEnglish ? ", " : "、", shown) + extra,
            ToolTipIcon.Error);
        RevealDock(false);
        Show();
        NativeMethods.FlashWindow(Handle, true);
        var flashTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        flashTimer.Tick += (_, _) =>
        {
            flashTimer.Stop();
            NativeMethods.FlashWindow(Handle, false);
            flashTimer.Dispose();
        };
        flashTimer.Start();
        SendEvent("model:alert", new ModelAlertPayload { Count = result.NewRecords.Count, Models = models, Records = result.NewRecords.Take(5).ToList() });
        return result.NewRecords;
    }

    private void OpenLogin()
    {
        if (_loginForm is { IsDisposed: false })
        {
            _loginForm.Show();
            _loginForm.Activate();
            return;
        }

        _loginForm = new LoginForm(input =>
        {
            try
            {
                var existing = _config.Data.Accounts.FirstOrDefault(item => item.WorkspaceId == input.WorkspaceId);
                if (existing is not null)
                {
                    input.Id = existing.Id;
                    input.Name = existing.Name;
                }
                else input.Name = _config.Data.Accounts.Count == 0 ? "Main" : $"Account {_config.Data.Accounts.Count + 1}";
                var account = _config.UpsertAccount(input);
                RebuildTrayMenu();
                SendEvent("account:login-state", new LoginStatePayload { Account = account });
                _ = RefreshAfterLoginAsync();
                return Task.CompletedTask;
            }
            catch (Exception error)
            {
                SendEvent("account:login-state", new LoginStatePayload { Ok = false, Error = error.Message });
                throw;
            }
        });
        _loginForm.FormClosed += (_, _) => _loginForm = null;
        _loginForm.Show(this);
        SendEvent("account:login-state", new LoginStatePayload { Started = true });
    }

    private async Task RefreshAfterLoginAsync()
    {
        await RefreshUsageAsync(false);
        if (!IsDisposed) ScheduleRefresh();
    }

    private void OpenWorkspace()
    {
        var account = _config.ActiveAccount();
        var url = account is null ? AppConstants.BaseUrl : $"{AppConstants.BaseUrl}/workspace/{account.WorkspaceId}/go";
        NativeMethods.OpenExternal(url);
    }

    private object ExportCsv()
    {
        if (_lastUsage?.Records.Count is not > 0) throw new InvalidOperationException(AppText.T("当前没有可导出的使用记录。", "There are no usage records to export."));
        using var dialog = new SaveFileDialog
        {
            Title = AppText.T("导出使用记录", "Export usage records"),
            FileName = $"OpenCode-usage-{DateTime.Now:yyyy-MM-dd}.csv",
            Filter = AppText.T("CSV 文件 (*.csv)|*.csv", "CSV files (*.csv)|*.csv"),
            AddExtension = true,
            DefaultExt = "csv"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return new ExportResult { Canceled = true };
        var rows = new List<string[]>
        {
            AppText.IsEnglish
                ? new[] { "Time", "Model", "Provider", "Input tokens", "Output tokens", "Reasoning tokens", "Cache tokens", "Cost", "Session" }
                : new[] { "时间", "模型", "服务商", "输入 Token", "输出 Token", "推理 Token", "缓存 Token", "成本", "会话" }
        };
        rows.AddRange(_lastUsage.Records.Select(item => new[]
        {
            item.Time, item.Model, item.Provider, item.InputTokens.ToString(), item.OutputTokens.ToString(),
            item.ReasoningTokens.ToString(), (item.CacheReadTokens + item.CacheWriteTokens).ToString(), item.Cost.ToString(), item.Session
        }));
        var csv = "\ufeff" + string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row.Select(CsvEscape))));
        File.WriteAllText(dialog.FileName, csv, new UTF8Encoding(true));
        return new ExportResult { FilePath = dialog.FileName };
    }

    private static string CsvEscape(string? value)
    {
        var text = value ?? "";
        return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }

    private void InitializeTray()
    {
        _trayIcon.Text = AppConstants.AppName;
        _trayIcon.Visible = true;
        if (File.Exists(AppConstants.IconPath)) _trayIcon.Icon = new Icon(AppConstants.IconPath);
        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left) ToggleWidget();
        };
        _trayMenu.Opening += (_, _) => RebuildTrayMenu();
        RebuildTrayMenu();
    }

    private void RebuildTrayMenu()
    {
        _trayMenu.Items.Clear();
        var active = _config.ActiveAccount();
        var settings = _config.Data.Settings;
        var license = _license.Current;
        var editionText = license.IsPro
            ? AppText.T("专业版已激活", "Pro activated")
            : AppText.T("免费版 · 仅紧凑窗口", "Free · compact only");
        _trayMenu.Items.Add(new ToolStripMenuItem($"{editionText} · {(active is null ? AppText.T("未连接 OpenCode", "OpenCode not connected") : active.Name)}") { Enabled = false });
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(new ToolStripMenuItem(Visible ? AppText.T("隐藏挂件", "Hide widget") : AppText.T("显示挂件", "Show widget"), null, (_, _) => ToggleWidget()));
        if (license.IsPro)
            _trayMenu.Items.Add(new ToolStripMenuItem(settings.Compact ? AppText.T("展开挂件", "Expand widget") : AppText.T("收起挂件", "Collapse widget"), null, (_, _) => ToggleCompact(null)));
        else
            _trayMenu.Items.Add(new ToolStripMenuItem(AppText.T("解锁展开模式...", "Unlock expanded mode..."), null, (_, _) => OpenLicenseDialog()));
        _trayMenu.Items.Add(new ToolStripMenuItem(license.IsPro ? AppText.T("查看 / 管理授权...", "View / manage license...") : AppText.T("购买 / 输入授权码...", "Buy / enter license key..."), null, (_, _) => OpenLicenseDialog()));
        _trayMenu.Items.Add(CheckItem(AppText.T("始终置顶", "Always on top"), settings.AlwaysOnTop, value => UpdateSingleSetting("alwaysOnTop", value)));
        _trayMenu.Items.Add(CheckItem(AppText.T("贴边自动隐藏", "Auto-hide at edge"), settings.EdgeHide, value => UpdateSingleSetting("edgeHide", value)));
        _trayMenu.Items.Add(CheckItem(AppText.T("鼠标穿透", "Click-through"), settings.ClickThrough, value => UpdateSingleSetting("clickThrough", value)));
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(new ToolStripMenuItem(AppText.T("立即刷新", "Refresh now"), null, async (_, _) => { await RefreshUsageAsync(false); ScheduleRefresh(); }));
        _trayMenu.Items.Add(new ToolStripMenuItem(active is null ? AppText.T("登录 OpenCode", "Sign in to OpenCode") : AppText.T("重新登录 / 添加账户", "Sign in again / add account"), null, (_, _) => OpenLogin()));
        _trayMenu.Items.Add(new ToolStripMenuItem(AppText.T("打开 OpenCode", "Open OpenCode"), null, (_, _) => OpenWorkspace()));
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(new ToolStripMenuItem(AppText.T("退出", "Exit"), null, (_, _) => ExitApplication()));
    }

    private static ToolStripMenuItem CheckItem(string text, bool value, Action<bool> changed)
    {
        var item = new ToolStripMenuItem(text) { Checked = value, CheckOnClick = true };
        item.CheckedChanged += (_, _) => changed(item.Checked);
        return item;
    }

    private void UpdateSingleSetting(string name, bool value)
    {
        using var document = JsonDocument.Parse($"{{\"{name}\":{value.ToString().ToLowerInvariant()}}}");
        _config.UpdateSettings(document.RootElement);
        _dockHidden = false;
        ApplyWidgetSettings();
        if (_config.Data.Settings.EdgeHide) QueueDockHide();
        SendEvent("widget:state", new WidgetStatePayload { Settings = _config.Data.Settings, NextRefreshAt = _nextRefreshAt });
    }

    private void ShowBalloon(string title, string text, ToolTipIcon icon)
    {
        _trayIcon.BalloonTipTitle = title;
        _trayIcon.BalloonTipText = text;
        _trayIcon.BalloonTipIcon = icon;
        _trayIcon.ShowBalloonTip(5000);
    }

    private bool ToggleCompact(bool? force)
    {
        RevealDock(false);
        var next = force ?? !_config.Data.Settings.Compact;
        if (!next && !_license.Current.IsPro) next = true;
        _config.SetCompact(next);
        ApplyWidgetSettings();
        SendEvent("widget:state", new WidgetStatePayload { Compact = next, Settings = _config.Data.Settings, License = _license.Current, NextRefreshAt = _nextRefreshAt });
        QueueDockHide(900);
        return next;
    }

    private object OpenLicenseDialog()
    {
        if (!Visible) ShowWidget();
        RevealDock(false);
        var wasPro = _license.Current.IsPro;
        using var dialog = new LicenseForm(_license, _store);
        _dockHideTimer.Stop();
        _modalDialogOpen = true;
        try { dialog.ShowDialog(this); }
        finally { _modalDialogOpen = false; }
        var status = _license.Current;

        if (!status.IsPro) _config.SetCompact(true);
        else if (!wasPro && dialog.LicenseChanged) _config.SetCompact(false);

        ApplyWidgetSettings();
        var result = new LicenseStatePayload { Changed = dialog.LicenseChanged, License = status, Compact = _config.Data.Settings.Compact, Settings = _config.Data.Settings };
        SendEvent("license:state", result);
        SendEvent("widget:state", new WidgetStatePayload { Compact = _config.Data.Settings.Compact, Settings = _config.Data.Settings, License = status, NextRefreshAt = _nextRefreshAt });
        return result;
    }

    private void SetCompactModelRows(JsonElement value)
    {
        var rows = 1;
        if (value.ValueKind == JsonValueKind.Number) value.TryGetInt32(out rows);
        rows = Math.Clamp(rows, 1, 5);
        if (rows == _compactModelRows) return;
        _compactModelRows = rows;
        if (_config.Data.Settings.Compact) ApplyWidgetSettings();
    }

    private void SetRendererHover(JsonElement value)
    {
        _rendererPointerInside = value.ValueKind == JsonValueKind.True;
        if (_rendererPointerInside)
        {
            _dockHideTimer.Stop();
            RevealDock(false);
        }
        else QueueDockHide();
    }

    private int CurrentHeightDip() => _config.Data.Settings.Compact
        ? 79 + Math.Clamp(_compactModelRows, 1, 5) * 19
        : AppConstants.WidgetHeight;

    private int ScaleDip(int value)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
        return Math.Max(1, (int)Math.Round(value * dpi / 96d));
    }

    private Rectangle VisibleBounds()
    {
        var settings = _config.Data.Settings;
        var width = ScaleDip(AppConstants.WidgetWidth);
        var height = ScaleDip(CurrentHeightDip());
        var hasSaved = settings.WidgetX.HasValue && settings.WidgetY.HasValue;
        var screen = hasSaved ? Screen.FromPoint(new Point(settings.WidgetX!.Value, settings.WidgetY!.Value)) : Screen.PrimaryScreen!;
        var area = screen.WorkingArea;
        var edgeGap = ScaleDip(AppConstants.EdgeGap);
        var x = hasSaved ? settings.WidgetX!.Value : area.Right - width - edgeGap;
        var y = hasSaved ? settings.WidgetY!.Value : area.Top + edgeGap;
        if (settings.DockedEdge == "right") x = area.Right - width;
        if (settings.DockedEdge == "top") y = area.Top;
        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - width));
        y = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - height));
        return new Rectangle(x, y, width, height);
    }

    private Rectangle HiddenDockBounds()
    {
        var visible = VisibleBounds();
        var area = Screen.FromRectangle(visible).WorkingArea;
        return _config.Data.Settings.DockedEdge switch
        {
            "right" => new Rectangle(area.Right - ScaleDip(AppConstants.EdgeReveal), visible.Y, visible.Width, visible.Height),
            "top" => new Rectangle(visible.X, area.Top - visible.Height + ScaleDip(AppConstants.EdgeReveal), visible.Width, visible.Height),
            _ => visible
        };
    }

    private void EnforceLicenseMode()
    {
        if (_license.Current.IsPro || _config.Data.Settings.Compact) return;
        _config.SetCompact(true);
        ApplyWidgetSettings();
        var status = _license.Current;
        SendEvent("license:state", new LicenseStatePayload { License = status, Compact = true, Settings = _config.Data.Settings });
        SendEvent("widget:state", new WidgetStatePayload { Compact = true, Settings = _config.Data.Settings, License = status, NextRefreshAt = _nextRefreshAt });
    }

    private void ApplyWidgetSettings()
    {
        if (!_license.Current.IsPro && !_config.Data.Settings.Compact) _config.SetCompact(true);
        TopMost = _config.Data.Settings.AlwaysOnTop;
        NativeMethods.SetClickThrough(Handle, _config.Data.Settings.ClickThrough);
        SetWindowBounds(_dockHidden && _config.Data.Settings.EdgeHide && _config.Data.Settings.DockedEdge is not null
            ? HiddenDockBounds()
            : VisibleBounds());
        ApplyWindowShape();
        RebuildTrayMenu();
    }

    private void SetWindowBounds(Rectangle bounds)
    {
        _internalBoundsChange = true;
        try { Bounds = bounds; }
        finally { _internalBoundsChange = false; }
    }

    private void ApplyWindowShape()
    {
        if (Width <= 0 || Height <= 0) return;
        var radius = ScaleDip(_config.Data.Settings.Compact ? 22 : 24);
        using var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(Width - diameter, 0, diameter, diameter, 270, 90);
        path.AddArc(Width - diameter, Height - diameter, diameter, diameter, 0, 90);
        path.AddArc(0, Height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        Region?.Dispose();
        Region = new Region(path);
    }

    private void QueuePlacementSave()
    {
        if (_internalBoundsChange || _dockHidden) return;
        _moveTimer.Stop();
        _moveTimer.Start();
    }

    private void PersistWindowPlacement()
    {
        if (_internalBoundsChange || _dockHidden) return;
        var area = Screen.FromRectangle(Bounds).WorkingArea;
        var distanceRight = Math.Abs(area.Right - Right);
        var distanceTop = Math.Abs(Top - area.Top);
        string? edge = null;
        var snapDistance = ScaleDip(AppConstants.SnapDistance);
        if (_config.Data.Settings.EdgeHide && (distanceRight <= snapDistance || distanceTop <= snapDistance))
            edge = distanceRight <= distanceTop ? "right" : "top";

        var x = Left;
        var y = Top;
        if (edge == "right") x = area.Right - Width;
        if (edge == "top") y = area.Top;
        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - Width));
        y = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - Height));
        var patch = JsonSerializer.SerializeToElement(new WidgetDockPatch { DockedEdge = edge, WidgetX = x, WidgetY = y }, JsonOptions);
        _config.UpdateSettings(patch);
        if (edge is not null)
        {
            _dockHidden = false;
            SetWindowBounds(VisibleBounds());
            QueueDockHide();
        }
        SendEvent("widget:state", new WidgetStatePayload { Settings = _config.Data.Settings, DockedEdge = edge });
    }

    private void UpdateDockPointerState()
    {
        if (_modalDialogOpen)
        {
            _dockHideTimer.Stop();
            return;
        }
        if (!Visible || !_config.Data.Settings.EdgeHide || _config.Data.Settings.DockedEdge is null)
        {
            _dockHideTimer.Stop();
            return;
        }
        var point = Cursor.Position;
        if (_dockHidden)
        {
            var visible = VisibleBounds();
            var area = Screen.FromRectangle(visible).WorkingArea;
            var inHotZone = _config.Data.Settings.DockedEdge == "right"
                ? point.X >= area.Right - ScaleDip(24) && point.X <= area.Right + ScaleDip(2) && point.Y >= visible.Top - ScaleDip(14) && point.Y <= visible.Bottom + ScaleDip(14)
                : point.Y >= area.Top - ScaleDip(2) && point.Y <= area.Top + ScaleDip(24) && point.X >= visible.Left - ScaleDip(14) && point.X <= visible.Right + ScaleDip(14);
            if (inHotZone) RevealDock(false);
            return;
        }

        if (Bounds.Contains(point) || _rendererPointerInside) _dockHideTimer.Stop();
        else if (!_dockHideTimer.Enabled) QueueDockHide(450);
    }

    private void QueueDockHide(int delay = 700)
    {
        var settings = _config.Data.Settings;
        if (_modalDialogOpen || !settings.EdgeHide || settings.DockedEdge is null || _rendererPointerInside || settings.ClickThrough) return;
        _dockHideTimer.Stop();
        _dockHideTimer.Interval = Math.Max(1, delay);
        _dockHideTimer.Start();
    }

    private void HideDock()
    {
        var settings = _config.Data.Settings;
        if (_modalDialogOpen || !Visible || !settings.EdgeHide || settings.DockedEdge is null || _rendererPointerInside || settings.ClickThrough) return;
        if (Bounds.Contains(Cursor.Position)) return;
        _dockHidden = true;
        SetWindowBounds(HiddenDockBounds());
    }

    private void RevealDock(bool focus)
    {
        _dockHideTimer.Stop();
        _dockHidden = false;
        SetWindowBounds(VisibleBounds());
        if (focus) Activate();
    }

    private void ShowWidget()
    {
        RevealDock(false);
        if (!Visible) Show();
        WindowState = FormWindowState.Normal;
        Activate();
        QueueDockHide(1200);
    }

    private void ToggleWidget()
    {
        if (Visible) Hide(); else ShowWidget();
    }

    private static void ApplyLaunchAtLogin(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (key is null) return;
        if (enabled) key.SetValue(AppConstants.AppName, $"\"{Application.ExecutablePath}\"");
        else key.DeleteValue(AppConstants.AppName, false);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs args)
    {
        if (_quitting) return;
        args.Cancel = true;
        Hide();
    }

    private void ExitApplication()
    {
        _quitting = true;
        _trayIcon.Visible = false;
        Application.Exit();
    }

    private void DisposeResources()
    {
        _refreshTimer.Dispose();
        _dockPollTimer.Dispose();
        _dockHideTimer.Dispose();
        _moveTimer.Dispose();
        _licenseTimer.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayMenu.Dispose();
        _webView.Dispose();
    }
}
