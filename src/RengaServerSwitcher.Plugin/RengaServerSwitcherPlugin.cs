using System.IO;
using System.Windows.Interop;
using System.Windows.Threading;

namespace RengaServerSwitcher;

public sealed class RengaServerSwitcherPlugin : Renga.IPlugin
{
    private Renga.IApplication? _app;
    private Renga.ApplicationEventSource? _appEvents;
    private readonly List<Renga.ActionEventSource> _actionEvents = new();
    private MainWindow? _window;
    private string _pluginFolder = string.Empty;
    private readonly Logger _log = new();
    private Renga.IImage? _actionIcon;

    public bool Initialize(string pluginFolder)
    {
        try
        {
            _pluginFolder = pluginFolder;
            _app = new Renga.Application();
            var ui = _app.UI;
            var panel = ui.CreateUIPanelExtension();
            var action = ui.CreateAction();
            action.DisplayName = "Renga Server Switcher";

            // Toolbar icon. If the image cannot be loaded, the plugin still starts
            // and Renga falls back to its default action appearance.
            try
            {
                var iconPath = Path.Combine(pluginFolder, "RengaServerSwitcher.png");
                if (File.Exists(iconPath))
                {
                    _actionIcon = ui.CreateImage();
                    _actionIcon.LoadFromFile(iconPath);
                    action.Icon = _actionIcon;
                }
            }
            catch (Exception ex)
            {
                _log.Error("Cannot load toolbar icon.", ex);
            }

            var actionEvents = new Renga.ActionEventSource(action);
            actionEvents.Triggered += (_, _) => ShowWindow();
            _actionEvents.Add(actionEvents);
            panel.AddToolButton(action);
            ui.AddExtensionToPrimaryPanel(panel);

            _appEvents = new Renga.ApplicationEventSource(_app);
            _appEvents.ConnectedToServer += OnConnectionChanged;
            _appEvents.DisconnectedFromServer += OnConnectionChanged;

            _log.Info($"Plugin initialized. Renga={_app.ProductName} {_app.VersionS}.");

            var preferences = new PreferencesRepository().Load();
            if (preferences.AutoShowOnStartup)
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(ShowWindow), DispatcherPriority.ApplicationIdle);

            return true;
        }
        catch (Exception ex)
        {
            _log.Error("Plugin initialization failed.", ex);
            return false;
        }
    }

    public void Stop()
    {
        try
        {
            _window?.Close();
            _appEvents?.Dispose();
            foreach (var source in _actionEvents) source.Dispose();
            _actionEvents.Clear();
            _log.Info("Plugin stopped.");
        }
        catch (Exception ex) { _log.Error("Plugin stop error.", ex); }
    }

    private void OnConnectionChanged() => _window?.RefreshFromRenga();

    private void ShowWindow()
    {
        if (_app == null) return;
        try
        {
            if (_window != null)
            {
                if (_window.WindowState == System.Windows.WindowState.Minimized)
                    _window.WindowState = System.Windows.WindowState.Normal;
                _window.Activate();
                _window.RefreshFromRenga();
                return;
            }

            _window = new MainWindow(_app, _pluginFolder, _log);
            try
            {
                var hwnd = _app.GetMainWindowHandle();
                if (hwnd != 0) new WindowInteropHelper(_window).Owner = new IntPtr(hwnd);
            }
            catch (Exception ex) { _log.Error("Cannot assign Renga window as owner.", ex); }

            _window.Closed += (_, _) => _window = null;
            _window.Show();
        }
        catch (Exception ex)
        {
            _log.Error("Cannot open main window.", ex);
            _app.UI.ShowMessageBox(Renga.MessageIcon.MessageIcon_Error, "Renga Server Switcher", ex.Message);
        }
    }
}
