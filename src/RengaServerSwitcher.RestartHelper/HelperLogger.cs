using System.IO;
using System.Text;

namespace RengaServerSwitcher.RestartHelper;

internal static class HelperLogger
{
    public static void Write(string level, string text)
    {
        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RengaServerSwitcher", "Logs");
            Directory.CreateDirectory(root);
            var file = Path.Combine(root, $"RengaServerSwitcher-{DateTime.Now:yyyy-MM-dd}.log");
            File.AppendAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [Helper] {text}{Environment.NewLine}", new UTF8Encoding(false));
        }
        catch { }
    }
}
