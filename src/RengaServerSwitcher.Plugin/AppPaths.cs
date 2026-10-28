using System.IO;

namespace RengaServerSwitcher;

internal static class AppPaths
{
    public static string Root => Ensure(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RengaServerSwitcher"));

    public static string ProfilesFile => Path.Combine(Root, "profiles.json");
    public static string PreferencesFile => Path.Combine(Root, "settings.json");
    public static string LogsDirectory => Ensure(Path.Combine(Root, "Logs"));
    public static string PendingDirectory => Ensure(Path.Combine(Root, "Pending"));
    public static string LocalModeMarkerFile => Path.Combine(Root, "local-mode.marker");

    public static string GetRengaSettingsIniPath(string productName)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var safeProductName = string.IsNullOrWhiteSpace(productName) ? "Renga Professional" : productName.Trim();
        return Path.Combine(local, "Renga Software", safeProductName, "settings.ini");
    }

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
