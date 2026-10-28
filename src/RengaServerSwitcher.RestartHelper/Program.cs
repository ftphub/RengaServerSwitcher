using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace RengaServerSwitcher.RestartHelper;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var requestPath = GetRequestPath(args);
            if (string.IsNullOrWhiteSpace(requestPath) || !File.Exists(requestPath))
                throw new FileNotFoundException("Restart request file not found.", requestPath);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<RestartRequest>(File.ReadAllText(requestPath), options)
                          ?? throw new InvalidOperationException("Cannot parse restart request.");

            HelperLogger.Write("INFO", $"Waiting for Renga PID={request.RengaProcessId}, target='{request.ProfileName}', local={request.LocalMode}.");
            if (!WaitForExit(request.RengaProcessId, 120_000))
            {
                HelperLogger.Write("ERROR", "Renga did not exit within 120 seconds. Restart cancelled; settings were not changed.");
                return 2;
            }

            // Renga can release native/COM resources a little later than the main window disappears.
            // Do not start a new instance immediately after the old process terminates.
            if (!WaitUntilNoRengaProcesses(15_000))
            {
                HelperLogger.Write("WARN", "Another Renga process is still running. Waiting an additional 5 seconds before applying settings.");
            }

            HelperLogger.Write("INFO", "Old Renga process exited. Settling for 3000 ms before updating settings.ini.");
            Thread.Sleep(3000);

            var accessKey = request.LocalMode
                ? string.Empty
                : DpapiProtector.Unprotect(request.ProtectedAccessKey);

            // В Renga 9.3 пустой ServerHostName может быть интерпретирован как локальный хост.
            // Если на компьютере запущен Collaboration Server на 8820, приложение остаётся
            // в состоянии «Подключено к серверу». Поэтому для гарантированного автономного
            // режима используем корректный loopback-адрес и заведомо свободный локальный порт.
            // Это даёт обычный отказ соединения без обращения к сети.
            if (request.LocalMode)
            {
                request.Host = "127.0.0.1";
                request.UserName = string.IsNullOrWhiteSpace(request.UserName) ? Environment.UserName : request.UserName;
                request.Port = GetUnusedLoopbackPort();
                request.RealtimeCollaboration = false;
                accessKey = string.Empty;
                HelperLogger.Write("INFO", $"Local mode settings prepared: endpoint={request.Host}:{request.Port}, user='{request.UserName}', realtime=false.");
            }

            Exception? last = null;
            for (var attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    IniFileService.Apply(request, accessKey);
                    last = null;
                    break;
                }
                catch (Exception ex)
                {
                    last = ex;
                    HelperLogger.Write("WARN", $"settings.ini update attempt {attempt} failed: {ex.Message}");
                    Thread.Sleep(300);
                }
            }

            if (last != null)
                throw new InvalidOperationException("Cannot update Renga collaboration settings after 10 attempts.", last);

            if (!File.Exists(request.RengaExecutablePath))
                throw new FileNotFoundException("Renga executable not found.", request.RengaExecutablePath);

            UpdateLocalModeMarker(request);

            // A normal Process.Start (including ShellExecute) still starts from this helper process
            // and can inherit environment state originating from the old Renga instance. The user's
            // manual launch works with the very same settings.ini, so launch with a freshly-created
            // Windows user environment instead.
            HelperLogger.Write("INFO", "Settings applied. Waiting 5000 ms before explorer-parent launch of Renga.");
            Thread.Sleep(5000);

            var newPid = FreshProcessLauncher.Start(request.RengaExecutablePath);
            HelperLogger.Write("INFO", $"Renga explorer-parent launch completed. Target='{request.ProfileName}'. PID={newPid}.");

            try { File.Delete(requestPath); } catch { }
            return 0;
        }
        catch (Exception ex)
        {
            HelperLogger.Write("ERROR", $"Restart failed: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }


    private static int GetUnusedLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static void UpdateLocalModeMarker(RestartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LocalModeMarkerPath))
            return;

        try
        {
            var dir = Path.GetDirectoryName(request.LocalModeMarkerPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            if (request.LocalMode)
                File.WriteAllText(request.LocalModeMarkerPath, "LOCAL");
            else if (File.Exists(request.LocalModeMarkerPath))
                File.Delete(request.LocalModeMarkerPath);
        }
        catch (Exception ex)
        {
            HelperLogger.Write("WARN", $"Cannot update local-mode marker: {ex.Message}");
        }
    }

    private static string? GetRequestPath(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i].Equals("--request", StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    private static bool WaitForExit(int pid, int timeoutMs)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.WaitForExit(timeoutMs);
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static bool WaitUntilNoRengaProcesses(int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                var running = Process.GetProcessesByName("Renga");
                try
                {
                    if (running.Length == 0)
                        return true;
                }
                finally
                {
                    foreach (var process in running)
                        process.Dispose();
                }
            }
            catch
            {
                // If process enumeration is temporarily unavailable, retry.
            }

            Thread.Sleep(250);
        }

        return false;
    }
}
