# DevspaceNgrokFoot

如果要把本项目交给另一台电脑/另一位使用者，并让 AI 负责适配修改，请优先阅读：

```text
AI_CUSTOMIZATION_GUIDE.md
```

一个极小的 Windows 托盘程序，用来同时启动：

```text
devspace serve
ngrok http 7676
```

## 使用

1. 双击 `DevspaceNgrokFoot.exe`。
2. 程序不会打开控制台窗口，会出现在 Windows 11 任务栏右下角的系统托盘。
3. 启动前会先检查本机 `7676` 的 DevSpace 和 `4040` 的 ngrok 管理接口；如果已经由 PowerShell 等方式启动，则直接复用，不再重复启动。
4. 右键托盘图标，点击“退出”，程序只会结束由它自己启动的 DevSpace 和 ngrok 进程树，不会关闭启动前已经存在的服务。

需要自行启动服务时，托盘程序会直接拉起 PowerShell 7 (`pwsh.exe`)；PowerShell 正常加载用户 Profile 后再执行
`devspace serve` 和 `ngrok http 7676`。在创建 PowerShell 子进程前，程序会从 Windows 的 Machine/User 持久环境刷新通用环境，
因此不在代码里维护 `PATH`、SDK 或 `DEVSPACE_*` 等具体开发变量清单。

运行日志写入同目录：

- `devspace.log`
- `ngrok.log`

右键托盘图标还可以：

- 点击“查看 DevSpace 输出”，打开独立 PowerShell 窗口，显示 `devspace.log` 最近 100 行并持续跟随新日志。
- 点击“查看 ngrok 输出”，打开独立 PowerShell 窗口，显示 `ngrok.log` 最近 100 行并持续跟随新日志。
- 点击“打开日志目录”，直接打开程序所在目录。

关闭这些日志查看窗口只会停止查看日志，不会停止后台 DevSpace 或 ngrok 服务。

## 重新编译

双击 `build.cmd`。它使用 Windows 自带的 .NET Framework C# 编译器，不需要安装额外开发依赖。

图标构图基于图像模型生成的 Windows 11 风格视觉稿，并在 `IconGenerator.cs` 中重绘为可缩放资源。构建时会生成：

- `DevspaceNgrokFoot.ico`：包含 16、24、32、48、64、128、256 px 多尺寸图标，并嵌入 EXE。
- `icon-preview.png`：256 px 预览图。

托盘图标也会从当前 EXE 中提取同一套图标资源，因此资源管理器、桌面快捷方式和系统托盘显示保持一致。

## Edge 自动启动

`edge-extension` + `edge-integration` 提供一个轻量的 Edge Native Messaging 集成：当 Edge 打开指定的 🐻 ChatGPT Project，或打开带 `temporary-chat=true` 的 ChatGPT 页面时，如果托盘程序尚未运行，就自动启动 `DevspaceNgrokFoot.exe`；如果已经运行，则什么也不做。

首次设置：

1. 双击 `edge-integration\setup-edge-integration.cmd`。它只会编译一个很小的 `DevspaceNgrokFoot.NativeHost.exe` 并注册到当前用户的 Edge Native Messaging Hosts，不会安装大型 runtime。
2. Edge 会打开 `edge://extensions`。打开“开发人员模式”，点击“加载解压缩”，选择项目里的 `edge-extension` 文件夹。
3. 扩展使用固定开发 ID `pomlpmhgnbemhbdmefpjpmccehfmcafl`，因此 Native Host 注册无需手工复制扩展 ID。

当前匹配：

- `https://chatgpt.com/g/g-p-6a709f3c5ef08191bc68fc40b7a05804-/project...`
- `https://chatgpt.com/...?...temporary-chat=true...`
