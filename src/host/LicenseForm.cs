namespace OpenCodeDesktopWidget;

internal sealed class LicenseForm : Form
{
    private readonly LicenseService _license;
    private readonly StoreConfig _store;
    private readonly AutoLicenseClient? _autoClient;
    private readonly TextBox _licenseBox = new();
    private readonly Label _statusLabel = new();
    private readonly Button _deactivateButton = new();
    private readonly Button _purchaseButton;
    private CancellationTokenSource? _purchaseCancellation;

    public bool LicenseChanged { get; private set; }

    public LicenseForm(LicenseService license, StoreConfig store)
    {
        _license = license;
        _store = store;
        _autoClient = store.HasLicenseServer ? new AutoLicenseClient(store.LicenseServerUrl) : null;

        Text = AppText.T("专业版授权", "Pro license");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(620, 438);
        BackColor = Color.FromArgb(23, 31, 45);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        if (File.Exists(AppConstants.IconPath)) Icon = new Icon(AppConstants.IconPath);

        Controls.Add(MakeLabel(_store.ProductName, 24, 20, 560, 30, 17F, FontStyle.Bold));
        Controls.Add(MakeLabel(
            AppText.T(
                "免费版只能使用收起后的紧凑窗口。付款成功后软件会自动验证并解锁专业版。",
                "The free edition only supports the collapsed compact window. After payment, the app verifies the purchase and unlocks Pro automatically."),
            24, 57, 565, 42, 9.5F));

        Controls.Add(MakeLabel(AppText.T("本机设备码", "Device code"), 24, 112, 120, 24, 9F, FontStyle.Bold));
        var deviceBox = new TextBox
        {
            Left = 24, Top = 138, Width = 438, Height = 32, ReadOnly = true,
            Text = _license.DeviceCode,
            BackColor = Color.FromArgb(12, 18, 28), ForeColor = Color.FromArgb(174, 211, 255),
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Mono", 11F, FontStyle.Bold)
        };
        Controls.Add(deviceBox);

        var copyButton = MakeButton(AppText.T("复制设备码", "Copy device code"), 474, 138, 116, 32, false);
        copyButton.Click += (_, _) => CopyText(deviceBox.Text, AppText.T("设备码已复制。", "Device code copied."));
        Controls.Add(copyButton);

        Controls.Add(MakeLabel(AppText.T("授权码（自动付款失败时可手动输入）", "License key (manual fallback)"), 24, 187, 280, 24, 9F, FontStyle.Bold));
        _licenseBox.SetBounds(24, 213, 566, 92);
        _licenseBox.Multiline = true;
        _licenseBox.ScrollBars = ScrollBars.Vertical;
        _licenseBox.PlaceholderText = AppText.T("付款后会自动填入；也可以粘贴以 OCW1. 开头的授权码", "Filled automatically after payment, or paste a key beginning with OCW1.");
        _licenseBox.BackColor = Color.FromArgb(12, 18, 28);
        _licenseBox.ForeColor = Color.White;
        _licenseBox.BorderStyle = BorderStyle.FixedSingle;
        _licenseBox.Font = new Font("Cascadia Mono", 9F);
        Controls.Add(_licenseBox);

        _statusLabel.SetBounds(24, 316, 566, 42);
        _statusLabel.ForeColor = Color.FromArgb(183, 196, 216);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        Controls.Add(_statusLabel);

        _purchaseButton = MakeButton(AppText.T("购买并自动解锁", "Buy & auto-unlock"), 24, 378, 154, 36, true);
        _purchaseButton.Click += async (_, _) => await BeginAutomaticPurchaseAsync();
        Controls.Add(_purchaseButton);

        _deactivateButton.SetBounds(188, 378, 132, 36);
        _deactivateButton.Text = AppText.T("解除本机授权", "Deactivate license");
        StyleButton(_deactivateButton, false);
        _deactivateButton.Click += (_, _) => Deactivate();
        Controls.Add(_deactivateButton);

        var closeButton = MakeButton(AppText.T("关闭", "Close"), 374, 378, 92, 36, false);
        closeButton.Click += (_, _) => Close();
        Controls.Add(closeButton);

        var activateButton = MakeButton(AppText.T("手动激活", "Manual activation"), 476, 378, 114, 36, true);
        activateButton.Click += async (_, _) => await ActivateAsync();
        Controls.Add(activateButton);
        AcceptButton = activateButton;
        CancelButton = closeButton;

        FormClosed += (_, _) => _purchaseCancellation?.Cancel();
        Shown += async (_, _) =>
        {
            await RefreshOrActivateExistingAsync();
            await ResumePendingPurchaseAsync();
        };
        RenderStatus();
        _purchaseButton.Enabled = !_license.Current.IsPro;
    }

    private void RenderStatus(string? temporaryMessage = null)
    {
        var status = _license.Current;
        _statusLabel.Text = temporaryMessage ?? (status.Message + (status.IsPro && !string.IsNullOrWhiteSpace(status.Customer)
            ? AppText.T($"  授权给：{status.Customer}", $"  Licensed to: {status.Customer}") : ""));
        _statusLabel.ForeColor = status.IsPro ? Color.FromArgb(99, 230, 164) : Color.FromArgb(183, 196, 216);
        _deactivateButton.Visible = status.IsPro;
    }

    private async Task BeginAutomaticPurchaseAsync()
    {
        if (_autoClient is null)
        {
            OpenLegacyPurchasePage();
            return;
        }

        _purchaseCancellation?.Cancel();
        _purchaseCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var token = _purchaseCancellation.Token;
        _purchaseButton.Enabled = false;
        try
        {
            RenderStatus(AppText.T("正在创建安全付款页面…", "Creating a secure checkout page…"));
            var started = await _autoClient.StartCheckoutAsync(_license.DeviceCode, AppText.Language, token);
            _license.SavePendingPurchase(started.PurchaseToken!);
            NativeMethods.OpenExternal(started.CheckoutUrl!);
            RenderStatus(AppText.T("付款页面已打开。完成付款后请保持此窗口开启，软件会自动解锁。", "Checkout opened. Keep this window open after payment; the app will unlock automatically."));

            await PollAndActivateAsync(started.PurchaseToken!, token);
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed) RenderStatus(AppText.T("自动检查已停止。可以再次点击购买继续。", "Automatic checking stopped. Click Buy again to continue."));
        }
        catch (Exception error)
        {
            if (!IsDisposed)
            {
                RenderStatus(AppText.T("自动付款未完成：", "Automatic purchase did not complete: ") + error.Message);
                MessageBox.Show(this, error.Message, AppText.T("付款验证失败", "Payment verification failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            if (!IsDisposed) _purchaseButton.Enabled = !_license.Current.IsPro;
        }
    }

    private async Task ResumePendingPurchaseAsync()
    {
        if (_autoClient is null || _license.Current.IsPro) return;
        var pending = _license.ReadPendingPurchase();
        if (string.IsNullOrWhiteSpace(pending)) return;
        _purchaseCancellation?.Cancel();
        _purchaseCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        _purchaseButton.Enabled = false;
        try
        {
            RenderStatus(AppText.T(
                "正在检查上次付款状态。你也可以点击购买开始新的付款。",
                "Checking your previous purchase status. You can also click Buy to start a new checkout."));
            // Do not lock the purchase button while an older pending checkout is being polled.
            // Clicking Buy cancels this poll and creates a fresh checkout token.
            _purchaseButton.Enabled = true;
            await PollAndActivateAsync(pending, _purchaseCancellation.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            _license.ClearPendingPurchase();
            if (!IsDisposed) RenderStatus(AppText.T("无法恢复上次付款：", "Unable to resume the previous purchase: ") + error.Message);
        }
        finally
        {
            if (!IsDisposed) _purchaseButton.Enabled = !_license.Current.IsPro;
        }
    }

    private async Task PollAndActivateAsync(string purchaseToken, CancellationToken token)
    {
        if (_autoClient is null) return;
        while (!token.IsCancellationRequested)
        {
            var result = await _autoClient.GetPurchaseStatusAsync(purchaseToken, _license.DeviceCode, token);
            if (string.Equals(result.Status, "paid", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(result.LicenseKey))
            {
                _licenseBox.Text = result.LicenseKey;
                _license.InstallEntitlement(result.LicenseKey);
                var activated = await _autoClient.ActivateAsync(result.LicenseKey, _license.DeviceCode, _license.MachineFingerprint, token);
                var status = _license.InstallSession(activated.SessionToken);
                LicenseChanged = true;
                RenderStatus();
                MessageBox.Show(this,
                    status.Message + AppText.T($"  已使用设备 {activated.DeviceCount}/{activated.DeviceLimit}。", $"  Devices used: {activated.DeviceCount}/{activated.DeviceLimit}."),
                    AppText.T("付款成功", "Payment successful"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
                return;
            }
            if (string.Equals(result.Status, "failed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(result.Error ?? AppText.T("付款验证失败。", "Payment verification failed."));
            await Task.Delay(TimeSpan.FromSeconds(3), token);
        }
    }

    private async Task ActivateAsync()
    {
        try
        {
            if (_autoClient is null) throw new InvalidOperationException(AppText.T("卖家尚未配置授权服务器。", "The seller has not configured the license server."));
            var entitlement = _licenseBox.Text.Trim();
            _license.InstallEntitlement(entitlement);
            RenderStatus(AppText.T("正在联系授权服务器…", "Contacting the license server…"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var activated = await _autoClient.ActivateAsync(entitlement, _license.DeviceCode, _license.MachineFingerprint, timeout.Token);
            var status = _license.InstallSession(activated.SessionToken);
            LicenseChanged = true;
            RenderStatus();
            MessageBox.Show(this,
                status.Message + AppText.T($"  已使用设备 {activated.DeviceCount}/{activated.DeviceLimit}。", $"  Devices used: {activated.DeviceCount}/{activated.DeviceLimit}."),
                AppText.T("激活成功", "Activation successful"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (UpgradeRequiredException error)
        {
            HandleUpgradeRequired(error);
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, AppText.T("激活失败", "Activation failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RenderStatus();
        }
    }

    private async Task RefreshOrActivateExistingAsync()
    {
        if (_autoClient is null) return;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            SessionResult? result = null;
            if (!string.IsNullOrWhiteSpace(_license.SessionToken))
                result = await _autoClient.RefreshAsync(_license.SessionToken, timeout.Token);
            else if (!string.IsNullOrWhiteSpace(_license.EntitlementKey))
                result = await _autoClient.ActivateAsync(_license.EntitlementKey, _license.DeviceCode, _license.MachineFingerprint, timeout.Token);
            if (result?.SessionToken is not null)
            {
                _license.InstallSession(result.SessionToken);
                LicenseChanged = true;
                RenderStatus();
            }
        }
        catch (UpgradeRequiredException error)
        {
            HandleUpgradeRequired(error);
        }
        catch (LicenseServerException error) when ((error.Status ?? "").ToLowerInvariant() is "revoked" or "refunded" or "expired" or "deviceremoved")
        {
            _license.ClearSession();
            LicenseChanged = true;
            RenderStatus(AppText.T("授权已被停用或本机已解绑。", "The license was disabled or this device was removed."));
        }
        catch
        {
            // Network failures keep the existing signed token usable until its offline grace period ends.
            RenderStatus();
        }
    }

    private void HandleUpgradeRequired(UpgradeRequiredException error)
    {
        var message = AppText.T(
            $"此版本已停止签发授权。最低版本：{error.MinVersion ?? "未知"}。",
            $"This version can no longer receive a license. Minimum version: {error.MinVersion ?? "unknown"}.");
        if (!string.IsNullOrWhiteSpace(error.DownloadUrl) &&
            MessageBox.Show(this, message + AppText.T("\n\n现在打开下载页面？", "\n\nOpen the download page now?"),
                AppText.T("需要升级", "Update required"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            NativeMethods.OpenExternal(error.DownloadUrl);
        else
            MessageBox.Show(this, message, AppText.T("需要升级", "Update required"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void Deactivate()
    {
        if (MessageBox.Show(this,
                AppText.T("解除后将立即恢复为只能使用紧凑窗口的免费版。", "After deactivation, the app will immediately return to the free compact-only edition."),
                AppText.T("解除授权", "Deactivate license"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            _license.Deactivate();
            LicenseChanged = true;
            _licenseBox.Clear();
            RenderStatus();
            _purchaseButton.Enabled = true;
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, AppText.T("解除失败", "Deactivation failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenLegacyPurchasePage()
    {
        if (!_store.HasPurchaseUrl)
        {
            var extra = string.IsNullOrWhiteSpace(_store.SupportEmail) ? "" : AppText.T($"\n\n联系邮箱：{_store.SupportEmail}", $"\n\nSupport email: {_store.SupportEmail}");
            MessageBox.Show(this,
                AppText.T("卖家还没有配置自动授权服务器或购买链接。", "The seller has not configured an automatic license server or purchase URL.") + extra,
                AppText.T("购买服务未配置", "Purchase service not configured"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        NativeMethods.OpenExternal(_store.PurchaseUrl);
    }

    private static void CopyText(string value, string successMessage)
    {
        try { Clipboard.SetText(value); MessageBox.Show(successMessage, AppText.T("已复制", "Copied"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception error) { MessageBox.Show(AppText.T("复制失败：", "Copy failed: ") + error.Message, AppText.T("复制失败", "Copy failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private static Label MakeLabel(string text, int x, int y, int width, int height, float size, FontStyle style = FontStyle.Regular) => new()
    {
        Text = text, Left = x, Top = y, Width = width, Height = height, AutoEllipsis = true,
        Font = new Font("Segoe UI", size, style), ForeColor = Color.White
    };

    private static Button MakeButton(string text, int x, int y, int width, int height, bool primary)
    {
        var button = new Button { Text = text };
        button.SetBounds(x, y, width, height);
        StyleButton(button, primary);
        return button;
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Color.FromArgb(111, 166, 255) : Color.FromArgb(70, 82, 102);
        button.BackColor = primary ? Color.FromArgb(77, 126, 218) : Color.FromArgb(37, 48, 65);
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
    }
}
