# DevspaceNgrokFoot

一个轻量级 Windows 托盘启动器，用来自动启动并管理：

```powershell
devspace serve
ngrok http 7676
```

它的目标很简单：把原本需要手动打开两个 PowerShell 窗口、分别启动 DevSpace 和 ngrok 的流程，变成一个托盘程序，并保留必要的日志查看、服务复用和 Edge 自动启动能力。

## 功能

- 一键启动 DevSpace 与 ngrok。
- 启动前自动检测已有服务，避免重复启动。
- DevSpace 默认检测 `127.0.0.1:7676/mcp`。
- ngrok 默认检测 `127.0.0.1:4040/api/tunnels`，并确认 tunnel 指向 `7676`。
- 如果服务已经由其他 PowerShell / 脚本启动，则直接复用。
- 退出托盘时，只结束由本程序自己启动的服务，不影响启动前已经存在的 DevSpace / ngrok。
- 继承 Windows Machine/User 持久环境，并正常加载 PowerShell Profile。
- 支持实时查看 DevSpace / ngrok 日志输出。
- 支持通过 Microsoft Edge 扩展，在打开指定 ChatGPT Project 或 Temporary Chat 时自动启动托盘程序。
- 不内置 Node.js、DevSpace、ngrok，不包含大型 runtime。

## 运行效果

双击：

```text
DevspaceNgrokFoot.exe
```

程序会驻留在 Windows 系统托盘，不显示额外控制台窗口。

右键托盘图标可以看到：

```text
查看 DevSpace 输出
查看 ngrok 输出
打开日志目录
----------------
退出
```

“查看输出”会打开一个独立 PowerShell 窗口，显示日志最近 100 行并持续跟随新输出。关闭日志查看窗口不会关闭后台服务。

## 工作流程

```text
DevspaceNgrokFoot.exe
        |
        +--> 检查 DevSpace :7676
        |       |
        |       +--> 已运行 -> 复用
        |       +--> 未运行 -> PowerShell -> devspace serve
        |
        +--> 检查 ngrok :4040
                |
                +--> 已运行且 tunnel 指向 7676 -> 复用
                +--> 未运行 -> PowerShell -> ngrok http 7676
```

如果服务由本程序启动，stdout / stderr 会写入：

```text
devspace.log
ngrok.log
```

## 使用前提

本项目本身不安装依赖。使用者需要在自己的电脑上提前准备并配置：

- Windows 10 / 11
- PowerShell 7 (`pwsh.exe`)
- DevSpace CLI
- ngrok CLI
- 自己的 ngrok 账号和 authtoken
- 自己的 DevSpace 配置、Owner Password / Token、允许访问的目录等

建议 DevSpace 使用 Codex 工具模式，例如：

```text
DEVSPACE_TOOL_MODE=codex
DEVSPACE_WIDGETS=off
```

本程序不会把这些用户环境写死在代码中，而是从 Windows Machine/User 环境和 PowerShell Profile 中继承。

## 快速开始

### 1. 先确认手动命令能运行

在 PowerShell 中分别测试：

```powershell
devspace serve
```

```powershell
ngrok http 7676
```

如果这两条命令在你的环境中都能正常工作，再使用本托盘程序。

### 2. 启动托盘程序

直接运行：

```text
DevspaceNgrokFoot.exe
```

程序会自动检测并启动缺失的服务。

### 3. 查看日志

右键系统托盘图标：

- `查看 DevSpace 输出`
- `查看 ngrok 输出`
- `打开日志目录`

## Edge 自动启动

项目中的：

```text
edge-extension\
edge-integration\
```

提供一个轻量的 Microsoft Edge Native Messaging 集成。

效果是：

```text
Edge 打开指定 ChatGPT 页面
        |
        v
扩展检查 URL
        |
        v
Native Messaging Host
        |
        v
检查 DevspaceNgrokFoot 是否已经运行
        |
        +--> 已运行 -> 什么都不做
        +--> 未运行 -> 自动启动 DevspaceNgrokFoot.exe
```

当前扩展会匹配：

- 配置好的 ChatGPT Project URL
- `https://chatgpt.com/?temporary-chat=true`

### Edge 首次配置

运行：

```text
edge-integration\setup-edge-integration.cmd
```

脚本会：

1. 编译 `DevspaceNgrokFoot.NativeHost.exe`。
2. 在当前 Windows 用户下注册 Edge Native Messaging Host。
3. 打开 `edge://extensions`。
4. 打开 `edge-extension` 文件夹。

然后在 Edge 中：

1. 开启“开发人员模式”。
2. 点击“加载解压缩”。
3. 选择 `edge-extension` 文件夹。

## 修改自己的 ChatGPT Project

默认匹配逻辑在：

```text
edge-extension\background.js
```

主要修改：

```javascript
const PROJECT_PREFIX = "/g/g-p-xxxxxxxxxxxxxxxx-/project";
```

把它替换为自己的 ChatGPT Project 路径即可。

如果你准备让 AI 帮你适配这套项目，建议直接把整个仓库交给 AI，并让它先阅读：

```text
AI_CUSTOMIZATION_GUIDE.md
README.md
```

`AI_CUSTOMIZATION_GUIDE.md` 详细记录了项目结构、启动链路、Edge Extension / Native Host 关系、可修改项、安全边界和验证方法。

## 项目结构

```text
DevspaceNgrokFoot\
├─ DevspaceNgrokFoot.exe
├─ DevspaceNgrokFoot.NativeHost.exe
├─ DevspaceNgrokFoot.ico
├─ TrayApp.cs
├─ EdgeNativeHost.cs
├─ IconGenerator.cs
├─ build.cmd
├─ README.md
├─ AI_CUSTOMIZATION_GUIDE.md
│
├─ edge-extension\
│  ├─ manifest.json
│  └─ background.js
│
└─ edge-integration\
   ├─ build-native-host.cmd
   ├─ register-native-host.ps1
   └─ setup-edge-integration.cmd
```

运行后还可能生成：

```text
devspace.log
ngrok.log
edge-integration\native-host-manifest.json
```

这些本机运行文件不会提交到 Git 仓库。

## 重新编译

主程序：

```text
build.cmd
```

它使用 Windows 自带的 .NET Framework C# 编译器，不依赖 Visual Studio。

Native Messaging Host：

```text
edge-integration\build-native-host.cmd
```

## 关于安全和配置

请不要把以下内容提交到仓库或分享给其他使用者：

```text
ngrok authtoken
DevSpace Owner Password / Token
API Key
个人 PowerShell Profile 中的密钥
个人 .devspace 配置
运行日志中的敏感内容
```

每个使用者都应该使用自己的 ngrok 和 DevSpace 配置。

## 适合的使用场景

这个工具主要适合以下工作流：

```text
ChatGPT / MCP
      |
      v
ngrok 公网 tunnel
      |
      v
本机 DevSpace :7676
      |
      v
本地开发目录 / 编译器 / SDK / Shell 环境
```

它只负责把这条链路的“本地启动和查看日志”自动化，不接管你的开发环境，也不会把第三方 runtime 打包进程序。

## AI 二次适配

如果你是 AI，正在为另一位使用者修改这个项目，请先阅读：

[`AI_CUSTOMIZATION_GUIDE.md`](./AI_CUSTOMIZATION_GUIDE.md)

里面包含完整的接手说明和验证清单。
