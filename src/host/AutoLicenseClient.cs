using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenCodeDesktopWidget;

internal sealed class AutoLicenseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public AutoLicenseClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "3.4.3";
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"OpenCode-Desktop-Widget/{version}");
        _http.DefaultRequestHeaders.Add("X-App-Version", version);
    }

    public async Task<CheckoutStartResult> StartCheckoutAsync(string deviceCode, string language, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(_baseUrl + "/api/purchase",
            new PurchaseRequest { DeviceCode = deviceCode, Language = language, ProductId = "opencode-desktop-widget-pro" }, cancellationToken);
        var payload = await ReadAsync<CheckoutStartResult>(response, cancellationToken);
        ThrowIfUpgradeRequired(response, payload);
        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(payload.CheckoutUrl) || string.IsNullOrWhiteSpace(payload.PurchaseToken))
            throw new InvalidOperationException(payload.Error ?? AppText.T("无法创建付款页面。", "Unable to create the checkout page."));
        return payload;
    }

    public async Task<PurchaseStatusResult> GetPurchaseStatusAsync(string purchaseToken, string deviceCode, CancellationToken cancellationToken)
    {
        var url = _baseUrl + "/api/license-status?token=" + Uri.EscapeDataString(purchaseToken);
        using var response = await _http.GetAsync(url, cancellationToken);
        var payload = await ReadAsync<PurchaseStatusResult>(response, cancellationToken);
        ThrowIfUpgradeRequired(response, payload);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(payload.Error ?? AppText.T("无法检查付款状态。", "Unable to check payment status."));
        return payload;
    }

    public async Task<SessionResult> ActivateAsync(string licenseKey, string deviceCode, string machineFingerprint, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(_baseUrl + "/api/activate",
            new ActivateRequest { LicenseKey = licenseKey, DeviceCode = deviceCode, MachineFingerprint = machineFingerprint }, cancellationToken);
        var payload = await ReadAsync<SessionResult>(response, cancellationToken);
        ThrowIfUpgradeRequired(response, payload);
        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(payload.SessionToken))
            throw new LicenseServerException(payload.Status, payload.Error ?? AppText.T("服务器拒绝激活。", "The server denied activation."), response.StatusCode);
        return payload;
    }

    public async Task<SessionResult> RefreshAsync(string sessionToken, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(_baseUrl + "/api/refresh", new RefreshRequest { SessionToken = sessionToken }, cancellationToken);
        var payload = await ReadAsync<SessionResult>(response, cancellationToken);
        ThrowIfUpgradeRequired(response, payload);
        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(payload.SessionToken))
            throw new LicenseServerException(payload.Status, payload.Error ?? AppText.T("无法续签授权。", "Unable to renew the license."), response.StatusCode);
        return payload;
    }

    private static void ThrowIfUpgradeRequired<T>(HttpResponseMessage response, T payload) where T : ApiResult
    {
        if (response.StatusCode == HttpStatusCode.UpgradeRequired || payload.UpgradeRequired)
            throw new UpgradeRequiredException(payload.MinClientVersion, payload.LatestVersion, payload.DownloadUrl,
                payload.Error ?? AppText.T("需要升级应用后才能继续。", "You must update the app to continue."));
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : ApiResult, new()
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken) ?? new T();
        }
        catch { return new T(); }
    }
}

internal class ApiResult
{
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("upgradeRequired")] public bool UpgradeRequired { get; set; }
    [JsonPropertyName("minClientVersion")] public string? MinClientVersion { get; set; }
    [JsonPropertyName("latestVersion")] public string? LatestVersion { get; set; }
    [JsonPropertyName("downloadUrl")] public string? DownloadUrl { get; set; }
}

internal sealed class CheckoutStartResult : ApiResult
{
    [JsonPropertyName("purchaseToken")] public string? PurchaseToken { get; set; }
    [JsonPropertyName("token")] public string? Token { get => PurchaseToken; set => PurchaseToken = value; }
    [JsonPropertyName("checkoutUrl")] public string? CheckoutUrl { get; set; }
}

internal sealed class PurchaseStatusResult : ApiResult
{
    [JsonPropertyName("licenseKey")] public string? LicenseKey { get; set; }
    [JsonPropertyName("license")] public string? License { get => LicenseKey; set => LicenseKey = value; }
    [JsonPropertyName("customerEmail")] public string? CustomerEmail { get; set; }
}

internal sealed class SessionResult : ApiResult
{
    [JsonPropertyName("sessionToken")] public string? SessionToken { get; set; }
    [JsonPropertyName("deviceCount")] public int DeviceCount { get; set; }
    [JsonPropertyName("deviceLimit")] public int DeviceLimit { get; set; }
}

internal sealed class LicenseServerException : Exception
{
    public string? Status { get; }
    public HttpStatusCode HttpStatus { get; }
    public LicenseServerException(string? status, string message, HttpStatusCode httpStatus) : base(message)
    {
        Status = status;
        HttpStatus = httpStatus;
    }
}

internal sealed class UpgradeRequiredException : Exception
{
    public string? MinVersion { get; }
    public string? LatestVersion { get; }
    public string? DownloadUrl { get; }
    public UpgradeRequiredException(string? minVersion, string? latestVersion, string? downloadUrl, string message) : base(message)
    {
        MinVersion = minVersion;
        LatestVersion = latestVersion;
        DownloadUrl = downloadUrl;
    }
}
