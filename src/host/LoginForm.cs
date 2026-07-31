using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OpenCodeDesktopWidget;

internal sealed class LoginForm : Form
{
    private readonly Func<AccountInput, Task> _completed;
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private CoreWebView2Environment? _environment;
    private bool _capturing;

    public LoginForm(Func<AccountInput, Task> completed)
    {
        _completed = completed;
        Text = AppText.T("登录 OpenCode", "Sign in to OpenCode");
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1000, 760);
        MinimumSize = new Size(760, 600);
        BackColor = Color.FromArgb(16, 24, 40);
        ShowInTaskbar = true;
        if (File.Exists(AppConstants.IconPath)) Icon = new Icon(AppConstants.IconPath);
        Controls.Add(_webView);
        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(AppConstants.WebViewDataDirectory, "login"));
            await _webView.EnsureCoreWebView2Async(_environment);
            Configure(_webView);
            var cookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync(AppConstants.BaseUrl);
            foreach (var cookie in cookies.Where(item => item.Name == "auth"))
                _webView.CoreWebView2.CookieManager.DeleteCookie(cookie);
            _webView.CoreWebView2.Navigate(AppConstants.BaseUrl + "/workspace");
        }
        catch (Exception error)
        {
            MessageBox.Show(this, AppText.T($"登录页面加载失败：{error.Message}", $"The sign-in page failed to load: {error.Message}"), AppConstants.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Configure(WebView2 webView)
    {
        webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        webView.CoreWebView2.SourceChanged += (_, _) => _ = TryCaptureSafeAsync(webView);
        webView.CoreWebView2.NavigationCompleted += (_, _) => _ = TryCaptureSafeAsync(webView);
        webView.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            OpenPopup(args.Uri);
        };
    }

    private void OpenPopup(string uri)
    {
        if (_environment is null) return;
        var popup = new Form
        {
            Text = AppText.T("OpenCode 登录", "OpenCode sign-in"),
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(700, 760),
            MinimumSize = new Size(560, 600)
        };
        if (File.Exists(AppConstants.IconPath)) popup.Icon = new Icon(AppConstants.IconPath);
        var child = new WebView2 { Dock = DockStyle.Fill };
        popup.Controls.Add(child);
        popup.Shown += async (_, _) =>
        {
            try
            {
                await child.EnsureCoreWebView2Async(_environment);
                Configure(child);
                child.CoreWebView2.Navigate(uri);
            }
            catch (Exception error)
            {
                MessageBox.Show(popup, AppText.T($"登录弹窗加载失败：{error.Message}", $"The sign-in popup failed to load: {error.Message}"), AppConstants.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                popup.Close();
            }
        };
        popup.Show(this);
    }

    private async Task TryCaptureSafeAsync(WebView2 source)
    {
        try { await TryCaptureAsync(source); }
        catch (Exception error)
        {
            if (!IsDisposed) MessageBox.Show(this, error.Message, AppConstants.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task TryCaptureAsync(WebView2 source)
    {
        if (_capturing || source.CoreWebView2 is null) return;
        var workspaceId = ParseWorkspaceId(source.Source?.ToString()) ?? ParseWorkspaceId(_webView.Source?.ToString());
        if (workspaceId is null) return;
        var cookies = await source.CoreWebView2.CookieManager.GetCookiesAsync(AppConstants.BaseUrl);
        var auth = cookies.FirstOrDefault(item => item.Name == "auth")?.Value ?? "";
        if (auth.Length < 20) return;

        _capturing = true;
        try
        {
            await _completed(new AccountInput { Name = "Main", WorkspaceId = workspaceId, Auth = auth });
            BeginInvoke(new Action(Close));
        }
        catch (Exception error)
        {
            _capturing = false;
            MessageBox.Show(this, error.Message, AppConstants.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string? ParseWorkspaceId(string? url)
    {
        var match = Regex.Match(url ?? "", "/workspace/(wrk_[A-Za-z0-9_-]+)");
        return match.Success ? match.Groups[1].Value : null;
    }
}
