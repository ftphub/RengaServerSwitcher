using System.IO;
using System.Text;

namespace RengaServerSwitcher;

internal sealed class Logger
{
    private readonly object _sync = new();
    private string LogFile => Path.Combine(AppPaths.LogsDirectory, $"RengaServerSwitcher-{DateTime.Now:yyyy-MM-dd}.log");

    public void Info(string message) => Write("INFO", message);
    public void Error(string message, Exception? ex = null) => Write("ERROR", ex == null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}");

    private void Write(string level, string message)
    {
        try
        {
            // Never pass access keys/secrets to this method.
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            lock (_sync)
                File.AppendAllText(LogFile, line, new UTF8Encoding(false));
        }
        catch
        {
            // Logging must never break Renga.
        }
    }
}
