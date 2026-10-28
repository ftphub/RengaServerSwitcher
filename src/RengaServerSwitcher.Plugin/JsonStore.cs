using System.IO;
using System.Text;
using System.Text.Json;

namespace RengaServerSwitcher;

internal static class JsonStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static T Load<T>(string path, T fallback)
    {
        if (!File.Exists(path)) return fallback;
        var json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(json, Options) ?? fallback;
    }

    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(temp, json, new UTF8Encoding(false));
        File.Move(temp, path, true);
    }
}
