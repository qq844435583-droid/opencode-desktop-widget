using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace OpenCodeDesktopWidget;

internal sealed class LicenseService
{
    private const string ProductId = "opencode-desktop-widget-pro";
    private const string EntitlementPrefix = "OCW1";
    private const string SessionPrefix = "OCW2";
    private const string PublicKeyPem =
        "-----BEGIN PUBLIC KEY-----\n" +
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAES8PIZUmcPluaYSQo388cY32YLOeE\n" +
        "5dR3OD23x2qEypRbLmwFKfGWqF43PnvZxfkFnKrRZuFnQgr7XBzhi7c7Zg==\n" +
        "-----END PUBLIC KEY-----\n";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _deviceCode;
    private readonly string _machineFingerprint;
    private string? _entitlementKey;
    private string? _sessionToken;

    public LicenseService()
    {
        var identity = ReadMachineIdentity();
        _deviceCode = CreateDeviceCode(identity);
        _machineFingerprint = CreateFingerprint(identity).ToUpperInvariant();
        _entitlementKey = ReadProtected(AppConstants.LicensePath);
        _sessionToken = ReadProtected(AppConstants.SessionLicensePath);
    }

    public LicenseStatus Current => ValidateSession(_sessionToken);
    public string DeviceCode => _deviceCode;
    public string MachineFingerprint => _machineFingerprint;
    public string? EntitlementKey => _entitlementKey;
    public string? SessionToken => _sessionToken;
    public bool HasEntitlement => !string.IsNullOrWhiteSpace(_entitlementKey);

    public void SavePendingPurchase(string purchaseToken)
    {
        Directory.CreateDirectory(AppConstants.AppDataDirectory);
        File.WriteAllText(AppConstants.PendingPurchasePath, SecureStore.Protect(purchaseToken.Trim()));
    }

    public string? ReadPendingPurchase()
    {
        try
        {
            if (!File.Exists(AppConstants.PendingPurchasePath)) return null;
            var token = SecureStore.Reveal(File.ReadAllText(AppConstants.PendingPurchasePath)).Trim();
            return token.Length is 43 or 48 ? token : null;
        }
        catch { return null; }
    }

    public void ClearPendingPurchase()
    {
        try { if (File.Exists(AppConstants.PendingPurchasePath)) File.Delete(AppConstants.PendingPurchasePath); }
        catch { }
    }

    public EntitlementInfo InstallEntitlement(string? licenseKey)
    {
        var normalized = NormalizeLicenseKey(licenseKey);
        var info = ValidateEntitlement(normalized);
        Directory.CreateDirectory(AppConstants.AppDataDirectory);
        File.WriteAllText(AppConstants.LicensePath, SecureStore.Protect(normalized));
        _entitlementKey = normalized;
        ClearPendingPurchase();
        return info;
    }

    public LicenseStatus InstallSession(string? sessionToken)
    {
        var normalized = NormalizeLicenseKey(sessionToken);
        var status = ValidateSession(normalized);
        if (!status.IsPro) throw new InvalidOperationException(status.Message);
        Directory.CreateDirectory(AppConstants.AppDataDirectory);
        File.WriteAllText(AppConstants.SessionLicensePath, SecureStore.Protect(normalized));
        _sessionToken = normalized;
        return status;
    }

    public void ClearSession()
    {
        _sessionToken = null;
        TryDelete(AppConstants.SessionLicensePath);
    }

    public LicenseStatus Deactivate()
    {
        _entitlementKey = null;
        _sessionToken = null;
        try
        {
            TryDelete(AppConstants.LicensePath);
            TryDelete(AppConstants.SessionLicensePath);
        }
        catch (Exception error)
        {
            throw new InvalidOperationException(AppText.T("无法删除本机授权文件：", "Unable to delete the local license files: ") + error.Message, error);
        }
        return Current;
    }

    private EntitlementInfo ValidateEntitlement(string licenseKey)
    {
        var payload = VerifyAndRead<EntitlementPayload>(licenseKey, EntitlementPrefix);
        if (payload.Version != 1 || !string.Equals(payload.Product, ProductId, StringComparison.Ordinal) ||
            !string.Equals(payload.Edition, "pro", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(payload.LicenseId))
            throw new InvalidOperationException(AppText.T("授权码不适用于当前产品。", "This license key is not valid for this product."));

        DateTimeOffset? expiresAt = ParseOptionalDate(payload.ExpiresAt, "授权到期时间格式无效。", "The license expiration date is invalid.");
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException(AppText.T($"授权已于 {expiresAt:yyyy-MM-dd} 到期。", $"The license expired on {expiresAt:yyyy-MM-dd}."));

        return new EntitlementInfo(payload.LicenseId, payload.Customer ?? "", expiresAt?.ToString("O"));
    }

    private LicenseStatus ValidateSession(string? sessionToken)
    {
        var free = new LicenseStatus
        {
            DeviceCode = _deviceCode,
            Edition = "free",
            HasEntitlement = HasEntitlement,
            NeedsRefresh = HasEntitlement,
            Message = HasEntitlement
                ? AppText.T("授权需要联网验证。连接网络后将自动恢复专业版。", "The license needs online verification. Pro will be restored automatically when connected.")
                : AppText.T("免费版：仅可使用收起后的紧凑窗口。", "Free edition: only the collapsed compact window is available.")
        };
        if (string.IsNullOrWhiteSpace(sessionToken)) return free;

        try
        {
            var payload = VerifyAndRead<SessionPayload>(sessionToken, SessionPrefix);
            if (payload.Version != 2 || !string.Equals(payload.Product, ProductId, StringComparison.Ordinal) ||
                !string.Equals(payload.Edition, "pro", StringComparison.OrdinalIgnoreCase))
                return free with { Message = AppText.T("授权令牌不适用于当前产品。", "This license token is not valid for this product.") };

            if (!NormalizeDeviceCode(payload.Machine).Equals(NormalizeDeviceCode(_deviceCode), StringComparison.Ordinal))
                return free with { Message = AppText.T("授权令牌属于另一台设备。", "This license token belongs to another device.") };

            var expectedFingerprint = HashFingerprint(_machineFingerprint);
            if (!string.Equals(payload.FingerprintHash, expectedFingerprint, StringComparison.OrdinalIgnoreCase))
                return free with { Message = AppText.T("设备指纹与授权令牌不匹配。", "The device fingerprint does not match the license token.") };

            if (!DateTimeOffset.TryParse(payload.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt) ||
                !DateTimeOffset.TryParse(payload.GraceUntil, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var graceUntil))
                return free with { Message = AppText.T("授权令牌时间格式无效。", "The license token contains invalid dates.") };

            expiresAt = expiresAt.ToUniversalTime();
            graceUntil = graceUntil.ToUniversalTime();
            var now = DateTimeOffset.UtcNow;
            if (now > graceUntil)
                return free with { Message = AppText.T("离线授权宽限期已结束，请联网重新验证。", "The offline license grace period has ended. Connect to the internet to verify again."), NeedsRefresh = true };

            var inGrace = now > expiresAt;
            return new LicenseStatus
            {
                IsValid = true,
                IsPro = true,
                IsPerpetual = false,
                HasEntitlement = HasEntitlement,
                NeedsRefresh = inGrace || expiresAt - now <= TimeSpan.FromDays(2),
                InGracePeriod = inGrace,
                DeviceCode = _deviceCode,
                Edition = "pro",
                LicenseId = payload.LicenseId ?? "",
                Customer = payload.Customer ?? "",
                IssuedAt = payload.IssuedAt,
                ExpiresAt = expiresAt.ToString("O"),
                GraceUntil = graceUntil.ToString("O"),
                SessionId = payload.SessionId ?? "",
                MinClientVersion = payload.MinClientVersion ?? "",
                Message = inGrace
                    ? AppText.T($"专业版处于离线宽限期，请在 {graceUntil:yyyy-MM-dd} 前联网。", $"Pro is in offline grace mode. Connect before {graceUntil:yyyy-MM-dd}.")
                    : AppText.T($"专业版已验证，下次续签日期 {expiresAt:yyyy-MM-dd}。", $"Pro is verified. The next renewal is due on {expiresAt:yyyy-MM-dd}.")
            };
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException or InvalidOperationException)
        {
            return free with { Message = AppText.T("授权令牌验证失败。", "License token validation failed.") };
        }
    }

    private static T VerifyAndRead<T>(string token, string expectedPrefix) where T : class
    {
        var parts = NormalizeLicenseKey(token).Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !parts[0].Equals(expectedPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid token format");
        var payloadBytes = Base64UrlDecode(parts[1]);
        var signatureBytes = Base64UrlDecode(parts[2]);
        using var verifier = ECDsa.Create();
        verifier.ImportFromPem(PublicKeyPem);
        if (!verifier.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            throw new CryptographicException("Invalid signature");
        return JsonSerializer.Deserialize<T>(payloadBytes, JsonOptions) ?? throw new JsonException("Invalid payload");
    }

    private static DateTimeOffset? ParseOptionalDate(string? value, string zhError, string enError)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            throw new InvalidOperationException(AppText.T(zhError, enError));
        return parsed.ToUniversalTime();
    }

    private static string? ReadProtected(string path)
    {
        try { return File.Exists(path) ? NormalizeLicenseKey(SecureStore.Reveal(File.ReadAllText(path))) : null; }
        catch { return null; }
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static string NormalizeLicenseKey(string? value) =>
        string.Concat((value ?? "").Where(character => !char.IsWhiteSpace(character))).Trim();

    private static string NormalizeDeviceCode(string? value) =>
        string.Concat((value ?? "").Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var rem = normalized.Length % 4;
        if (rem == 2) normalized += "==";
        else if (rem == 3) normalized += "=";
        return Convert.FromBase64String(normalized);
    }

    private static string ReadMachineIdentity()
    {
        try
        {
            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null)?.ToString()
                   ?? Environment.MachineName;
        }
        catch { return Environment.MachineName; }
    }

    private static string CreateDeviceCode(string identity)
    {
        var source = Encoding.UTF8.GetBytes($"{ProductId}|{identity}");
        var hex = Convert.ToHexString(SHA256.HashData(source).AsSpan(0, 10));
        return string.Join("-", Enumerable.Range(0, 5).Select(index => hex.Substring(index * 4, 4)));
    }

    private static string CreateFingerprint(string identity)
    {
        var source = Encoding.UTF8.GetBytes($"{ProductId}|fingerprint|{identity}|{Environment.Is64BitOperatingSystem}");
        return Convert.ToHexString(SHA256.HashData(source));
    }

    private static string HashFingerprint(string fingerprint) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint))).ToLowerInvariant();
}

internal sealed record EntitlementInfo(string LicenseId, string Customer, string? ExpiresAt);

internal sealed record LicenseStatus
{
    [JsonPropertyName("isValid")] public bool IsValid { get; init; }
    [JsonPropertyName("isPro")] public bool IsPro { get; init; }
    [JsonPropertyName("isPerpetual")] public bool IsPerpetual { get; init; }
    [JsonPropertyName("hasEntitlement")] public bool HasEntitlement { get; init; }
    [JsonPropertyName("needsRefresh")] public bool NeedsRefresh { get; init; }
    [JsonPropertyName("inGracePeriod")] public bool InGracePeriod { get; init; }
    [JsonPropertyName("edition")] public string Edition { get; init; } = "free";
    [JsonPropertyName("licenseId")] public string LicenseId { get; init; } = "";
    [JsonPropertyName("customer")] public string Customer { get; init; } = "";
    [JsonPropertyName("deviceCode")] public string DeviceCode { get; init; } = "";
    [JsonPropertyName("sessionId")] public string SessionId { get; init; } = "";
    [JsonPropertyName("minClientVersion")] public string MinClientVersion { get; init; } = "";
    [JsonPropertyName("issuedAt")] public string? IssuedAt { get; init; }
    [JsonPropertyName("expiresAt")] public string? ExpiresAt { get; init; }
    [JsonPropertyName("graceUntil")] public string? GraceUntil { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = "";
}
