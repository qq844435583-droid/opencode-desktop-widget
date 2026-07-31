<p align="center">
  <img src="assets/readme/hero.svg" alt="OpenCode Desktop Widget" width="100%">
</p>

<p align="center">
  <a href="../../releases"><img alt="Latest release" src="https://img.shields.io/github/v/release/qq844435583-droid/opencode-desktop-widget?style=for-the-badge&color=7259ff"></a>
  <img alt="Windows" src="https://img.shields.io/badge/Windows-10%20%7C%2011-1677ff?style=for-the-badge&logo=windows11&logoColor=white">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-512bd4?style=for-the-badge&logo=dotnet&logoColor=white">
  <img alt="WebView2" src="https://img.shields.io/badge/WebView2-Edge-0f6cbd?style=for-the-badge&logo=microsoftedge&logoColor=white">
</p>

<p align="center">
  <b>A lightweight Windows desktop widget for monitoring OpenCode usage in real time.</b><br>
  一款轻量级 Windows 桌面挂件，实时监控 OpenCode 使用量。<br>
  Rolling, weekly, and monthly quotas · Recent requests · Model OK/NG alerts · English and Chinese<br>
  滚动 / 每周 / 每月额度 · 最近调用记录 · 模型 OK/NG 提醒 · 中英文双语界面
</p>

<p align="center">
  <a href="#preview">Preview 预览</a> ·
  <a href="#features">Features 功能</a> ·
  <a href="#download">Download 下载</a> ·
  <a href="#quick-start">Quick Start 快速开始</a> ·
  <a href="#build-from-source">Build 构建</a> ·
  <a href="#changelog">Changelog 更新日志</a>
</p>

---

## Preview · 界面预览

<p align="center">
  <img src="assets/readme/widget-expanded.svg" alt="Expanded English interface" width="520">
</p>

<p align="center">
  <b>Expanded Pro interface</b> — quotas, request history, token totals, model status, and account controls in one view.<br>
  <b>Pro 展开界面</b> — 额度、调用记录、Token 统计、模型状态和账户控制一屏展示。
</p>

<p align="center">
  <img src="assets/readme/widget-compact.svg" alt="Compact English interface" width="760">
</p>

<p align="center">
  <b>Compact Free interface</b> — a small always-available overview that stays out of your way.<br>
  <b>免费版紧凑界面</b> — 小巧常驻的概览视图，不打扰你的工作。
</p>

## Features · 主要功能

**English:**

- **Live quota monitoring** — rolling, weekly, and monthly usage with reset times.
- **Recent request history** — view the latest 50 calls and token totals.
- **Model OK / NG rules** — wildcard matching, desktop notifications, and window flashing.
- **Desktop-friendly behavior** — always on top, click-through, edge auto-hide, and tray controls.
- **Fast and lightweight** — C# WinForms + Microsoft Edge WebView2, without bundling Chromium.
- **Bilingual UI** — switch between English and Simplified Chinese.
- **Multiple accounts** — save and switch between OpenCode accounts.
- **CSV export** — export usage records for your own analysis.
- **Secure local storage** — sign-in credentials are encrypted with Windows DPAPI.

**中文：**

- **实时额度监控** — 滚动、每周、每月用量及重置时间一目了然。
- **最近调用记录** — 查看最近 50 次调用及 Token 统计。
- **模型 OK / NG 规则** — 支持通配符匹配、桌面通知和窗口闪烁提醒。
- **桌面友好交互** — 始终置顶、鼠标穿透、贴边自动隐藏、托盘菜单控制。
- **轻量快速** — C# WinForms + Microsoft Edge WebView2，不内置 Chromium 浏览器。
- **中英文双语界面** — 一键切换简体中文和英文。
- **多账户管理** — 保存并切换多个 OpenCode 账户。
- **CSV 导出** — 导出使用记录，方便自行分析。
- **本地安全存储** — 登录凭据使用 Windows DPAPI 加密保存。

## Free and Pro · 免费版与 Pro 版

| Feature 功能 | Free 免费版 | Pro 专业版 |
|---|:---:|:---:|
| Compact quota overview 紧凑额度概览 | ✓ | ✓ |
| Model OK / NG status 模型 OK/NG 状态 | ✓ | ✓ |
| Full expanded interface 完整展开界面 | — | ✓ |
| Recent request history 最近调用记录 | — | ✓ |
| Detailed token statistics 详细 Token 统计 | — | ✓ |
| One-time purchase 一次性买断 | — | **US$2** |

## Download · 下载

1. Open the [Releases](../../releases) page.
2. Download the newest `OpenCode-Desktop-Widget-Protected-client.zip`.
3. Extract the ZIP and run `OpenCode.Desktop.Widget.exe`.

1. 打开 [Releases](../../releases) 发布页。
2. 下载最新的 `OpenCode-Desktop-Widget-Protected-client.zip`。
3. 解压后运行 `OpenCode.Desktop.Widget.exe`。

No installer is required. · 无需安装，解压即用。

### Requirements · 系统要求

- Windows 10 or Windows 11 · Windows 10 或 Windows 11
- Microsoft Edge WebView2 Runtime · Microsoft Edge WebView2 运行时
- Internet access for OpenCode sign-in and usage synchronization · 需要联网登录 OpenCode 并同步用量数据

> WebView2 is included with Windows 11 and is normally installed with Microsoft Edge on Windows 10.
>
> Windows 11 自带 WebView2；Windows 10 通常随 Microsoft Edge 一并安装。

## Quick Start · 快速开始

1. Launch the widget. It starts in compact Free mode. · 启动挂件，默认进入紧凑版免费模式。
2. Select **Web sign-in** and complete your OpenCode login. · 点击**网页登录**，完成 OpenCode 登录。
3. Your workspace, quota data, and recent requests are detected automatically. · 工作区、额度和最近调用记录自动读取。
4. Use the tray menu to show, hide, refresh, expand, collapse, open settings, or exit. · 通过托盘菜单显示 / 隐藏、刷新、展开 / 收起、打开设置或退出。
5. Select the lock icon to purchase Pro or enter a license key. · 点击锁形图标购买 Pro 版或输入授权码。

## License behavior · 许可说明

- One license supports up to **2 devices** by default. · 单个授权默认支持最多 **2 台设备**。
- Pro sessions renew automatically every **7 days**. · Pro 授权每 **7 天**自动续期。
- A **14-day offline grace period** keeps Pro available without a network connection. · 断网状态下享有 **14 天离线宽限期**，Pro 功能持续可用。
- Refunded, revoked, or unbound licenses return the widget to compact Free mode. · 退款、撤销或解绑的授权会自动降回紧凑版免费模式。
- Legacy license keys remain supported and migrate automatically. · 旧版授权码仍然兼容，并自动迁移。

## Build from Source · 从源码构建

### Requirements · 构建要求

- Windows 10/11
- .NET 8 SDK or Visual Studio 2022 · .NET 8 SDK 或 Visual Studio 2022
- Microsoft Edge WebView2 Runtime

Run one of the included scripts: · 运行以下任一脚本：

```bat
build.bat
```

Framework-dependent build. Output: · 框架依赖构建，输出目录：

```text
publish\win-x64\
```

Or create a self-contained build: · 或生成自包含构建：

```bat
build-self-contained.bat
```

The project pins `Microsoft.Web.WebView2` to version `1.0.4078.44`. · 项目将 `Microsoft.Web.WebView2` 固定为 `1.0.4078.44` 版本。

## Configuration · 配置文件

`store.json`, located next to the executable, controls the product name, license server, purchase link, and support email: · `store.json` 位于可执行文件同目录，用于配置产品名、授权服务器、购买链接和支持邮箱：

```json
{
  "productName": "OpenCode Desktop Widget Pro",
  "licenseServerUrl": "https://your-license-server.example.com",
  "purchaseUrl": "https://buy.stripe.com/your-payment-link",
  "supportEmail": "support@example.com"
}
```

User settings are stored at: · 用户设置保存在：

```text
%APPDATA%\OpenCode Desktop Widget\config.json
```

## Security and privacy · 安全与隐私

- OpenCode credentials are encrypted locally with Windows DPAPI. · OpenCode 凭据使用 Windows DPAPI 在本地加密。
- The application does not bundle a separate Chromium browser. · 应用不捆绑独立的 Chromium 浏览器。
- Usage data is displayed locally by the desktop client. · 用量数据由桌面客户端本地展示。
- Review the source code and build it yourself when stronger verification is required. · 如需更强验证，可审查源码并自行构建。

## Changelog · 更新日志

- **v3.4.3** — revocable server licenses, 2-device limit, 7-day renewal, 14-day offline grace period, minimum-version control. · 可撤销的服务端授权、2 台设备限制、7 天自动续期、14 天离线宽限期、最低版本控制。
- **v3.3.0** — automatic Stripe license delivery after payment. · 支付后自动通过 Stripe 发放授权。
- **v3.2.0** — English and Chinese interface. · 中英文双语界面。
- **v3.1.0** — Free compact mode and paid expanded Pro mode. · 免费紧凑模式与付费展开 Pro 模式。
- **v3.0.x** — WebView2 rewrite and Windows scaling fixes. · WebView2 重写并修复 Windows 缩放问题。

## Disclaimer · 免责声明

This project is provided as-is without warranty. OpenCode names and services belong to their respective owners. Use of OpenCode data is subject to OpenCode's terms of service.

本项目按现状提供，不作任何担保。OpenCode 名称及服务归其各自所有者所有。使用 OpenCode 数据须遵守 OpenCode 服务条款。
