namespace OpenCodeDesktopWidget;

internal static class AppText
{
    private static string _language = "zh-CN";

    public static string Language => _language;
    public static bool IsEnglish => _language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    public static void SetLanguage(string? language) => _language = NormalizeLanguage(language);

    public static string NormalizeLanguage(string? language) =>
        (language ?? "").StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en-US" : "zh-CN";

    public static string T(string chinese, string english) => IsEnglish ? english : chinese;
}
