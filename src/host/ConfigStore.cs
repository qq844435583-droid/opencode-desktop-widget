using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OpenCodeDesktopWidget;

internal sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public AppConfig Data { get; private set; }

    public ConfigStore()
    {
        Data = Read();
    }

    private AppConfig Read()
    {
        try
        {
            if (!File.Exists(AppConstants.ConfigPath)) return new AppConfig();
            var json = File.ReadAllText(AppConstants.ConfigPath);
            var root = JsonNode.Parse(json) as JsonObject;
            if (root is null) return new AppConfig();

            if (root["accounts"] is not JsonArray && (root["auth"] is not null || root["workspace_id"] is not null))
            {
                var migrated = new AppConfig();
                var account = BuildAccount(new AccountInput
                {
                    Name = "Main",
                    WorkspaceId = root["workspace_id"]?.GetValue<string>() ?? "",
                    Auth = root["auth"]?.GetValue<string>() ?? ""
                });
                migrated.Accounts.Add(account);
                migrated.ActiveAccountId = account.Id;
                return migrated;
            }

            var parsed = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            parsed.Accounts ??= [];
            parsed.Settings ??= new WidgetSettings();
            parsed.Cache ??= [];
            parsed.ModelAlerts ??= [];

            // Electron v1/v2 stored the interval as refreshMinutes. Preserve that setting.
            if (root["settings"] is JsonObject settingsNode &&
                settingsNode["refreshSeconds"] is null &&
                settingsNode["refreshMinutes"] is JsonValue legacyValue &&
                legacyValue.TryGetValue<double>(out var legacyMinutes) &&
                legacyMinutes > 0 && Math.Abs(legacyMinutes - 5) > double.Epsilon)
            {
                parsed.Settings.RefreshSeconds = (int)Math.Round(legacyMinutes * 60);
            }

            NormalizeSettings(parsed.Settings);
            parsed.Version = 4;
            return parsed;
        }
        catch
        {
            try { File.Copy(AppConstants.ConfigPath, AppConstants.ConfigPath + $".broken-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"); }
            catch { }
            return new AppConfig();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(AppConstants.AppDataDirectory);
        var temporary = AppConstants.ConfigPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(Data, JsonOptions));
        File.Move(temporary, AppConstants.ConfigPath, true);
    }

    public Account? ActiveAccount() => Data.Accounts.FirstOrDefault(item => item.Id == Data.ActiveAccountId);

    public AccountCredentials? Credentials(string? accountId = null)
    {
        var id = accountId ?? Data.ActiveAccountId;
        var account = Data.Accounts.FirstOrDefault(item => item.Id == id);
        if (account is null) return null;
        return new AccountCredentials
        {
            Id = account.Id,
            Name = account.Name,
            WorkspaceId = account.WorkspaceId,
            Auth = SecureStore.Reveal(account.AuthSecret)
        };
    }

    public PublicAccount UpsertAccount(AccountInput input)
    {
        var index = string.IsNullOrWhiteSpace(input.Id) ? -1 : Data.Accounts.FindIndex(item => item.Id == input.Id);
        var previous = index >= 0 ? Data.Accounts[index] : null;
        var account = BuildAccount(input, previous);
        if (!Regex.IsMatch(account.WorkspaceId, "^wrk_[A-Za-z0-9_-]+$")) throw new InvalidOperationException(AppText.T("工作区 ID 格式不正确。", "The workspace ID format is invalid."));
        if (string.IsNullOrWhiteSpace(account.AuthSecret)) throw new InvalidOperationException(AppText.T("未检测到有效的登录凭据。", "No valid sign-in credentials were detected."));
        if (input.Auth is not null && input.Auth.Trim().Length < 20) throw new InvalidOperationException(AppText.T("登录凭据格式不正确。", "The sign-in credential format is invalid."));
        if (index >= 0) Data.Accounts[index] = account; else Data.Accounts.Add(account);
        Data.ActiveAccountId = account.Id;
        Save();
        return ToPublic(account);
    }

    private static Account BuildAccount(AccountInput input, Account? previous = null)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var authSecret = input.Auth is null ? previous?.AuthSecret ?? "" : SecureStore.Protect(input.Auth.Trim());
        var requestedId = string.IsNullOrWhiteSpace(input.Id) ? null : input.Id.Trim();
        var generatedId = $"acc_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
        var name = (input.Name ?? previous?.Name ?? "OpenCode").Trim();
        if (name.Length == 0) name = "OpenCode";
        return new Account
        {
            Id = previous?.Id ?? requestedId ?? generatedId[..Math.Min(31, generatedId.Length)],
            Name = name[..Math.Min(40, name.Length)],
            WorkspaceId = (input.WorkspaceId ?? previous?.WorkspaceId ?? "").Trim(),
            AuthSecret = authSecret,
            CreatedAt = previous?.CreatedAt ?? timestamp,
            UpdatedAt = timestamp
        };
    }

    public bool DeleteAccount(string id)
    {
        var removed = Data.Accounts.RemoveAll(item => item.Id == id) > 0;
        if (!removed) return false;
        Data.Cache.Remove(id);
        Data.ModelAlerts.Remove(id);
        if (Data.ActiveAccountId == id) Data.ActiveAccountId = Data.Accounts.FirstOrDefault()?.Id;
        Save();
        return true;
    }

    public void SetActive(string id)
    {
        if (!Data.Accounts.Any(item => item.Id == id)) throw new InvalidOperationException(AppText.T("账户不存在。", "The account does not exist."));
        Data.ActiveAccountId = id;
        Save();
    }

    public WidgetSettings UpdateSettings(JsonElement patch)
    {
        var target = JsonSerializer.SerializeToNode(Data.Settings, JsonOptions)!.AsObject();
        if (patch.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in patch.EnumerateObject())
                target[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }
        Data.Settings = target.Deserialize<WidgetSettings>(JsonOptions) ?? new WidgetSettings();
        NormalizeSettings(Data.Settings);
        Data.Version = 4;
        Save();
        return Data.Settings;
    }


    public void SetCompact(bool compact)
    {
        if (Data.Settings.Compact == compact) return;
        Data.Settings.Compact = compact;
        Save();
    }

    public void SetCache(string accountId, UsageData data)
    {
        Data.Cache[accountId] = data;
        Save();
    }

    public ModelAlertState GetModelAlertState(string? accountId = null)
    {
        var id = accountId ?? Data.ActiveAccountId;
        return id is not null && Data.ModelAlerts.TryGetValue(id, out var state)
            ? new ModelAlertState { RuleSignature = state.RuleSignature, Keys = [.. state.Keys] }
            : new ModelAlertState();
    }

    public void SetModelAlertState(string accountId, ModelAlertState value)
    {
        Data.ModelAlerts[accountId] = new ModelAlertState
        {
            RuleSignature = value.RuleSignature ?? "",
            Keys = value.Keys.TakeLast(300).ToList()
        };
        Save();
    }

    public object PublicState() => new BootstrapState
    {
        ActiveAccountId = Data.ActiveAccountId,
        Accounts = Data.Accounts.Select(ToPublic).ToList(),
        Settings = Data.Settings,
        Cache = Data.Cache
    };

    private static PublicAccount ToPublic(Account account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        WorkspaceId = account.WorkspaceId,
        HasAuth = !string.IsNullOrWhiteSpace(account.AuthSecret),
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt
    };

    private static void NormalizeSettings(WidgetSettings settings)
    {
        settings.RefreshSeconds = Math.Clamp(settings.RefreshSeconds <= 0 ? AppConstants.DefaultRefreshSeconds : settings.RefreshSeconds,
            AppConstants.MinRefreshSeconds, AppConstants.MaxRefreshSeconds);
        settings.WarningThreshold = Math.Clamp(settings.WarningThreshold <= 0 ? 25 : settings.WarningThreshold, 5, 80);
        settings.Language = AppText.NormalizeLanguage(settings.Language);
        settings.DockedEdge = settings.DockedEdge is "top" or "right" ? settings.DockedEdge : null;
        settings.ModelOkRules = ModelRules.Normalize(settings.ModelOkRules);
        settings.ModelNgRules = ModelRules.Normalize(settings.ModelNgRules);
    }
}

internal sealed class AccountInput
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? Id { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("workspaceId")]
    public string? WorkspaceId { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("auth")]
    public string? Auth { get; set; }
}
