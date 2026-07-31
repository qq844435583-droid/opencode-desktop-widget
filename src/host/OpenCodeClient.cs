using System.Net;
using System.Text;
using System.Text.Json;

namespace OpenCodeDesktopWidget;

internal sealed class OpenCodeClient
{
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        UseCookies = false
    });

    public async Task<UsageData> FetchUsageAsync(AccountCredentials account, CancellationToken cancellationToken = default)
    {
        var summaryTask = CallAsync(AppConstants.SummaryHash, account.WorkspaceId, account.Auth, cancellationToken);
        var listTask = CallAsync(AppConstants.ListHash, account.WorkspaceId, account.Auth, cancellationToken);
        await Task.WhenAll(summaryTask, listTask);
        var summaryRaw = await summaryTask;
        var listRaw = await listTask;
        var records = OpenCodeParser.ParseRecords(listRaw);
        return new UsageData
        {
            WorkspaceId = account.WorkspaceId,
            Summary = OpenCodeParser.ParseSummary(summaryRaw),
            Records = records,
            Detail = records.Count > 0 ? OpenCodeParser.Summarize(records) : OpenCodeParser.ParseAggregate(listRaw) ?? new UsageDetail(),
            Source = "api",
            FetchedAt = DateTime.UtcNow.ToString("O")
        };
    }

    private static async Task<string> CallAsync(string hash, string workspaceId, string auth, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(auth) || auth.Length < 20) throw new InvalidOperationException(AppText.T("登录凭据缺失，请重新登录。", "Sign-in credentials are missing. Please sign in again."));
        var args = JsonSerializer.Serialize(new ServerRequestArgs
        {
            T = new ServerRequestT
            {
                T = 9,
                I = 0,
                L = 1,
                A = [new ServerRequestA { T = 1, S = workspaceId }],
                O = 0
            },
            F = 31,
            M = []
        });
        var url = $"{AppConstants.BaseUrl}/_server?id={hash}&args={Uri.EscapeDataString(args)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Cookie", "auth=" + auth);
        request.Headers.TryAddWithoutValidation("X-Server-Id", hash);
        request.Headers.TryAddWithoutValidation("X-Server-Instance", "fn:" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/131.0 WebView2");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(AppConstants.RequestTimeoutMs);
        try
        {
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new InvalidOperationException(AppText.T("登录已过期，请重新登录。", "Your sign-in has expired. Please sign in again."));
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(AppText.T($"OpenCode 请求失败（HTTP {(int)response.StatusCode}）。", $"The OpenCode request failed (HTTP {(int)response.StatusCode})."));
            var text = await response.Content.ReadAsStringAsync(timeout.Token);
            if (text.Contains("/auth/authorize", StringComparison.Ordinal) &&
                (text.Contains("status:302", StringComparison.Ordinal) || text.Contains("location", StringComparison.Ordinal) || text.Contains("OpenAuth", StringComparison.Ordinal)))
                throw new InvalidOperationException(AppText.T("登录已过期，请重新登录。", "Your sign-in has expired. Please sign in again."));
            return text;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(AppText.T("连接 OpenCode 超时，请检查网络后重试。", "The OpenCode connection timed out. Check your network and try again."));
        }
    }
}
