# OpenCode Desktop Widget · WebView2 Edition

<p align="center">
  <img src="assets/readme/icon.png" alt="OpenCode Desktop Widget icon" width="96" />
</p>

<p align="center">
  轻量级 Windows 桌面挂件，用于查看 OpenCode 用量、最近调用记录与模型状态。<br>
  A lightweight Windows desktop widget for monitoring OpenCode usage, recent requests, and model status.
</p>

<p align="center">
  <img alt="Windows" src="https://img.shields.io/badge/Windows-10%2F11-0078D4?logo=windows&logoColor=white">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white">
  <img alt="WebView2" src="https://img.shields.io/badge/WebView2-Microsoft%20Edge-0F6CBD?logo=microsoftedge&logoColor=white">
  <img alt="UI" src="https://img.shields.io/badge/UI-HTML%20%2B%20CSS%20%2B%20JavaScript-111827">
</p>

## 预览 / Preview

以下英文截图直接由当前仓库中的真实界面文件 `src/renderer` 渲染，并使用项目内置的演示数据。它们不是生成图片，也不是重新绘制的示意图。  
The English screenshots below are rendered directly from the real `src/renderer` UI in this repository using the project's built-in demo data. They are not AI-generated or redrawn mockups.

### 展开模式 / Expanded Mode

![OpenCode Desktop Widget expanded mode](assets/readme/screenshot-expanded.png)

### 收起模式 / Compact Mode

![OpenCode Desktop Widget compact mode](assets/readme/screenshot-compact.png)

## 功能特性 / Features

- **三段额度概览**：显示滚动额度、每周额度、每月额度，以及各自的重置倒计时。  
  **Three usage periods:** View rolling, weekly, and monthly usage together with the reset countdown for each period.

- **最近调用记录**：展示最近 50 条调用记录，并支持分页浏览。  
  **Recent request history:** Review the latest 50 requests with built-in pagination.

- **模型状态标记**：根据自定义规则显示 `OK`、`NG` 或中性状态，便于快速确认当前调用的模型。  
  **Model status labels:** Apply customizable rules to mark models as `OK`, `NG`, or neutral for quick identification.

- **紧凑模式**：将窗口收起成更小的桌面挂件，同时保留额度与最近模型状态。  
  **Compact mode:** Collapse the window into a smaller desktop widget while keeping usage and recent model status visible.

- **桌面增强功能**：支持窗口置顶、贴边隐藏、自动展开以及系统托盘驻留。  
  **Desktop integration:** Includes always-on-top, edge hiding, automatic reveal, and system tray support.

- **网页登录**：通过 Microsoft Edge WebView2 完成 OpenCode 登录，并读取所需的登录 Cookie。  
  **Web sign-in:** Sign in to OpenCode through Microsoft Edge WebView2 and capture the required authentication cookie.

- **提醒与模型规则**：支持剩余额度阈值提醒、自定义 OK/NG 模型规则以及 NG 模型警报。  
  **Alerts and model rules:** Configure low-usage warnings, custom OK/NG model rules, and NG model notifications.

- **安全配置存储**：登录凭据通过 Windows DPAPI 加密，并存储在用户配置目录。  
  **Secure configuration storage:** Authentication data is encrypted with Windows DPAPI and stored in the user's configuration directory.

## 项目说明 / About

这个版本将原先的 Electron/Chromium 宿主替换为 **C# WinForms + Microsoft Edge WebView2**。界面继续使用原有的 **HTML、CSS 和 JavaScript**，但发布包不再需要附带完整的 Chromium。  
This edition replaces the previous Electron/Chromium host with **C# WinForms + Microsoft Edge WebView2**. The interface continues to use the existing **HTML, CSS, and JavaScript**, without bundling a full Chromium runtime with the application.

主挂件、系统托盘、窗口置顶、鼠标穿透和贴边隐藏均由 WinForms 原生实现。  
The main widget, system tray integration, always-on-top behavior, click-through handling, and edge hiding are implemented natively with WinForms.

## 技术栈 / Technology Stack

- **宿主 / Host:** C# · WinForms · .NET 8
- **嵌入式浏览器 / Embedded browser:** Microsoft Edge WebView2
- **界面 / Interface:** HTML · CSS · JavaScript
- **数据脚本 / Data scripts:** `scripts/metrics.js` and `scripts/records.js`
- **安全存储 / Secure storage:** Windows DPAPI
- **配置路径 / Configuration path:** `%APPDATA%\OpenCode Desktop Widget\config.json`

## 开发与编译 / Development and Build

### 环境要求 / Requirements

1. Windows 10 或 Windows 11。  
   Windows 10 or Windows 11.
2. Visual Studio 2022，或者 .NET 8 SDK。  
   Visual Studio 2022 or the .NET 8 SDK.
3. Microsoft Edge WebView2 Runtime。  
   Microsoft Edge WebView2 Runtime.

项目固定使用以下 WebView2 版本：  
The project currently pins the following WebView2 package version:

```text
Microsoft.Web.WebView2 1.0.4078.44
```

### 编译普通版本 / Build the Standard Version

普通构建生成较小的发布包，但目标电脑需要安装 .NET 8 Desktop Runtime。  
The standard build produces a smaller package, but the target computer must have the .NET 8 Desktop Runtime installed.

```bat
build.bat
```

输出目录如下：  
The output is written to:

```text
publish\win-x64\
```

### 编译独立版本 / Build the Self-Contained Version

独立版本不要求目标电脑预先安装 .NET Runtime，但发布包会更大。  
The self-contained build does not require a preinstalled .NET Runtime, but the resulting package is larger.

```bat
build-self-contained.bat
```

### 启动程序 / Run the Application

```text
publish\win-x64\OpenCode.Desktop.Widget.exe
```

首次使用时点击 **“网页登录”**。完成 OpenCode 登录后，程序会自动捕获工作区信息和登录 Cookie。  
On first launch, select **“Sign in on the web.”** After the OpenCode sign-in is complete, the application automatically captures the workspace information and authentication cookie.

## 配置兼容 / Configuration Compatibility

配置文件继续保存在以下位置：  
The configuration file remains at:

```text
%APPDATA%\OpenCode Desktop Widget\config.json
```

- 旧 Electron 版的 `plain:` 凭据可以直接读取。  
  Existing `plain:` credentials from the Electron edition can be read directly.
- `enc:` 凭据会尝试使用 Windows DPAPI 解密。  
  Existing `enc:` credentials are decrypted through Windows DPAPI when possible.
- 如果旧版加密格式不兼容，程序会提示用户重新登录。  
  If an older encrypted format is incompatible, the application asks the user to sign in again.

## v3.0.1 修复内容 / Fixes in v3.0.1

- 修复 Windows 125%、150% 和 175% 缩放下 WebView2 内容被放大或裁切的问题。  
  Fixed WebView2 content being enlarged or clipped at 125%, 150%, and 175% Windows scaling.
- 修复完整模式底部分页栏和账户栏不可见的问题。  
  Fixed the pagination and account areas being hidden in expanded mode.
- 修复标题栏刷新倒计时被按钮遮挡的问题。  
  Fixed the refresh countdown being obscured by title-bar buttons.
- 修复紧凑模式高度错误以及模型行消失的问题。  
  Fixed incorrect compact-mode height and missing model rows.
- 修复 CSS 圆角与原生窗口圆角不一致导致的黑边。  
  Fixed black edges caused by a mismatch between CSS and native window corner radii.

## 项目结构 / Project Structure

```text
assets/           图标与其他资源 / Icons and other assets
scripts/          数据抓取脚本 / Data extraction scripts
src/host/         WinForms 宿主逻辑 / WinForms host logic
src/renderer/     HTML/CSS/JS 界面 / HTML, CSS, and JavaScript interface
publish/win-x64/  发布输出 / Published application output
```

## 授权 / License

请根据实际发布与收费方式，在公开仓库前补充适用的许可证或商业授权说明。  
Before publishing the repository, add the license or commercial-use terms that match your actual distribution and payment model.
