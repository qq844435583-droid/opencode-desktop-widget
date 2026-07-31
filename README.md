# OpenCode Desktop Widget

Windows 桌面挂件：在桌面边角实时显示你的 OpenCode 用量（滚动/周/月额度、最近 50 条调用、模型状态），NG 模型告警，中英双语。

- 基于 C# WinForms + Microsoft Edge WebView2（不再随包内置整个 Chromium）
- 登录凭据使用 Windows DPAPI 加密保存
- 免费版为紧凑收起模式；专业版解锁完整展开界面

## 下载与安装（开箱即用）

1. 前往 [Releases](https://github.com/qq844435583-droid/opencode-desktop-widget/releases) 下载最新的
   `OpenCode-Desktop-Widget-Protected-client.zip`（免安装，解压即可运行）。
2. 系统要求：Windows 10/11，需已安装 **Microsoft Edge WebView2 Runtime**
   （Win11 自带；Win10 一般随 Edge 自动安装，缺失时 [点此下载](https://developer.microsoft.com/microsoft-edge/webview2/)）。
3. 解压后双击 `OpenCode.Desktop.Widget.exe` 启动。

## 快速开始

- 启动后为免费紧凑模式；点击标题栏锁图标可**购买专业版**或**输入授权码**。
- 购买后自动解锁：点击挂件上的展开按钮即可使用完整界面。
- 首次使用点击 **网页登录**，完成 OpenCode 登录后自动捕获工作区与额度数据。
- 托盘图标提供：显示/隐藏挂件、立即刷新、展开/收起、设置、退出。

## 授权说明

- 一个授权默认最多绑定 **2 台设备**。
- 专业版会话每 7 天自动联网续签；断网期间有 14 天离线宽限期，宽限内保持专业版功能。
- 授权被吊销、退款或设备解绑后，客户端会回到免费紧凑模式。
- 旧版授权码仍可正常激活，自动迁移到服务器授权记录。

## 从源码构建（可选）

```text
需求：Windows 10/11、.NET 8 SDK、Microsoft Edge WebView2 Runtime
```

双击 `build.bat`（框架依赖版）或 `build-self-contained.bat`（免装 .NET），输出在 `publish\win-x64\`。
项目已固定 `Microsoft.Web.WebView2 1.0.4078.44`。

## 配置

`store.json`（EXE 同目录）可配置授权服务器地址与付款链接：

```json
{
  "productName": "OpenCode Desktop Widget Pro",
  "licenseServerUrl": "https://license-server-vercel-rho.vercel.app",
  "purchaseUrl": "https://buy.stripe.com/test_28EdR1cKTgH4c8mbI0e7m01",
  "supportEmail": ""
}
```

配置文件位置：`%APPDATA%\OpenCode Desktop Widget\config.json`（兼容旧版 Electron 数据，自动迁移）。

## 功能

- 实时用量：滚动/周/月额度百分比、重置时间、最近 50 条调用明细
- 模型 OK/NG 规则（支持通配符），NG 模型触发桌面通知 + 窗口闪烁
- 贴边自动隐藏、始终置顶、鼠标穿透、自动刷新（10s–24h 可调）
- 中英双语一键切换；CSV 导出使用记录
- 多账户管理与切换

## 版本历史

- **v3.4.3**：服务器可吊销授权、两设备限制、7 天续签、14 天离线宽限、最低版本控制。
- **v3.3.0**：Stripe 自动解锁——付款后自动签发授权，无需人工发码。
- **v3.2.0**：中英双语界面。
- **v3.1.0**：付费解锁专业版（紧凑/展开模式）。
- **v3.0.x**：WebView2 重写（替代 Electron），缩放修复。

## 免责声明

本软件按现状提供，不包含任何形式的明示或默示担保。使用 OpenCode 数据请遵守 OpenCode 服务条款。
