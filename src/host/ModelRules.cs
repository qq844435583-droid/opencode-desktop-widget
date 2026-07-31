using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenCodeDesktopWidget;

internal static class ModelRules
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static List<string> Normalize(IEnumerable<string>? values)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var item in values ?? [])
        {
            var rule = (item ?? "").Trim();
            if (string.IsNullOrEmpty(rule) || !seen.Add(rule)) continue;
            result.Add(rule[..Math.Min(160, rule.Length)]);
            if (result.Count >= 200) break;
        }
        return result;
    }

    public static bool Matches(string? model, string? rule)
    {
        var subject = (model ?? "").Trim();
        var pattern = (rule ?? "").Trim();
        if (subject.Length == 0 || pattern.Length == 0) return false;
        try
        {
            var expression = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(subject, expression, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch { return string.Equals(subject, pattern, StringComparison.OrdinalIgnoreCase); }
    }

    public static string Classify(string? model, IEnumerable<string>? okRules, IEnumerable<string>? ngRules)
    {
        if (Normalize(ngRules).Any(rule => Matches(model, rule))) return "ng";
        if (Normalize(okRules).Any(rule => Matches(model, rule))) return "ok";
        return "unknown";
    }

    public static string Signature(IEnumerable<string>? okRules, IEnumerable<string>? ngRules)
    {
        return JsonSerializer.Serialize(new RuleSignaturePayload
        {
            Ok = Normalize(okRules).Select(item => item.ToLowerInvariant()).ToList(),
            Ng = Normalize(ngRules).Select(item => item.ToLowerInvariant()).ToList()
        }, JsonOptions);
    }

    public static (List<UsageRecord> NewRecords, List<string> AlertedKeys) FindNewNgRecords(
        IEnumerable<UsageRecord>? records,
        IEnumerable<string>? okRules,
        IEnumerable<string>? ngRules,
        IEnumerable<string>? alertedKeys)
    {
        var known = new HashSet<string>(alertedKeys ?? []);
        var next = known.ToList();
        var found = new List<UsageRecord>();
        foreach (var record in records ?? [])
        {
            if (Classify(record.Model, okRules, ngRules) != "ng") continue;
            var key = RecordKey(record);
            if (key.Length == 0 || !known.Add(key)) continue;
            next.Add(key);
            found.Add(record);
        }
        return (found, next.TakeLast(300).ToList());
    }

    private static string RecordKey(UsageRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Id)) return "id:" + record.Id.Trim();
        return string.Join('|', new[] { record.Time, record.Model, record.Provider, record.Session, record.Cost.ToString(CultureInfo.InvariantCulture) });
    }
}
