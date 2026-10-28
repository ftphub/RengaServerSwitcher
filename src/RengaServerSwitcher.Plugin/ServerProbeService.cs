using System.Diagnostics;
using System.Net.Sockets;

namespace RengaServerSwitcher;

internal sealed record ServerProbeResult(bool Success, long ElapsedMilliseconds, string Message);

internal static class ServerProbeService
{
    public static async Task<ServerProbeResult> ProbeAsync(string host, int port, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port).WaitAsync(timeout);
            sw.Stop();
            return new ServerProbeResult(true, sw.ElapsedMilliseconds,
                $"TCP-соединение с {host}:{port} установлено за {sw.ElapsedMilliseconds} мс.\n\nЭто подтверждает доступность адреса и порта, но не проверяет ключ доступа Renga.");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ServerProbeResult(false, sw.ElapsedMilliseconds,
                $"Не удалось подключиться к {host}:{port}.\n\n{ex.Message}");
        }
    }
}
