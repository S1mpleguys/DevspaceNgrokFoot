using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

internal static class EdgeNativeHost
{
    private const string MutexName = "Local\\DevspaceNgrokFoot.Tray";
    private const int MaxRequestBytes = 1024 * 1024;

    private static int Main(string[] args)
    {
        try
        {
            byte[] request = ReadMessage(Console.OpenStandardInput());
            if (request == null)
            {
                return 0;
            }

            bool alreadyRunning = IsTrayRunning();
            bool started = false;

            if (!alreadyRunning)
            {
                string launcher = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "DevspaceNgrokFoot.exe");

                if (!File.Exists(launcher))
                {
                    WriteResponse(false, false, false, "DevspaceNgrokFoot.exe not found.");
                    return 2;
                }

                var startInfo = new ProcessStartInfo();
                startInfo.FileName = launcher;
                startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                using (var process = Process.Start(startInfo))
                {
                    started = process != null;
                }

                for (int i = 0; i < 20 && !IsTrayRunning(); i++)
                {
                    Thread.Sleep(100);
                }
            }

            bool running = IsTrayRunning();
            WriteResponse(running, started, alreadyRunning, running ? null : "Launcher did not become ready.");
            return running ? 0 : 3;
        }
        catch (Exception ex)
        {
            try
            {
                WriteResponse(false, false, false, ex.Message);
            }
            catch
            {
            }
            return 1;
        }
    }

    private static bool IsTrayRunning()
    {
        try
        {
            using (Mutex.OpenExisting(MutexName))
            {
                return true;
            }
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static byte[] ReadMessage(Stream input)
    {
        byte[] lengthBytes = ReadExactly(input, 4);
        if (lengthBytes == null)
        {
            return null;
        }

        int length = BitConverter.ToInt32(lengthBytes, 0);
        if (length < 0 || length > MaxRequestBytes)
        {
            throw new InvalidDataException("Invalid native messaging request length.");
        }

        return ReadExactly(input, length) ?? new byte[0];
    }

    private static byte[] ReadExactly(Stream input, int count)
    {
        if (count == 0)
        {
            return new byte[0];
        }

        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = input.Read(buffer, offset, count - offset);
            if (read <= 0)
            {
                if (offset == 0)
                {
                    return null;
                }

                throw new EndOfStreamException();
            }
            offset += read;
        }

        return buffer;
    }

    private static void WriteResponse(bool ok, bool started, bool alreadyRunning, string error)
    {
        string json =
            "{\"ok\":" + Bool(ok) +
            ",\"started\":" + Bool(started) +
            ",\"alreadyRunning\":" + Bool(alreadyRunning) +
            (string.IsNullOrEmpty(error) ? string.Empty : ",\"error\":\"" + JsonEscape(error) + "\"") +
            "}";

        byte[] payload = Encoding.UTF8.GetBytes(json);
        byte[] length = BitConverter.GetBytes(payload.Length);
        Stream output = Console.OpenStandardOutput();
        output.Write(length, 0, length.Length);
        output.Write(payload, 0, payload.Length);
        output.Flush();
    }

    private static string Bool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string JsonEscape(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
