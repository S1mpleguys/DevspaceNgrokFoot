# DevspaceNgrokFoot：AI 接手与个性化改造指南

> 这份文档主要写给“后续负责修改本项目的 AI”。
>
> 目标是让新的使用者把整个项目目录交给 AI 后，AI 不需要重新猜项目结构、启动链路、Edge 扩展机制和安全边界，就可以直接根据新使用者的环境进行修改、构建和验证。

---

## 1. 项目目标

本项目是一个轻量 Windows 托盘启动器，用来自动管理两条原本需要手动在 PowerShell 中执行的命令：

```powershell
devspace serve
ngrok http 7676
```

主要功能：

- 启动前检测 DevSpace 是否已经监听 `127.0.0.1:7676`。
- 检测 ngrok 管理接口 `127.0.0.1:4040` 是否已经存在指向 `7676` 的 tunnel。
- 已经手动启动的服务直接复用，不重复启动。
- 缺失的服务通过 PowerShell 启动。
- PowerShell 子进程继承 Windows Machine/User 持久环境，并正常加载用户 PowerShell Profile。
- 托盘菜单可以查看 DevSpace/ngrok 实时日志。
- 支持 Edge 扩展：打开指定 ChatGPT Project 或 Temporary Chat 时自动启动托盘程序。

本项目**不是**安装器，也**不内置** Node.js、DevSpace、ngrok。

---

## 2. 运行前提

新的使用者需要自行准备并验证：

```text
PowerShell / pwsh
devspace
ngrok
```

建议在 PowerShell 中先手工确认：

```powershell
devspace --version
ngrok version
```

并确认手动执行以下两条命令是可用的：

```powershell
devspace serve
ngrok http 7676
```

如果手动执行都不能正常工作，不要先修改本项目，应优先修复新使用者自己的 DevSpace/ngrok 环境。

---

## 3. 当前项目主要文件

```text
DevspaceNgrokFoot\
├─ TrayApp.cs
│  └─ 主托盘程序、服务检测、PowerShell 启动、日志查看
│
├─ build.cmd
│  └─ 使用 Windows .NET Framework csc.exe 构建主 EXE
│
├─ IconGenerator.cs
├─ DevspaceNgrokFoot.ico
├─ icon-preview.png
│
├─ EdgeNativeHost.cs
│  └─ Edge Native Messaging Host
│
├─ DevspaceNgrokFoot.NativeHost.exe
│
├─ edge-extension\
│  ├─ manifest.json
│  └─ background.js
│
├─ edge-integration\
│  ├─ build-native-host.cmd
│  ├─ register-native-host.ps1
│  ├─ setup-edge-integration.cmd
│  └─ native-host-manifest.json   # 注册脚本生成/刷新
│
├─ README.md
└─ AI_CUSTOMIZATION_GUIDE.md
```

运行过程中还会出现：

```text
devspace.log
ngrok.log
```

这两个日志文件不属于发布配置，不应该作为个人配置模板使用。

---

## 4. 主程序启动链路

`TrayApp.cs` 当前核心流程：

```text
DevspaceNgrokFoot.exe
        ↓
检查 Mutex：Local\DevspaceNgrokFoot.Tray
        ↓
检查 127.0.0.1:7676/mcp
        │
        ├─ 可用 → 复用现有 DevSpace
        └─ 不可用 → PowerShell 执行 devspace serve
        ↓
检查 127.0.0.1:4040/api/tunnels
        │
        ├─ 已有指向 7676 的 tunnel → 复用
        └─ 没有 → PowerShell 执行 ngrok http 7676
```

重要行为：

- 只有**由托盘程序自己启动的服务**，在托盘“退出”时才会被结束。
- 启动托盘之前已经存在的 DevSpace/ngrok 会被视为外部服务，不会在退出时被杀掉。
- 不要轻易破坏这个“服务所有权”语义。

---

## 5. PowerShell / 环境变量设计

这是本项目之前重点修复过的部分。

不要把开发环境变量一个个硬编码到 C# 中。

当前逻辑是：

```text
Windows Machine 持久环境
          +
Windows User 持久环境
          ↓
RefreshPersistentEnvironment(...)
          ↓
启动 PowerShell / pwsh
          ↓
正常加载用户 Profile
          ↓
devspace serve / ngrok http 7676
```

原因：DevSpace 后续执行的命令会继承 DevSpace 服务进程的 `process.env`，所以开发工具链可能依赖：

```text
PATH
JAVA_HOME
ANDROID_HOME
Python
CMake
ARM GCC
Git
SDK 变量
用户 PowerShell Profile 中设置的变量
```

因此 AI 修改时应优先保留现有的通用环境继承方案。

### DevSpace Codex 模式

使用者自己的环境应确保 DevSpace 以需要的 tool mode 启动。

当前已验证过的环境为：

```text
DEVSPACE_TOOL_MODE=codex
DEVSPACE_WIDGETS=off
```

如果新使用者的 DevSpace 版本不同，AI 应先检查对应版本的工具模式配置，不要盲目假定完全相同。

---

## 6. 托盘日志查看功能

当前托盘菜单包含：

```text
查看 DevSpace 输出
查看 ngrok 输出
打开日志目录
────────────────
退出
```

服务本身仍然后台运行，stdout/stderr 写入：

```text
devspace.log
ngrok.log
```

点击“查看输出”时只是额外打开 PowerShell：

```powershell
Get-Content <日志文件> -Tail 100 -Wait
```

因此：

```text
关闭日志查看 PowerShell窗口 ≠ 关闭服务
```

修改日志功能时应继续保持“观察窗口”和“服务进程”解耦。

---

## 7. Edge 自动启动架构

当前采用 Edge 官方 Native Messaging 机制：

```text
Edge 标签页 URL
      ↓
edge-extension/background.js
      ↓
chrome.runtime.sendNativeMessage(...)
      ↓
DevspaceNgrokFoot.NativeHost.exe
      ↓
检测托盘 Mutex
      ↓
未运行时启动 DevspaceNgrokFoot.exe
```

Native Host 名称：

```text
com.devspacengrokfoot.launcher
```

Windows 当前用户注册位置：

```text
HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.devspacengrokfoot.launcher
```

注册表默认值指向：

```text
edge-integration\native-host-manifest.json
```

注册脚本会根据**当前项目实际目录**生成 Native Host EXE 的绝对路径，所以项目不要求固定放在原开发者的目录。

---

## 8. 新使用者最常需要修改的配置：ChatGPT Project URL

目前项目匹配规则位于：

```text
edge-extension\background.js
```

当前有：

```javascript
const PROJECT_PREFIX = "/g/g-p-6a709f3c5ef08191bc68fc40b7a05804-/project";
```

这是原使用者自己的 ChatGPT Project 路径。

### 新使用者修改方法

假设新使用者的 Project URL 是：

```text
https://chatgpt.com/g/g-p-ABCDEFG123456789-/project
```

则改成：

```javascript
const PROJECT_PREFIX = "/g/g-p-ABCDEFG123456789-/project";
```

不要把整个 `https://chatgpt.com` 写入 `PROJECT_PREFIX`，因为代码中比较的是 `url.pathname`。

Temporary Chat 当前判断为：

```javascript
url.searchParams.get("temporary-chat") === "true"
```

这部分通常不需要针对不同用户修改。

---

## 9. Edge 扩展固定 ID

`edge-extension/manifest.json` 中存在固定 `key`。

它的目的是让“加载解压缩”的开发版扩展保持稳定 Extension ID。

当前 ID：

```text
pomlpmhgnbemhbdmefpjpmccehfmcafl
```

`edge-integration/register-native-host.ps1` 中也写有相同 ID：

```powershell
$extensionId = "pomlpmhgnbemhbdmefpjpmccehfmcafl"
```

Native Host manifest 的：

```json
"allowed_origins": [
  "chrome-extension://pomlpmhgnbemhbdmefpjpmccehfmcafl/"
]
```

必须和真实扩展 ID 一致。

### AI 修改规则

如果只是改 Project URL：

```text
不要修改 manifest.json 的 key
不要修改 extensionId
```

这样 Native Messaging 注册无需变化。

只有在明确需要换扩展公钥/ID 时，才同时修改：

```text
manifest.json -> key
register-native-host.ps1 -> extensionId
native-host-manifest.json -> allowed_origins（重新执行注册脚本即可生成）
```

---

## 10. Edge 扩展首次加载步骤

在新电脑上：

1. 运行：

```text
edge-integration\setup-edge-integration.cmd
```

2. 打开：

```text
edge://extensions
```

3. 打开“开发人员模式”。

4. 点击“加载解压缩”。

5. 选择：

```text
edge-extension
```

浏览器安全机制决定了“首次加载解压扩展”需要用户主动操作，不应尝试通过脚本绕过。

修改 `background.js` 后，通常需要在 `edge://extensions` 对该扩展点击“重新加载”。

---

## 11. Native Messaging Host 协议

实现文件：

```text
EdgeNativeHost.cs
```

Native Messaging 使用 stdin/stdout：

```text
4 字节 little-endian 消息长度
+
UTF-8 JSON payload
```

当前收到消息后主要检查：

```text
Local\DevspaceNgrokFoot.Tray
```

如果托盘已经运行：

```json
{
  "ok": true,
  "started": false,
  "alreadyRunning": true
}
```

如果没运行，则启动同目录：

```text
DevspaceNgrokFoot.exe
```

Native Host 只负责“确保托盘已经运行”，不要把 DevSpace/ngrok 的复杂启动逻辑重复搬进 Native Host。

---

## 12. 如何构建

### 主程序

执行：

```text
build.cmd
```

使用 Windows 自带 .NET Framework C# 编译器：

```text
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

或 x86 fallback。

不要随意换成需要额外安装的大型构建体系，除非新使用者明确要求。

### Native Host

执行：

```text
edge-integration\build-native-host.cmd
```

生成：

```text
DevspaceNgrokFoot.NativeHost.exe
```

### Edge 集成注册

执行：

```text
edge-integration\setup-edge-integration.cmd
```

它会完成 Native Host 编译和当前用户注册。

---

## 13. AI 修改后的最小验证清单

AI 不应只修改文件后直接宣称完成。至少验证以下项目。

### A. 编译

```text
主 TrayApp 编译成功
Native Host 编译成功（如果修改了 Edge 集成）
```

### B. 已运行服务复用

先手动启动：

```powershell
devspace serve
ngrok http 7676
```

再启动托盘，应确认：

```text
没有额外 DevSpace
没有额外 ngrok tunnel
退出托盘不会杀掉手动服务
```

### C. 托盘冷启动

关闭原服务后启动托盘，应确认：

```text
127.0.0.1:7676 正常监听
127.0.0.1:4040 正常监听
ngrok tunnel 指向 7676
```

### D. DevSpace Tool Mode

确认最终 DevSpace 环境符合新使用者的需求，例如：

```text
DEVSPACE_TOOL_MODE=codex
```

并实际验证需要的 MCP 工具，而不仅仅检查变量字符串。

建议至少验证：

```text
read
exec_command
apply_patch
```

### E. Edge 自动启动

关闭托盘后打开目标 ChatGPT Project：

```text
应自动出现 DevspaceNgrokFoot 托盘图标
```

然后重复刷新/切换 URL：

```text
不应重复启动托盘
```

### F. 日志查看

右键托盘：

```text
查看 DevSpace 输出
查看 ngrok 输出
```

确认实时输出可见。

关闭日志窗口后确认服务仍正常运行。

---

## 14. 不应该复制给新使用者的个人数据

打包/分享项目时不要包含以下内容：

```text
devspace.log
ngrok.log
```

也不要从原电脑额外复制：

```text
%USERPROFILE%\.devspace\
ngrok authtoken / ngrok 配置文件
任何 Owner Password / Token
PowerShell Profile 中的 API Key
个人 SDK 凭据
浏览器个人配置
```

尤其不要因为“方便朋友直接使用”就复制原使用者的认证信息。

新使用者应该使用：

```text
自己的 ngrok 账号/token
自己的 DevSpace 配置
自己的 ChatGPT Project URL
自己的开发环境变量
```

---

## 15. 便携包建议

如果需要给其他人分享，推荐 ZIP，不推荐之前尝试过的大型自包含安装包。

建议 ZIP 仅包含：

```text
DevspaceNgrokFoot.exe
DevspaceNgrokFoot.NativeHost.exe
DevspaceNgrokFoot.ico
edge-extension\
edge-integration\
README.md
AI_CUSTOMIZATION_GUIDE.md
```

如果朋友希望让 AI 修改源码，再额外包含：

```text
TrayApp.cs
EdgeNativeHost.cs
IconGenerator.cs
build.cmd
```

不要直接把整个开发目录无筛选压缩发送。

---

## 16. 推荐给后续 AI 的工作流程

新的使用者可以直接告诉 AI：

> 请先阅读 `AI_CUSTOMIZATION_GUIDE.md` 和 `README.md`，再检查当前源码。把这个项目改成适配我的电脑和我的 ChatGPT Project。先确认我本机的 DevSpace/ngrok 手动启动是否正常，然后修改需要的配置，重新构建，并按文档中的验证清单完成自测。不要复制原作者的 token、密码或个人配置。

AI 接手后建议按以下顺序：

```text
1. 阅读 AI_CUSTOMIZATION_GUIDE.md
2. 阅读 README.md
3. 阅读 TrayApp.cs / background.js / register-native-host.ps1
4. 获取新使用者自己的 ChatGPT Project URL
5. 检查 devspace/ngrok 实际安装和版本
6. 检查手工启动是否正常
7. 修改 PROJECT_PREFIX 等必要配置
8. 构建
9. 重新注册/重新加载 Edge 扩展（若需要）
10. 完成完整回归验证
```

---

## 17. 当前版本的设计原则

后续修改尽量保持以下原则：

```text
轻量 > 自包含大安装包
复用用户已有环境 > 自己维护所有 SDK
服务与日志查看解耦
已存在服务不重复启动
托盘只结束自己创建的子进程
Edge Native Host 只负责 ensure-running
认证信息属于每个用户自己
修改后必须实际验证
```

如果新需求与这些原则冲突，可以修改架构，但应明确说明为什么需要改变，而不是无意中破坏现有稳定行为。

