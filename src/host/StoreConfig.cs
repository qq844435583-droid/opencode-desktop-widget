using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenCodeDesktopWidget;

internal sealed class StoreConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = "OpenCode Desktop Widget Pro";
    [JsonPropertyName("licenseServerUrl")]
    public string LicenseServerUrl { get; set; } = "";
    [JsonPropertyName("purchaseUrl")]
    public string PurchaseUrl { get; set; } = "";
    [JsonPropertyName("supportEmail")]
    public string SupportEmail { get; set; } = "";

    public static StoreConfig Load()
    {
        try
        {
            if (!File.Exists(AppConstants.StoreConfigPath)) return new StoreConfig();
            var parsed = JsonSerializer.Deserialize<StoreConfig>(File.ReadAllText(AppConstants.StoreConfigPath), JsonOptions);
            return parsed ?? new StoreConfig();
        }
        catch { return new StoreConfig(); }
    }

    public bool HasLicenseServer => Uri.TryCreate(LicenseServerUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    public bool HasPurchaseUrl => Uri.TryCreate(PurchaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
