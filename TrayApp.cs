using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
    private const string MutexName = "Local\\DevspaceNgrokFoot.Tray";

    [STAThread]
    private static void Main()
    {
        bool createdNew;
        using (var mutex = new Mutex(true, MutexName, out createdNew))
        {
            if (!createdNew)
            {
                MessageBox.Show(
                    "DevSpace + ngrok 托盘程序已经在运行。",
                    "DevspaceNgrokFoot",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayContext());
        }
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon trayIcon;
    private readonly Icon appIcon;
    private readonly string baseDirectory;
    private Process devspaceProcess;
    private Process ngrokProcess;
    private bool shuttingDown;

    public TrayContext()
    {
        baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        var menu = new ContextMenuStrip();
        var devspaceLogItem = new ToolStripMenuItem("查看 DevSpace 输出");
        devspaceLogItem.Click += delegate { OpenLogViewer("DevSpace", "devspace.log"); };
        menu.Items.Add(devspaceLogItem);

        var ngrokLogItem = new ToolStripMenuItem("查看 ngrok 输出");
        ngrokLogItem.Click += delegate { OpenLogViewer("ngrok", "ngrok.log"); };
        menu.Items.Add(ngrokLogItem);

        var openLogDirectoryItem = new ToolStripMenuItem("打开日志目录");
        openLogDirectoryItem.Click += delegate { OpenLogDirectory(); };
        menu.Items.Add(openLogDirectoryItem);
        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += delegate { ExitApplication(); };
        menu.Items.Add(exitItem);

        trayIcon = new NotifyIcon();
        appIcon = LoadApplicationIcon();
        trayIcon.Icon = appIcon;
        trayIcon.Text = "DevSpace + ngrok :7676";
        trayIcon.ContextMenuStrip = menu;
        trayIcon.Visible = true;

        try
        {
            bool devspaceAlreadyRunning = IsDevspaceAvailable();
            if (!devspaceAlreadyRunning)
            {
                devspaceProcess = StartPowerShellCommand("devspace", "serve", "devspace.log");
                WaitUntilReady(
                    IsDevspaceAvailable,
                    devspaceProcess,
                    "devspace serve",
                    "devspace.log");
            }

            bool ngrokAlreadyRunning = IsNgrokTunnelAvailable();
            if (!ngrokAlreadyRunning)
            {
                ngrokProcess = StartPowerShellCommand("ngrok", "http 7676", "ngrok.log");
                WaitUntilReady(
                    IsNgrokTunnelAvailable,
                    ngrokProcess,
                    "ngrok http 7676",
                    "ngrok.log");
            }

            trayIcon.BalloonTipTitle = "DevspaceNgrokFoot";
            trayIcon.BalloonTipText =
                "DevSpace: " + (devspaceAlreadyRunning ? "已在运行" : "已启动") +
                "\r\nngrok: " + (ngrokAlreadyRunning ? "已在运行" : "已启动") +
                "\r\n右键托盘图标可退出。";
            trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            trayIcon.ShowBalloonTip(2500);
        }
        catch (Exception ex)
        {
            StopChildren();
            trayIcon.Visible = false;
            MessageBox.Show(
                "启动服务失败：\r\n\r\n" + ex.Message,
                "DevspaceNgrokFoot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ExitThread();
        }
    }

    private void OpenLogViewer(string displayName, string logFile)
    {
        string logPath = Path.Combine(baseDirectory, logFile);
        try
        {
            if (!File.Exists(logPath))
            {
                using (File.Create(logPath))
                {
                }
            }

            string powerShellCommand =
                "$Host.UI.RawUI.WindowTitle='DevspaceNgrokFoot - " +
                EscapePowerShellSingleQuoted(displayName) +
                "'; Get-Content -LiteralPath '" +
                EscapePowerShellSingleQuoted(logPath) +
                "' -Tail 100 -Wait";

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = ResolvePowerShellExecutable();
            startInfo.Arguments = "-NoLogo -Command \"" + powerShellCommand + "\"";
            startInfo.WorkingDirectory = baseDirectory;
            startInfo.UseShellExecute = true;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "无法打开 " + displayName + " 输出：\r\n\r\n" + ex.Message,
                "DevspaceNgrokFoot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OpenLogDirectory()
    {
        try
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            startInfo.Arguments = "\"" + baseDirectory.TrimEnd('\\') + "\"";
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "无法打开日志目录：\r\n\r\n" + ex.Message,
                "DevspaceNgrokFoot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private Process StartPowerShellCommand(string commandName, string commandArguments, string logFile)
    {
        string logPath = Path.Combine(baseDirectory, logFile);
        string powerShellCommand =
            "& '" + EscapePowerShellSingleQuoted(commandName) + "' " + commandArguments +
            " *>> '" + EscapePowerShellSingleQuoted(logPath) + "'";

        var startInfo = new ProcessStartInfo();
        startInfo.FileName = ResolvePowerShellExecutable();
        startInfo.Arguments = "-NoLogo -Command \"" + powerShellCommand + "\"";
        startInfo.WorkingDirectory = baseDirectory;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        RefreshPersistentEnvironment(startInfo);

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException(
                "无法通过 PowerShell 启动命令：" + commandName + " " + commandArguments);
        }

        return process;
    }

    private static string ResolvePowerShellExecutable()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appAlias = Path.Combine(localAppData, "Microsoft", "WindowsApps", "pwsh.exe");
        if (File.Exists(appAlias))
        {
            return appAlias;
        }

        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string installed = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");
        if (File.Exists(installed))
        {
            return installed;
        }

        return "pwsh.exe";
    }

    private static void RefreshPersistentEnvironment(ProcessStartInfo startInfo)
    {
        IDictionary machine = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
        IDictionary user = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);

        ApplyEnvironment(startInfo, machine, false);
        ApplyEnvironment(startInfo, user, false);

        string machinePath = GetEnvironmentValue(machine, "Path");
        string userPath = GetEnvironmentValue(user, "Path");
        if (!string.IsNullOrEmpty(machinePath) || !string.IsNullOrEmpty(userPath))
        {
            startInfo.EnvironmentVariables["Path"] = JoinPath(machinePath, userPath);
        }
    }

    private static void ApplyEnvironment(
        ProcessStartInfo startInfo,
        IDictionary values,
        bool includePath)
    {
        foreach (DictionaryEntry entry in values)
        {
            string name = entry.Key as string;
            string value = entry.Value as string;
            if (string.IsNullOrEmpty(name) || value == null)
            {
                continue;
            }

            if (!includePath && string.Equals(name, "Path", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            startInfo.EnvironmentVariables[name] = value;
        }
    }

    private static string GetEnvironmentValue(IDictionary values, string name)
    {
        foreach (DictionaryEntry entry in values)
        {
            string key = entry.Key as string;
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value as string;
            }
        }

        return null;
    }

    private static string JoinPath(string machinePath, string userPath)
    {
        if (string.IsNullOrEmpty(machinePath))
        {
            return userPath ?? string.Empty;
        }

        if (string.IsNullOrEmpty(userPath))
        {
            return machinePath;
        }

        return machinePath.TrimEnd(';') + ";" + userPath.TrimStart(';');
    }

    private static string EscapePowerShellSingleQuoted(string value)
    {
        return value.Replace("'", "''");
    }

    private void WaitUntilReady(
        Func<bool> isReady,
        Process process,
        string command,
        string logFile)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 5000)
        {
            if (isReady())
            {
                return;
            }

            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    "命令启动后立即退出：" + command +
                    "\r\n请查看：" + Path.Combine(baseDirectory, logFile));
            }

            Thread.Sleep(200);
        }

        throw new InvalidOperationException(
            "等待服务就绪超时：" + command +
            "\r\n请查看：" + Path.Combine(baseDirectory, logFile));
    }

    private static bool IsDevspaceAvailable()
    {
        return CanOpenHttp("http://127.0.0.1:7676/mcp", false);
    }

    private static bool IsNgrokTunnelAvailable()
    {
        string response;
        if (!TryGetHttp("http://127.0.0.1:4040/api/tunnels", out response))
        {
            return false;
        }

        return response.IndexOf("public_url", StringComparison.OrdinalIgnoreCase) >= 0 &&
               response.IndexOf("7676", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool CanOpenHttp(string url, bool requireSuccessStatus)
    {
        string response;
        try
        {
            return TryGetHttp(url, out response);
        }
        catch (WebException ex)
        {
            return !requireSuccessStatus && ex.Response != null;
        }
    }

    private static bool TryGetHttp(string url, out string body)
    {
        body = null;
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.Timeout = 750;
        request.ReadWriteTimeout = 750;
        request.Proxy = null;

        try
        {
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                body = reader.ReadToEnd();
                return true;
            }
        }
        catch (WebException ex)
        {
            if (ex.Response == null)
            {
                return false;
            }

            using (var response = ex.Response)
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                body = reader.ReadToEnd();
            }

            return true;
        }
    }

    private void ExitApplication()
    {
        if (shuttingDown)
        {
            return;
        }

        shuttingDown = true;
        trayIcon.Visible = false;
        StopChildren();
        trayIcon.Dispose();
        appIcon.Dispose();
        ExitThread();
    }

    private static Icon LoadApplicationIcon()
    {
        var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (extracted == null)
        {
            return (Icon)SystemIcons.Application.Clone();
        }

        try
        {
            return (Icon)extracted.Clone();
        }
        finally
        {
            extracted.Dispose();
        }
    }

    private void StopChildren()
    {
        StopProcessTree(ngrokProcess);
        StopProcessTree(devspaceProcess);
    }

    private static void StopProcessTree(Process process)
    {
        if (process == null)
        {
            return;
        }

        try
        {
            if (process.HasExited)
            {
                process.Dispose();
                return;
            }

            var killer = new ProcessStartInfo();
            killer.FileName = "taskkill.exe";
            killer.Arguments = "/PID " + process.Id + " /T /F";
            killer.UseShellExecute = false;
            killer.CreateNoWindow = true;
            killer.WindowStyle = ProcessWindowStyle.Hidden;

            using (var taskkill = Process.Start(killer))
            {
                if (taskkill != null)
                {
                    taskkill.WaitForExit(3000);
                }
            }
        }
        catch
        {
            // Exiting is best-effort. The process may already have stopped.
        }
        finally
        {
            process.Dispose();
        }
    }
}
