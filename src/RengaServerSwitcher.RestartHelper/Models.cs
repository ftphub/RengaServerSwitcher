namespace RengaServerSwitcher.RestartHelper;

internal sealed class RestartRequest
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
