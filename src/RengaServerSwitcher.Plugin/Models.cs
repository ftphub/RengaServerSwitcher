using System.Text.Json.Serialization;

namespace RengaServerSwitcher;

public sealed class ServerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8820;
    public string UserName { get; set; } = string.Empty;
    [JsonIgnore]
    public string AccessKey { get; set; } = string.Empty;
    public bool RealtimeCollaboration { get; set; } = true;

    [JsonIgnore] public bool IsCurrent { get; set; }
    [JsonIgnore] public string CurrentMarker => IsCurrent ? "● Текущий" : string.Empty;
    [JsonIgnore] public string Endpoint => $"{Host}:{Port}";
    [JsonIgnore] public string RealtimeText => RealtimeCollaboration ? "Да" : "Нет";
    [JsonIgnore] public string ProbeText { get; set; } = "—";
    [JsonIgnore] public string KeyStateText => string.IsNullOrEmpty(AccessKey) ? "не задан" : "сохранён";

    public ServerProfile Clone() => new()
    {
        Id = Id,
        Name = Name,
        Host = Host,
        Port = Port,
        UserName = UserName,
        AccessKey = AccessKey,
        RealtimeCollaboration = RealtimeCollaboration,
        IsCurrent = IsCurrent,
        ProbeText = ProbeText
    };
}

internal sealed class StoredServerProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string AccessKeyProtected { get; set; } = string.Empty;
    public bool RealtimeCollaboration { get; set; }
}

public sealed class PluginPreferences
{
    public bool AutoShowOnStartup { get; set; } = true;
    public bool RememberLastSelectedProfile { get; set; } = true;
    public bool CheckServerBeforeConnect { get; set; } = true;
    public bool ConfirmBeforeRestart { get; set; } = true;
    public Guid? LastSelectedProfileId { get; set; }
}

public sealed class RestartRequest
{
    public int RengaProcessId { get; set; }
    public string RengaExecutablePath { get; set; } = string.Empty;
    public string SettingsIniPath { get; set; } = string.Empty;
    public string LocalModeMarkerPath { get; set; } = string.Empty;
    public bool LocalMode { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8820;
    public string UserName { get; set; } = string.Empty;
    public string ProtectedAccessKey { get; set; } = string.Empty;
    public bool RealtimeCollaboration { get; set; }
}
