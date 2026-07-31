# OpenCode Desktop Widget v3.4.3

本版本加入服务器可吊销授权、最多两台设备、7 天续签、14 天离线宽限和最低版本控制。OpenCode 官网读取的数据保持原文。

# OpenCode Desktop Widget · WebView2 版

这个版本已经把 Electron/Chromium 宿主替换为 **C# WinForms + Microsoft Edge WebView2**。界面仍使用原来的 HTML/CSS/JavaScript，但不再随程序打包一整套 Chrome/Chromium。

## 主要变化

- 删除 Electron 主进程、preload 和 `ipcRenderer`。
- 主挂件、托盘、置顶、鼠标穿透、贴边隐藏改为 WinForms 原生实现。
- OpenCode 登录窗口改为 WebView2，并从 WebView2 CookieManager 读取 `auth` Cookie。
- API 失败时，使用隐藏 WebView2 页面执行 `scripts/metrics.js` 和 `scripts/records.js`。
- 登录凭据使用 Windows DPAPI 加密后保存在原来的配置目录。
- 保留现有 UI、模型 OK/NG 规则、通知、CSV 导出、自动刷新和账户缓存。

## 开发/编译环境

1. Windows 10/11
2. Visual Studio 2022，或 .NET 8 SDK
3. Microsoft Edge WebView2 Runtime

项目已固定使用 `Microsoft.Web.WebView2 1.0.4078.44`。双击 `build.bat`，输出目录：

```text
publish\win-x64\
```

这是依赖 .NET 8 Desktop Runtime 的较小版本。需要无需安装 .NET 的版本时，运行：

```text
build-self-contained.bat
```

## 启动

编译后运行：

```text
publish\win-x64\OpenCode.Desktop.Widget.exe
```

首次使用点击“网页登录”，完成 OpenCode 登录后会自动捕获工作区和登录 Cookie。

## 配置兼容

配置文件继续使用：

```text
%APPDATA%\OpenCode Desktop Widget\config.json
```

旧 Electron 版的 `plain:` 凭据可以直接读取。`enc:` 凭据会尝试使用 Windows DPAPI 解密；若旧版加密格式不兼容，程序会提示重新登录。

## v3.0.1 修复

- 修复 Windows 125% / 150% / 175% 缩放下 WebView2 内容被放大裁切。
- 修复完整模式底部分页和账户栏不可见。
- 修复标题栏刷新秒数被按钮遮挡。
- 修复紧凑模式高度不正确、模型行消失。
- 修复 CSS 圆角与原生窗口圆角不一致造成的黑边。

## v3.1.0 付费解锁

- 免费版启动后强制使用紧凑窗口，无法通过网页脚本、托盘菜单或修改普通设置展开。
- 紧凑窗口标题栏显示黄色锁，点击后可购买或输入授权码。
- 专业版激活后自动展开，并解锁完整额度、最近调用、分页、账户信息和设置面板。
- 使用 ECDSA P-256 签名授权码，客户端仅包含公钥；卖家私钥只保存在 `seller-tools`。
- 授权默认绑定设备，使用 Windows DPAPI 保存在 `%APPDATA%\OpenCode Desktop Widget\license.dat`。
- 详细出售、发码和打包流程见 `README-SELLER.md`。

付款页面可在 `store.json` 配置。该文件会复制到 EXE 同目录，也可以在编译后直接修改发布目录中的 `store.json`，无需重新编译。


## v3.2.0 中英双语

- 标题栏新增 `EN / 中` 快速切换按钮，免费紧凑版也可以直接切换语言。
- 设置页新增“界面语言”，支持简体中文与 English。
- 软件自身的按钮、提示、托盘菜单、授权窗口、登录窗口和通知会随语言切换。
- 从 OpenCode 官网/API 读取的工作区、模型名、计划名、重置说明和会话等文本字段不会被翻译，始终按官网返回内容显示。

## v3.3.0 Stripe 自动解锁

- 客户端向授权服务器申请一次性购买令牌，并将令牌附加到 Stripe Payment Link 的 `client_reference_id`。
- Stripe Webhook 确认支付后，服务器自动签发绑定设备的 ECDSA 授权码。
- 授权窗口每 3 秒轮询付款状态，拿到授权后自动激活并展开，无需卖家人工发码。
- 手动输入授权码仍作为备用通道保留。
- 自动授权服务器位于 `license-server`，部署步骤见 `README-SELLER.md`。

## v3.4.1 Vercel + Upstash

自动授权后端已迁移到 `license-server-vercel`。不需要官网、不需要卖家电脑开机，也不需要 Render 持久磁盘。部署与环境变量见 `README-SELLER.md`；账号操作交接见 `AGENT-FINISH-SETUP.md`。
