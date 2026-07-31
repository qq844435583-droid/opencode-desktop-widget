using System.Text.Json.Serialization;

namespace OpenCodeDesktopWidget;

internal sealed class AppConfig
{
    [JsonPropertyName("version")] public int Version { get; set; } = 4;
    [JsonPropertyName("activeAccountId")] public string? ActiveAccountId { get; set; }
    [JsonPropertyName("accounts")] public List<Account> Accounts { get; set; } = [];
    [JsonPropertyName("settings")] public WidgetSettings Settings { get; set; } = new();
    [JsonPropertyName("cache")] public Dictionary<string, UsageData> Cache { get; set; } = [];
    [JsonPropertyName("modelAlerts")] public Dictionary<string, ModelAlertState> ModelAlerts { get; set; } = [];
}

internal sealed class Account
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "OpenCode";
    [JsonPropertyName("workspaceId")] public string WorkspaceId { get; set; } = "";
    [JsonPropertyName("authSecret")] public string AuthSecret { get; set; } = "";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; set; } = "";
}

internal sealed class AccountCredentials
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("workspaceId")] public string WorkspaceId { get; init; } = "";
    [JsonPropertyName("auth")] public string Auth { get; init; } = "";
}

internal sealed class PublicAccount
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("workspaceId")] public string WorkspaceId { get; init; } = "";
    [JsonPropertyName("hasAuth")] public bool HasAuth { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = "";
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = "";
}

internal sealed class WidgetSettings
{
    [JsonPropertyName("refreshSeconds")] public int RefreshSeconds { get; set; } = AppConstants.DefaultRefreshSeconds;
    [JsonPropertyName("launchAtLogin")] public bool LaunchAtLogin { get; set; }
    [JsonPropertyName("closeToTray")] public bool CloseToTray { get; set; } = true;
    [JsonPropertyName("notifications")] public bool Notifications { get; set; } = true;
    [JsonPropertyName("warningThreshold")] public int WarningThreshold { get; set; } = 25;
    [JsonPropertyName("language")] public string Language { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en-US" : "zh-CN";
    [JsonPropertyName("compact")] public bool Compact { get; set; }
    [JsonPropertyName("alwaysOnTop")] public bool AlwaysOnTop { get; set; } = true;
    [JsonPropertyName("clickThrough")] public bool ClickThrough { get; set; }
    [JsonPropertyName("edgeHide")] public bool EdgeHide { get; set; } = true;
    [JsonPropertyName("dockedEdge")] public string? DockedEdge { get; set; }
    [JsonPropertyName("widgetX")] public int? WidgetX { get; set; }
    [JsonPropertyName("widgetY")] public int? WidgetY { get; set; }
    [JsonPropertyName("modelOkRules")] public List<string> ModelOkRules { get; set; } = [];
    [JsonPropertyName("modelNgRules")] public List<string> ModelNgRules { get; set; } = [];
    [JsonPropertyName("ngAlertEnabled")] public bool NgAlertEnabled { get; set; } = true;
}

internal sealed class UsageData
{
    [JsonPropertyName("workspaceId")] public string WorkspaceId { get; set; } = "";
    [JsonPropertyName("summary")] public UsageSummary Summary { get; set; } = new();
    [JsonPropertyName("records")] public List<UsageRecord> Records { get; set; } = [];
    [JsonPropertyName("detail")] public UsageDetail Detail { get; set; } = new();
    [JsonPropertyName("source")] public string Source { get; set; } = "api";
    [JsonPropertyName("fetchedAt")] public string FetchedAt { get; set; } = "";
    [JsonPropertyName("fallbackReason")] public string? FallbackReason { get; set; }
    [JsonPropertyName("diagnostics")] public object? Diagnostics { get; set; }
}

internal sealed class UsageSummary
{
    [JsonPropertyName("rolling")] public UsageMetric? Rolling { get; set; }
    [JsonPropertyName("weekly")] public UsageMetric? Weekly { get; set; }
    [JsonPropertyName("monthly")] public UsageMetric? Monthly { get; set; }
    [JsonPropertyName("useBalance")] public bool UseBalance { get; set; }
    [JsonPropertyName("isMine")] public bool IsMine { get; set; }
}

internal sealed class UsageMetric
{
    [JsonPropertyName("status")] public string Status { get; set; } = "unknown";
    [JsonPropertyName("resetText")] public string ResetText { get; set; } = "";
    [JsonPropertyName("resetInSec")] public int ResetInSec { get; set; }
    [JsonPropertyName("usedPercent")] public double UsedPercent { get; set; }
    [JsonPropertyName("usagePercent")] public double UsagePercent { get; set; }
    [JsonPropertyName("remainingPercent")] public double RemainingPercent { get; set; }
}

internal sealed class UsageRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("time")] public string Time { get; set; } = "";
    [JsonPropertyName("model")] public string Model { get; set; } = "unknown";
    [JsonPropertyName("provider")] public string Provider { get; set; } = "";
    [JsonPropertyName("session")] public string Session { get; set; } = "";
    [JsonPropertyName("inputTokens")] public double InputTokens { get; set; }
    [JsonPropertyName("outputTokens")] public double OutputTokens { get; set; }
    [JsonPropertyName("reasoningTokens")] public double ReasoningTokens { get; set; }
    [JsonPropertyName("cacheReadTokens")] public double CacheReadTokens { get; set; }
    [JsonPropertyName("cacheWriteTokens")] public double CacheWriteTokens { get; set; }
    [JsonPropertyName("cost")] public double Cost { get; set; }
    [JsonPropertyName("plan")] public string Plan { get; set; } = "";
    [JsonPropertyName("modelStatus")] public string ModelStatus { get; set; } = "unknown";
}

internal sealed class UsageDetail
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("totalCost")] public double TotalCost { get; set; }
    [JsonPropertyName("totalInput")] public double TotalInput { get; set; }
    [JsonPropertyName("totalOutput")] public double TotalOutput { get; set; }
    [JsonPropertyName("totalReasoning")] public double TotalReasoning { get; set; }
    [JsonPropertyName("totalCache")] public double TotalCache { get; set; }
    [JsonPropertyName("modelCounts")] public Dictionary<string, int> ModelCounts { get; set; } = [];
    [JsonPropertyName("plans")] public List<string> Plans { get; set; } = [];
}

internal sealed class ModelAlertState
{
    [JsonPropertyName("ruleSignature")] public string RuleSignature { get; set; } = "";
    [JsonPropertyName("keys")] public List<string> Keys { get; set; } = [];
}
