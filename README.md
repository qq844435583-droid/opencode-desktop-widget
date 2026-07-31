# OpenCode Desktop Widget

Windows desktop widget that shows your OpenCode usage in real time (rolling / weekly / monthly quotas, latest 50 calls, model status) at the edge of your screen, with NG-model alerts and a bilingual interface.

Windows 桌面挂件：在桌面边角实时显示你的 OpenCode 用量（滚动/周/月额度、最近 50 条调用、模型状态），NG 模型告警，中英双语。

- Built with C# WinForms + Microsoft Edge WebView2 (no bundled Chromium)
  基于 C# WinForms + Microsoft Edge WebView2（不再随包内置整个 Chromium）
- Sign-in credentials encrypted with Windows DPAPI
  登录凭据使用 Windows DPAPI 加密保存
- Free edition stays in compact collapsed mode; Pro unlocks the full expanded UI
  免费版为紧凑收起模式；专业版解锁完整展开界面

## Download & Install (ready to use) / 下载与安装（开箱即用）

1. Download the latest `OpenCode-Desktop-Widget-Protected-client.zip` from
   [Releases](https://github.com/qq844435583-droid/opencode-desktop-widget/releases).
   Extract and run — no installation needed.
   前往 [Releases](https://github.com/qq844435583-droid/opencode-desktop-widget/releases)
   下载最新的 `OpenCode-Desktop-Widget-Protected-client.zip`（免安装，解压即可运行）。
2. Requirements: Windows 10/11 with **Microsoft Edge WebView2 Runtime**
   (built into Win11; on Win10 it usually installs with Edge — if missing,
   [download here](https://developer.microsoft.com/microsoft-edge/webview2/)).
   系统要求：Windows 10/11，需已安装 **Microsoft Edge WebView2 Runtime**
   （Win11 自带；Win10 一般随 Edge 自动安装，缺失时[点此下载](https://developer.microsoft.com/microsoft-edge/webview2/)）。
3. Double-click `OpenCode.Desktop.Widget.exe` to start.
   解压后双击 `OpenCode.Desktop.Widget.exe` 启动。

## Quick Start / 快速开始

- The widget starts in free compact mode; click the lock icon in the title bar to
  **buy Pro** or **enter a license key**. 启动后为免费紧凑模式；点击标题栏锁图标可**购买专业版**或**输入授权码**。
- After purchase it unlocks automatically: click the expand button to use the full UI.
  购买后自动解锁：点击挂件上的展开按钮即可使用完整界面。
- First run: click **Web login** to sign in to OpenCode; your workspace and usage
  data are captured automatically. 首次使用点击**网页登录**，完成 OpenCode 登录后自动捕获工作区与额度数据。
- Tray icon menu: show/hide widget, refresh now, expand/collapse, settings, exit.
  托盘图标提供：显示/隐藏挂件、立即刷新、展开/收起、设置、退出。

## License / 授权说明

- One license binds up to **2 devices** by default. 一个授权默认最多绑定 **2 台设备**。
- Pro sessions renew automatically every 7 days; a 14-day offline grace period keeps
  Pro working without network. 专业版会话每 7 天自动联网续签；断网期间有 14 天离线宽限期，宽限内保持专业版功能。
- Revoked, refunded, or unbound licenses return the widget to free compact mode.
  授权被吊销、退款或设备解绑后，客户端会回到免费紧凑模式。
- Legacy license keys still work and migrate automatically to server records.
  旧版授权码仍可正常激活，自动迁移到服务器授权记录。

## Build from Source (optional) / 从源码构建（可选）

```text
Requirements: Windows 10/11, .NET 8 SDK, Microsoft Edge WebView2 Runtime
需求：Windows 10/11、.NET 8 SDK、Microsoft Edge WebView2 Runtime
```

Run `build.bat` (framework-dependent) or `build-self-contained.bat` (no .NET needed
to run); output goes to `publish\win-x64\`. WebView2 is pinned to `1.0.4078.44`.
双击 `build.bat`（框架依赖版）或 `build-self-contained.bat`（免装 .NET），输出在 `publish\win-x64\`。
项目已固定 `Microsoft.Web.WebView2 1.0.4078.44`。

## Configuration / 配置

`store.json` (next to the EXE) configures the license server and purchase link.
`store.json`（EXE 同目录）可配置授权服务器地址与付款链接：

```json
{
  "productName": "OpenCode Desktop Widget Pro",
  "licenseServerUrl": "https://license-server-vercel-rho.vercel.app",
  "purchaseUrl": "https://buy.stripe.com/test_28EdR1cKTgH4c8mbI0e7m01",
  "supportEmail": ""
}
```

User data lives in `%APPDATA%\OpenCode Desktop Widget\config.json`
(legacy Electron data is migrated automatically).
配置文件位置：`%APPDATA%\OpenCode Desktop Widget\config.json`（兼容旧版 Electron 数据，自动迁移）。

## Features / 功能

- Real-time usage: rolling/weekly/monthly percentages, reset time, latest 50 calls
  实时用量：滚动/周/月额度百分比、重置时间、最近 50 条调用明细
- Model OK/NG rules (wildcards supported) with desktop notification + window flash
  模型 OK/NG 规则（支持通配符），NG 模型触发桌面通知 + 窗口闪烁
- Edge auto-hide, always on top, click-through, auto refresh (10s–24h)
  贴边自动隐藏、始终置顶、鼠标穿透、自动刷新（10s–24h 可调）
- Simplified Chinese / English switch; CSV export of usage records
  中英双语一键切换；CSV 导出使用记录
- Multiple account management and switching
  多账户管理与切换

## Changelog / 版本历史

- **v3.4.3** — Revocable server license, 2-device limit, 7-day renewal, 14-day offline grace, minimum version control.
  服务器可吊销授权、两设备限制、7 天续签、14 天离线宽限、最低版本控制。
- **v3.3.0** — Stripe auto-unlock: license issued automatically after payment, no manual delivery.
  Stripe 自动解锁——付款后自动签发授权，无需人工发码。
- **v3.2.0** — Bilingual UI. 中英双语界面。
- **v3.1.0** — Paid Pro edition (compact/expanded modes). 付费解锁专业版（紧凑/展开模式）。
- **v3.0.x** — WebView2 rewrite (replacing Electron), scaling fixes. WebView2 重写（替代 Electron），缩放修复。

## Disclaimer / 免责声明

Provided as-is without warranty of any kind. Use of OpenCode data is subject to OpenCode's terms of service.
本软件按现状提供，不包含任何形式的明示或默示担保。使用 OpenCode 数据请遵守 OpenCode 服务条款。
