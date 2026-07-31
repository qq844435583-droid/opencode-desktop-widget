namespace OpenCodeDesktopWidget;

internal static class AppConstants
{
    public static readonly System.Drawing.Color WindowBackgroundColor = System.Drawing.Color.FromArgb(20, 28, 42);
    public const string AppName = "OpenCode Desktop Widget";
    public const string BaseUrl = "https://opencode.ai";
    public const int WidgetWidth = 350;
    public const int WidgetHeight = 568;
    public const int EdgeGap = 14;
    public const int SnapDistance = 24;
    public const int EdgeReveal = 10;
    public const int DefaultRefreshSeconds = 60;
    public const int MinRefreshSeconds = 10;
    public const int MaxRefreshSeconds = 86_400;
    public const int RequestTimeoutMs = 18_000;
    public const string SummaryHash = "c7389bd0e731f80f49593e5ee53835475f4e28594dd6bd83eb229bab753498cd";
    public const string ListHash = "6262ba54bff26cd7ec162f93db420e0d19df9cd94b2233dfe3b6b24c3f990388";

    public static string AppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppName);

    public static string ConfigPath => Path.Combine(AppDataDirectory, "config.json");
    public static string LicensePath => Path.Combine(AppDataDirectory, "license.dat");
    public static string SessionLicensePath => Path.Combine(AppDataDirectory, "license-session.dat");
    public static string PendingPurchasePath => Path.Combine(AppDataDirectory, "pending-purchase.dat");
    public static string WebViewDataDirectory => Path.Combine(AppDataDirectory, "WebView2");
    public static string ExecutableDirectory => AppContext.BaseDirectory;
    public static string IconPath => Path.Combine(ExecutableDirectory, "assets", "icon.ico");
    public static string StoreConfigPath => Path.Combine(ExecutableDirectory, "store.json");
    public static string RendererDirectory => Path.Combine(ExecutableDirectory, "src", "renderer");
    public static string ScriptsDirectory => Path.Combine(ExecutableDirectory, "scripts");
}
