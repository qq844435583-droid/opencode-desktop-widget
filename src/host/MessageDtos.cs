using System.Text.Json.Serialization;

namespace OpenCodeDesktopWidget;

internal sealed class EntitlementPayload
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("product")] public string Product { get; set; } = "";
    [JsonPropertyName("edition")] public string Edition { get; set; } = "";
    [JsonPropertyName("licenseId")] public string? LicenseId { get; set; }
    [JsonPropertyName("customer")] public string? Customer { get; set; }
    [JsonPropertyName("machine")] public string Machine { get; set; } = "";
    [JsonPropertyName("issuedAt")] public string? IssuedAt { get; set; }
    [JsonPropertyName("expiresAt")] public string? ExpiresAt { get; set; }
}

internal sealed class SessionPayload
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("product")] public string Product { get; set; } = "";
    [JsonPropertyName("edition")] public string Edition { get; set; } = "";
    [JsonPropertyName("licenseId")] public string? LicenseId { get; set; }
    [JsonPropertyName("customer")] public string? Customer { get; set; }
    [JsonPropertyName("machine")] public string Machine { get; set; } = "";
    [JsonPropertyName("fingerprintHash")] public string FingerprintHash { get; set; } = "";
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
    [JsonPropertyName("issuedAt")] public string? IssuedAt { get; set; }
    [JsonPropertyName("expiresAt")] public string ExpiresAt { get; set; } = "";
    [JsonPropertyName("graceUntil")] public string GraceUntil { get; set; } = "";
    [JsonPropertyName("minClientVersion")] public string? MinClientVersion { get; set; }
}

internal sealed class HostResponseEnvelope
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "response";
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("result")] public object? Result { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}

internal sealed class HostEventEnvelope
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "event";
    [JsonPropertyName("event")] public string? Event { get; set; }
    [JsonPropertyName("payload")] public object? Payload { get; set; }
}

internal sealed class OkResult
{
    [JsonPropertyName("ok")] public bool Ok { get; set; } = true;
}

internal sealed class StoreInfo
{
    [JsonPropertyName("productName")] public string ProductName { get; set; } = "";
    [JsonPropertyName("purchaseUrlConfigured")] public bool PurchaseUrlConfigured { get; set; }
}

internal sealed class BootstrapState
{
    [JsonPropertyName("activeAccountId")] public string? ActiveAccountId { get; set; }
    [JsonPropertyName("accounts")] public List<PublicAccount> Accounts { get; set; } = [];
    [JsonPropertyName("settings")] public WidgetSettings Settings { get; set; } = new();
    [JsonPropertyName("license")] public LicenseStatus License { get; set; } = new();
    [JsonPropertyName("store")] public StoreInfo Store { get; set; } = new();
    [JsonPropertyName("cache")] public Dictionary<string, UsageData> Cache { get; set; } = [];
    [JsonPropertyName("usage")] public UsageData? Usage { get; set; }
    [JsonPropertyName("platform")] public string Platform { get; set; } = "win32-webview2";
    [JsonPropertyName("appVersion")] public string AppVersion { get; set; } = "3.0.0";
    [JsonPropertyName("nextRefreshAt")] public long NextRefreshAt { get; set; }
}

internal sealed class AccountSaveResult
{
    [JsonPropertyName("ok")] public bool Ok { get; set; } = true;
    [JsonPropertyName("account")] public PublicAccount? Account { get; set; }
    [JsonPropertyName("state")] public BootstrapState? State { get; set; }
}

internal sealed class StateOnlyResult
{
    [JsonPropertyName("ok")] public bool Ok { get; set; } = true;
    [JsonPropertyName("state")] public BootstrapState? State { get; set; }
}

internal sealed class UsageUpdatePayload
{
    [JsonPropertyName("ok")] public bool Ok { get; set; }
    [JsonPropertyName("data")] public UsageData? Data { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("at")] public string? At { get; set; }
    [JsonPropertyName("loading")] public bool? Loading { get; set; }
    [JsonPropertyName("accountId")] public string? AccountId { get; set; }
    [JsonPropertyName("nextRefreshAt")] public long NextRefreshAt { get; set; }
}

internal sealed class LicenseStatePayload
{
    [JsonPropertyName("ok")] public bool Ok { get; set; } = true;
    [JsonPropertyName("license")] public LicenseStatus License { get; set; } = new();
    [JsonPropertyName("compact")] public bool Compact { get; set; }
    [JsonPropertyName("settings")] public WidgetSettings Settings { get; set; } = new();
    [JsonPropertyName("changed")] public bool? Changed { get; set; }
}

internal sealed class LicenseStatusResult
{
    [JsonPropertyName("ok")] public bool Ok { get; set; } = true;
    [JsonPropertyName("license")] public LicenseStatus License { get; set; } = new();
    [JsonPropertyName("compact")] public bool Compact { get; set; }
}

internal sealed class WidgetStatePayload
{
    [JsonPropertyName("settings")] public WidgetSettings Settings { get; set; } = new();
    [JsonPropertyName("compact")] public bool? Compact { get; set; }
    [JsonPropertyName("license")] public LicenseStatus? License { get; set; }
    [JsonPropertyName("nextRefreshAt")] public long? NextRefreshAt { get; set; }
    [JsonPropertyName("dockedEdge")] public string? DockedEdge { get; set; }
    [JsonPropertyName("widgetX")] public int? WidgetX { get; set; }
    [JsonPropertyName("widgetY")] public int? WidgetY { get; set; }
}

internal sealed class ToggleCompactResult
{
    [JsonPropertyName("ok")] public bool Ok { get; set; } = true;
    [JsonPropertyName("locked")] public bool? Locked { get; set; }
    [JsonPropertyName("compact")] public bool Compact { get; set; }
    [JsonPropertyName("license")] public LicenseStatus License { get; set; } = new();
}

internal sealed class SettingsUpdateResult
{
    [JsonPropertyName("ok")] public bool Ok { get; set; } = true;
    [JsonPropertyName("settings")] public WidgetSettings Settings { get; set; } = new();
    [JsonPropertyName("nextRefreshAt")] public long NextRefreshAt { get; set; }
}

internal sealed class ModelAlertPayload
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("models")] public List<string> Models { get; set; } = [];
    [JsonPropertyName("records")] public List<UsageRecord> Records { get; set; } = [];
}

internal sealed class LoginStatePayload
{
    [JsonPropertyName("ok")] public bool Ok { get; set; } = true;
    [JsonPropertyName("account")] public PublicAccount? Account { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("started")] public bool? Started { get; set; }
}

internal sealed class ExportResult
{
    [JsonPropertyName("canceled")] public bool Canceled { get; set; }
    [JsonPropertyName("filePath")] public string? FilePath { get; set; }
}

internal sealed class WidgetDockPatch
{
    [JsonPropertyName("dockedEdge")] public string? DockedEdge { get; set; }
    [JsonPropertyName("widgetX")] public int? WidgetX { get; set; }
    [JsonPropertyName("widgetY")] public int? WidgetY { get; set; }
}

internal sealed class ServerRequestArgs
{
    [JsonPropertyName("t")] public ServerRequestT T { get; set; } = new();
    [JsonPropertyName("f")] public int F { get; set; }
    [JsonPropertyName("m")] public List<object> M { get; set; } = [];
}

internal sealed class ServerRequestT
{
    [JsonPropertyName("t")] public int T { get; set; }
    [JsonPropertyName("i")] public int I { get; set; }
    [JsonPropertyName("l")] public int L { get; set; }
    [JsonPropertyName("a")] public List<ServerRequestA> A { get; set; } = [];
    [JsonPropertyName("o")] public int O { get; set; }
}

internal sealed class ServerRequestA
{
    [JsonPropertyName("t")] public int T { get; set; }
    [JsonPropertyName("s")] public string S { get; set; } = "";
}

internal sealed class RuleSignaturePayload
{
    [JsonPropertyName("ok")] public List<string> Ok { get; set; } = [];
    [JsonPropertyName("ng")] public List<string> Ng { get; set; } = [];
}

internal sealed class PurchaseRequest
{
    [JsonPropertyName("deviceCode")] public string DeviceCode { get; set; } = "";
    [JsonPropertyName("language")] public string Language { get; set; } = "";
    [JsonPropertyName("productId")] public string ProductId { get; set; } = "opencode-desktop-widget-pro";
}

internal sealed class ActivateRequest
{
    [JsonPropertyName("licenseKey")] public string LicenseKey { get; set; } = "";
    [JsonPropertyName("deviceCode")] public string DeviceCode { get; set; } = "";
    [JsonPropertyName("machineFingerprint")] public string MachineFingerprint { get; set; } = "";
}

internal sealed class RefreshRequest
{
    [JsonPropertyName("sessionToken")] public string SessionToken { get; set; } = "";
}
