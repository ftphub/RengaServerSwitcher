using System.Text;

namespace RengaServerSwitcher.RestartHelper;

internal static class IniFileService
{
    public static void Apply(RestartRequest request, string accessKey)
    {
        if (!request.LocalMode)
        {
            if (string.IsNullOrWhiteSpace(request.Host))
                throw new InvalidOperationException("ServerHostName cannot be empty for a server profile.");
            if (string.IsNullOrWhiteSpace(request.UserName))
                throw new InvalidOperationException("CollaborationUserName cannot be empty for a server profile.");
        }

        if (request.Port is < 1 or > 65535)
            throw new InvalidOperationException("CollaborationPort is out of range.");

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ServerHostName"] = Sanitize(request.Host),
            ["CollaborationUserName"] = Sanitize(request.UserName),
            ["CollaborationPort"] = request.Port.ToString(),
            ["CollaborationServerAccessKey"] = Sanitize(accessKey),
            ["RealtimeCollaboration"] = request.LocalMode ? "false" : (request.RealtimeCollaboration ? "true" : "false")
        };

        var path = request.SettingsIniPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var lines = File.Exists(path)
            ? File.ReadAllLines(path, Encoding.UTF8).ToList()
            : new List<string>();

        var sectionStart = lines.FindIndex(x => x.Trim().Equals("[Collaboration]", StringComparison.OrdinalIgnoreCase));
        if (sectionStart < 0)
        {
            if (lines.Count > 0 && lines[^1].Length != 0)
                lines.Add(string.Empty);
            sectionStart = lines.Count;
            lines.Add("[Collaboration]");
        }

        var sectionEnd = lines.FindIndex(sectionStart + 1,
            x => x.TrimStart().StartsWith("[", StringComparison.Ordinal));
        if (sectionEnd < 0)
            sectionEnd = lines.Count;

        foreach (var kv in values)
        {
            var prefix = kv.Key + "=";
            var index = -1;
            for (var i = sectionStart + 1; i < sectionEnd; i++)
            {
                if (lines[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            var newLine = prefix + kv.Value;
            if (index >= 0)
                lines[index] = newLine;
            else
            {
                lines.Insert(sectionEnd, newLine);
                sectionEnd++;
            }
        }

        var temp = path + ".rss.tmp";
        var backup = path + ".rss.bak";
        File.WriteAllLines(temp, lines, new UTF8Encoding(false));
        if (File.Exists(path))
            File.Copy(path, backup, true);
        File.Move(temp, path, true);
    }

    private static string Sanitize(string value) =>
        (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
}
