using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenCodeDesktopWidget;

internal static class OpenCodeParser
{
    public static UsageSummary ParseSummary(string raw)
    {
        var summary = new UsageSummary
        {
            Rolling = ExtractUsageBlock(raw, "rollingUsage"),
            Weekly = ExtractUsageBlock(raw, "weeklyUsage"),
            Monthly = ExtractUsageBlock(raw, "monthlyUsage"),
            UseBalance = Regex.IsMatch(raw, "useBalance:!0"),
            IsMine = Regex.IsMatch(raw, "mine:!0")
        };
        if (summary.Rolling is null && summary.Weekly is null && summary.Monthly is null)
            throw new InvalidOperationException(AppText.T("OpenCode 返回的数据中没有可识别的用量信息。", "No recognizable usage information was found in the OpenCode response."));
        return summary;
    }

    private static UsageMetric? ExtractUsageBlock(string text, string property)
    {
        var index = text.IndexOf(property, StringComparison.Ordinal);
        if (index < 0) return null;
        var segment = text.Substring(index, Math.Min(900, text.Length - index));
        var percent = MatchDouble(segment, "usagePercent:(\\d+(?:\\.\\d+)?)", double.NaN);
        if (double.IsNaN(percent)) return null;
        percent = Math.Clamp(percent, 0, 100);
        return new UsageMetric
        {
            Status = MatchValue(segment, "status:\\\"([^\\\"]+)\\\"") ?? "unknown",
            ResetInSec = (int)MatchDouble(segment, "resetInSec:(\\d+)", 0),
            UsedPercent = percent,
            UsagePercent = percent,
            RemainingPercent = Math.Clamp(100 - percent, 0, 100)
        };
    }

    public static List<UsageRecord> ParseRecords(string raw, int limit = 100)
    {
        var matches = Regex.Matches(raw, "id:\\\"(usg_[^\\\"]+)\\\"");
        var records = new List<UsageRecord>();
        for (var index = 0; index < matches.Count && records.Count < limit; index++)
        {
            var start = matches[index].Index;
            var end = index + 1 < matches.Count ? matches[index + 1].Index : Math.Min(raw.Length, start + 2600);
            var segment = raw[start..end];
            var model = MatchJsonString(segment, "model");
            if (model.Length == 0) model = MatchJsonString(segment, "modelID");
            var provider = MatchJsonString(segment, "provider");
            if (provider.Length == 0) provider = MatchJsonString(segment, "providerID");
            var session = MatchJsonString(segment, "sessionID");
            if (session.Length == 0) session = MatchJsonString(segment, "session");
            records.Add(new UsageRecord
            {
                Id = matches[index].Groups[1].Value,
                Time = MatchTimestamp(segment),
                Model = model.Length == 0 ? "unknown" : model,
                Provider = provider,
                Session = session,
                InputTokens = MatchNumber(segment, "inputTokens"),
                OutputTokens = MatchNumber(segment, "outputTokens"),
                ReasoningTokens = MatchNumber(segment, "reasoningTokens"),
                CacheReadTokens = MatchNumber(segment, "cacheReadTokens"),
                CacheWriteTokens = MatchNumber(segment, "cacheWriteTokens"),
                Cost = MatchNumber(segment, "cost"),
                Plan = MatchJsonString(segment, "plan")
            });
        }
        return records;
    }

    public static UsageDetail Summarize(IEnumerable<UsageRecord>? records)
    {
        var detail = new UsageDetail();
        var plans = new HashSet<string>();
        foreach (var record in records ?? [])
        {
            detail.Count++;
            detail.TotalCost += record.Cost;
            detail.TotalInput += record.InputTokens;
            detail.TotalOutput += record.OutputTokens;
            detail.TotalReasoning += record.ReasoningTokens;
            detail.TotalCache += record.CacheReadTokens + record.CacheWriteTokens;
            var model = string.IsNullOrWhiteSpace(record.Model) ? "unknown" : record.Model;
            detail.ModelCounts[model] = detail.ModelCounts.GetValueOrDefault(model) + 1;
            if (!string.IsNullOrWhiteSpace(record.Plan)) plans.Add(record.Plan);
        }
        detail.Plans = plans.ToList();
        return detail;
    }

    public static UsageDetail? ParseAggregate(string raw)
    {
        var ids = Regex.Matches(raw, "id:\\\"(usg_[^\\\"]+)\\\"")
            .Select(match => match.Groups[1].Value).ToHashSet();
        var detail = new UsageDetail { Count = ids.Count };
        foreach (Match match in Regex.Matches(raw, "(?:^|[,\\s{])(?:model|modelID):\\\"((?:\\\\.|[^\\\"])*)\\\""))
        {
            var model = DecodeJsonString(match.Groups[1].Value);
            if (string.IsNullOrWhiteSpace(model)) model = "unknown";
            detail.ModelCounts[model] = detail.ModelCounts.GetValueOrDefault(model) + 1;
        }
        if (detail.Count == 0) detail.Count = detail.ModelCounts.Values.Sum();
        detail.TotalCost = SumNumbers(raw, ["cost"]);
        detail.TotalInput = SumNumbers(raw, ["inputTokens", "input"]);
        detail.TotalOutput = SumNumbers(raw, ["outputTokens", "output"]);
        detail.TotalReasoning = SumNumbers(raw, ["reasoningTokens", "reasoning"]);
        detail.TotalCache = SumNumbers(raw, ["cacheReadTokens", "cacheWriteTokens", "cacheRead", "cacheWrite"]);
        foreach (Match match in Regex.Matches(raw, "(?:^|[,\\s{])plan:\\\"((?:\\\\.|[^\\\"])*)\\\""))
        {
            var plan = DecodeJsonString(match.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(plan) && !detail.Plans.Contains(plan)) detail.Plans.Add(plan);
        }
        var hasUsage = detail.Count > 0 || detail.TotalCost != 0 || detail.TotalInput != 0 ||
                       detail.TotalOutput != 0 || detail.TotalReasoning != 0 || detail.TotalCache != 0;
        return hasUsage ? detail : null;
    }

    private static double SumNumbers(string text, IEnumerable<string> keys)
    {
        double total = 0;
        foreach (var key in keys)
        {
            var pattern = $"(?:^|[,\\s{{]){Regex.Escape(key)}:(-?\\d+(?:\\.\\d+)?)";
            foreach (Match match in Regex.Matches(text, pattern))
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) total += value;
        }
        return total;
    }

    private static string MatchTimestamp(string segment)
    {
        foreach (var key in new[] { "timeCreated", "createdAt", "time", "date", "timeUpdated" })
        {
            var rpcPattern = $"(?:^|[,\\s{{]){Regex.Escape(key)}(?::\\$R\\[\\d+\\])?=new\\s+Date\\(\\\"([^\\\"]*)\\\"\\)";
            var rpc = Regex.Match(segment, rpcPattern);
            if (rpc.Success && DateTimeOffset.TryParse(rpc.Groups[1].Value, out var rpcDate)) return rpcDate.UtcDateTime.ToString("O");

            var numeric = Regex.Match(segment, $"(?:^|[,\\s{{]){Regex.Escape(key)}\\s*:\\s*(\\d{{8,16}})");
            if (numeric.Success && long.TryParse(numeric.Groups[1].Value, out var timestamp))
            {
                try
                {
                    var date = timestamp < 1_000_000_000_000
                        ? DateTimeOffset.FromUnixTimeSeconds(timestamp)
                        : DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                    return date.UtcDateTime.ToString("O");
                }
                catch { }
            }

            var text = MatchJsonString(segment, key);
            if (DateTimeOffset.TryParse(text, out var stringDate)) return stringDate.UtcDateTime.ToString("O");
        }
        return "";
    }

    private static double MatchNumber(string segment, string key) =>
        MatchDouble(segment, $"{Regex.Escape(key)}:(-?\\d+(?:\\.\\d+)?)", 0);

    private static double MatchDouble(string input, string pattern, double fallback)
    {
        var match = Regex.Match(input, pattern);
        return match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value : fallback;
    }

    private static string MatchJsonString(string segment, string key)
    {
        var pattern = $"{Regex.Escape(key)}:\\\"((?:\\\\.|[^\\\"])*)\\\"";
        var match = Regex.Match(segment, pattern);
        return match.Success ? DecodeJsonString(match.Groups[1].Value) : "";
    }

    private static string DecodeJsonString(string encoded)
    {
        try { return JsonSerializer.Deserialize<string>($"\"{encoded}\"") ?? encoded; }
        catch { return encoded; }
    }

    private static string? MatchValue(string input, string pattern)
    {
        var match = Regex.Match(input, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }
}
