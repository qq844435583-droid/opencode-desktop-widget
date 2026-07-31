using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OpenCodeDesktopWidget;

internal sealed class ScrapeService
{
    private CoreWebView2Environment? _environment;

    public async Task<UsageData> ScrapeAsync(AccountCredentials account, CancellationToken cancellationToken = default)
    {
        _environment ??= await CoreWebView2Environment.CreateAsync(null, Path.Combine(AppConstants.WebViewDataDirectory, "monitor"));
        using var host = new Form
        {
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Size = new Size(16, 16)
        };
        using var webView = new WebView2 { Dock = DockStyle.Fill };
        host.Controls.Add(webView);
        host.Show();
        await webView.EnsureCoreWebView2Async(_environment);
        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

        var cookieManager = webView.CoreWebView2.CookieManager;
        var cookie = cookieManager.CreateCookie("auth", account.Auth, ".opencode.ai", "/");
        cookie.IsSecure = true;
        cookie.IsHttpOnly = true;
        cookie.SameSite = CoreWebView2CookieSameSiteKind.None;
        cookieManager.AddOrUpdateCookie(cookie);

        var navigation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Completed(object? _, CoreWebView2NavigationCompletedEventArgs eventArgs)
        {
            if (eventArgs.IsSuccess) navigation.TrySetResult(true);
            else navigation.TrySetException(new InvalidOperationException(AppText.T($"网页备用通道加载失败：{eventArgs.WebErrorStatus}", $"The web fallback failed to load: {eventArgs.WebErrorStatus}")));
        }
        webView.CoreWebView2.NavigationCompleted += Completed;
        webView.CoreWebView2.Navigate($"{AppConstants.BaseUrl}/workspace/{account.WorkspaceId}/go");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(15_000);
            await navigation.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(AppText.T("网页备用通道加载超时。", "The web fallback timed out while loading."));
        }
        finally { webView.CoreWebView2.NavigationCompleted -= Completed; }

        await Task.Delay(1200, cancellationToken);
        var currentUrl = webView.Source?.ToString() ?? "";
        if (currentUrl.Contains("/auth/", StringComparison.OrdinalIgnoreCase) ||
            !currentUrl.Contains($"/workspace/{account.WorkspaceId}", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(AppText.T("网页登录状态已失效，请重新登录。", "The web sign-in session has expired. Please sign in again."));

        var metricsScript = await File.ReadAllTextAsync(Path.Combine(AppConstants.ScriptsDirectory, "metrics.js"), cancellationToken);
        var recordsScript = await File.ReadAllTextAsync(Path.Combine(AppConstants.ScriptsDirectory, "records.js"), cancellationToken);
        var metricsJson = await webView.CoreWebView2.ExecuteScriptAsync(metricsScript);
        var recordsJson = await webView.CoreWebView2.ExecuteScriptAsync(recordsScript);
        var summary = ParseMetrics(metricsJson);
        var records = ParseRecords(recordsJson);
        return new UsageData
        {
            WorkspaceId = account.WorkspaceId,
            Summary = summary,
            Records = records,
            Detail = OpenCodeParser.Summarize(records),
            Source = "web-fallback",
            FetchedAt = DateTime.UtcNow.ToString("O")
        };
    }

    private static UsageSummary ParseMetrics(string json)
    {
        using var document = JsonDocument.Parse(json);
        var summary = new UsageSummary { IsMine = true };
        if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException(AppText.T("网页已打开，但无法识别用量区域。", "The page opened, but the usage section could not be recognized."));
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("key", out var keyElement)) continue;
            if (!item.TryGetProperty("percent", out var percentElement) || percentElement.ValueKind != JsonValueKind.Number) continue;
            var percent = Math.Clamp(percentElement.GetDouble(), 0, 100);
            var metric = new UsageMetric
            {
                UsedPercent = percent,
                UsagePercent = percent,
                RemainingPercent = Math.Clamp(100 - percent, 0, 100),
                ResetText = item.TryGetProperty("reset", out var reset) ? reset.GetString() ?? "" : ""
            };
            switch (keyElement.GetString())
            {
                case "rolling": summary.Rolling = metric; break;
                case "weekly": summary.Weekly = metric; break;
                case "monthly": summary.Monthly = metric; break;
            }
        }
        if (summary.Rolling is null && summary.Weekly is null && summary.Monthly is null)
            throw new InvalidOperationException(AppText.T("网页已打开，但无法识别用量区域。", "The page opened, but the usage section could not be recognized."));
        return summary;
    }

    private static List<UsageRecord> ParseRecords(string json)
    {
        using var document = JsonDocument.Parse(json);
        var output = new List<UsageRecord>();
        if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) return output;
        var index = 0;
        foreach (var item in items.EnumerateArray())
        {
            var time = GetString(item, "time");
            var model = GetString(item, "model");
            output.Add(new UsageRecord
            {
                Id = $"scraped_{index++}_{time}_{model}",
                Time = time,
                Model = string.IsNullOrWhiteSpace(model) ? "unknown" : model,
                Session = GetString(item, "session"),
                InputTokens = ParseLooseNumber(GetString(item, "input")),
                OutputTokens = ParseLooseNumber(GetString(item, "output")),
                Cost = ParseLooseNumber(GetString(item, "cost"))
            });
            if (output.Count >= 50) break;
        }
        return output;
    }

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) ? property.GetString() ?? "" : "";

    private static double ParseLooseNumber(string value)
    {
        var normalized = Regex.Replace(value ?? "", "[^0-9.\\-]", "");
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : 0;
    }
}
