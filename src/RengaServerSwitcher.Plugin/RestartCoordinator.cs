using System.Diagnostics;
using System.IO;

namespace RengaServerSwitcher;

internal sealed class RestartCoordinator
{
    private readonly string _pluginFolder;
    private readonly Logger _log;

    public RestartCoordinator(string pluginFolder, Logger log)
    {
        _pluginFolder = pluginFolder;
        _log = log;
    }

    public void Start(Renga.IApplication app, ServerProfile? profile, bool localMode)
    {
        var helper = Path.Combine(_pluginFolder, "RengaServerSwitcher.RestartHelper.exe");
        if (!File.Exists(helper))
            throw new FileNotFoundException("Не найден RestartHelper. Соберите оба проекта решения и установите весь набор файлов плагина.", helper);

        var currentProcess = Process.GetCurrentProcess();
        var rengaExe = currentProcess.MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(rengaExe) || !File.Exists(rengaExe))
            throw new InvalidOperationException("Не удалось определить путь к Renga.exe.");

        var request = new RestartRequest
        {
            RengaProcessId = currentProcess.Id,
            RengaExecutablePath = rengaExe!,
            SettingsIniPath = AppPaths.GetRengaSettingsIniPath(app.ProductName),
            LocalModeMarkerPath = AppPaths.LocalModeMarkerFile,
            LocalMode = localMode,
            ProfileName = localMode ? "Автономная работа" : profile!.Name,

            // В локальном режиме helper сам выберет свободный loopback-порт.
            // Пустой host не используем: Renga может трактовать его как localhost.
            Host = localMode ? "127.0.0.1" : profile!.Host,
            Port = localMode ? 8820 : profile!.Port,
            UserName = localMode ? Environment.UserName : profile!.UserName,
            ProtectedAccessKey = localMode ? string.Empty : DpapiProtector.Protect(profile!.AccessKey),
            RealtimeCollaboration = localMode ? false : profile!.RealtimeCollaboration
        };

        var requestPath = Path.Combine(AppPaths.PendingDirectory, $"restart-{Guid.NewGuid():N}.json");
        JsonStore.Save(requestPath, request);

        var psi = new ProcessStartInfo
        {
            FileName = helper,
            UseShellExecute = false,
            WorkingDirectory = _pluginFolder,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("--request");
        psi.ArgumentList.Add(requestPath);

        var helperProcess = Process.Start(psi);
        if (helperProcess is null)
            throw new InvalidOperationException("Не удалось запустить RestartHelper.");

        _log.Info($"Restart helper started. Target='{request.ProfileName}', local={request.LocalMode}.");
    }
}
