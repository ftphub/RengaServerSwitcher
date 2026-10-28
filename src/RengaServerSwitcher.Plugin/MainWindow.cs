using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace RengaServerSwitcher;

internal sealed class MainWindow : Window
{
    private readonly Renga.IApplication _app;
    private readonly ProfileRepository _profiles;
    private readonly PreferencesRepository _preferencesRepo;
    private readonly RestartCoordinator _restart;
    private readonly Logger _log;
    private PluginPreferences _preferences;

    private readonly ObservableCollection<ServerProfile> _items = new();
    private readonly Dictionary<Guid, string> _probeStates = new();
    private readonly DataGrid _grid = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _current = new();
    private readonly TextBlock _user = new();
    private readonly TextBlock _selectedTitle = new();
    private readonly TextBlock _selectedDetails = new();
    private readonly TextBlock _selectedProbe = new();
    private readonly Button _connect = new() { Content = "Подключиться", Width = 145, Height = 36 };
    private readonly Button _local = new() { Content = "Остаться работать локально", Width = 215, Height = 36 };
    private readonly Button _edit = new() { Content = "Изменить", MinWidth = 100, Height = 32 };
    private readonly Button _delete = new() { Content = "Удалить", MinWidth = 100, Height = 32 };
    private readonly Button _probe = new() { Content = "Проверить доступность", MinWidth = 175, Height = 32 };
    private bool _isCurrentlyLocal;

    public MainWindow(Renga.IApplication app, string pluginFolder, Logger log)
    {
        _app = app;
        _log = log;
        _profiles = new ProfileRepository(log);
        _preferencesRepo = new PreferencesRepository();
        _preferences = _preferencesRepo.Load();
        _restart = new RestartCoordinator(pluginFolder, log);

        Title = "Renga Server Switcher";
        Width = 900;
        Height = 650;
        MinWidth = 800;
        MinHeight = 570;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = BuildUi();
        Loaded += (_, _) => ReloadProfiles();
    }

    public void RefreshFromRenga()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(RefreshFromRenga);
            return;
        }
        ReloadProfiles();
    }

    private UIElement BuildUi()
    {
        var root = new DockPanel { Margin = new Thickness(16) };

        var header = new Border
        {
            Padding = new Thickness(14),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var headStack = new StackPanel();
        _status.FontSize = 17;
        _status.FontWeight = FontWeights.SemiBold;
        _current.Margin = new Thickness(0, 6, 0, 0);
        _user.Margin = new Thickness(0, 4, 0, 0);
        headStack.Children.Add(_status);
        headStack.Children.Add(_current);
        headStack.Children.Add(_user);
        header.Child = headStack;
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var bottom = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var settings = new Button { Content = "⚙ Настройки", Width = 120, Height = 36 };
        settings.Click += Settings_Click;
        bottom.Children.Add(settings);

        var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        _local.Margin = new Thickness(0, 0, 10, 0);
        _local.Click += async (_, _) => await StartSwitchAsync(null, true);
        _connect.Click += async (_, _) => await ConnectSelectedAsync();
        actionPanel.Children.Add(_local);
        actionPanel.Children.Add(_connect);
        Grid.SetColumn(actionPanel, 2);
        bottom.Children.Add(actionPanel);
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        var tools = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var add = new Button { Content = "Добавить", MinWidth = 100, Height = 32, Margin = new Thickness(0, 0, 8, 0) };
        add.Click += Add_Click;
        _edit.Margin = new Thickness(0, 0, 8, 0);
        _edit.Click += Edit_Click;
        _delete.Margin = new Thickness(0, 0, 8, 0);
        _delete.Click += Delete_Click;
        _probe.Click += async (_, _) => await ProbeSelectedAsync();
        tools.Children.Add(add);
        tools.Children.Add(_edit);
        tools.Children.Add(_delete);
        tools.Children.Add(_probe);
        DockPanel.SetDock(tools, Dock.Top);
        root.Children.Add(tools);

        var selection = new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            Background = new SolidColorBrush(Color.FromRgb(249, 249, 249))
        };
        var selectionGrid = new Grid();
        selectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        selectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var selectionText = new StackPanel();
        _selectedTitle.FontWeight = FontWeights.SemiBold;
        _selectedTitle.FontSize = 14;
        _selectedDetails.Margin = new Thickness(0, 4, 0, 0);
        selectionText.Children.Add(_selectedTitle);
        selectionText.Children.Add(_selectedDetails);
        selectionGrid.Children.Add(selectionText);
        _selectedProbe.VerticalAlignment = VerticalAlignment.Center;
        _selectedProbe.Margin = new Thickness(16, 0, 0, 0);
        Grid.SetColumn(_selectedProbe, 1);
        selectionGrid.Children.Add(_selectedProbe);
        selection.Child = selectionGrid;
        DockPanel.SetDock(selection, Dock.Top);
        root.Children.Add(selection);

        _grid.ItemsSource = _items;
        _grid.AutoGenerateColumns = false;
        _grid.IsReadOnly = true;
        _grid.SelectionMode = DataGridSelectionMode.Single;
        _grid.SelectionUnit = DataGridSelectionUnit.FullRow;
        _grid.HeadersVisibility = DataGridHeadersVisibility.Column;
        _grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        _grid.CanUserAddRows = false;
        _grid.CanUserDeleteRows = false;
        _grid.Columns.Add(new DataGridTextColumn { Header = "Состояние", Binding = new Binding(nameof(ServerProfile.CurrentMarker)), Width = 100 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Профиль", Binding = new Binding(nameof(ServerProfile.Name)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Сервер", Binding = new Binding(nameof(ServerProfile.Endpoint)), Width = 180 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Пользователь", Binding = new Binding(nameof(ServerProfile.UserName)), Width = 145 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Автосинхр.", Binding = new Binding(nameof(ServerProfile.RealtimeText)), Width = 95 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Доступность", Binding = new Binding(nameof(ServerProfile.ProbeText)), Width = 125 });
        _grid.LoadingRow += (_, e) =>
        {
            if (e.Row.Item is ServerProfile p && p.IsCurrent)
            {
                e.Row.Background = new SolidColorBrush(Color.FromRgb(232, 247, 236));
                e.Row.FontWeight = FontWeights.SemiBold;
                e.Row.ToolTip = "Текущий активный профиль";
            }
            else
            {
                e.Row.Background = Brushes.Transparent;
                e.Row.FontWeight = FontWeights.Normal;
                e.Row.ToolTip = null;
            }
        };
        _grid.SelectionChanged += (_, _) => OnSelectionChanged();
        _grid.MouseDoubleClick += async (_, _) =>
        {
            if (_grid.SelectedItem is ServerProfile p && !p.IsCurrent)
                await ConnectSelectedAsync();
        };
        root.Children.Add(_grid);

        UpdateSelectionUi();
        return root;
    }

    private void ReloadProfiles()
    {
        try
        {
            var collab = _app.CollaborationSettings;
            var connected = _app.ConnectedToServer;
            var host = collab?.HostName ?? string.Empty;
            var port = collab?.Port ?? 0;
            var currentUser = collab?.UserName ?? string.Empty;

            var ini = ReadCollaborationFromIni();
            if (string.IsNullOrWhiteSpace(host)) host = ini.Host;
            if (port <= 0) port = ini.Port;
            if (string.IsNullOrWhiteSpace(currentUser)) currentUser = ini.UserName;

            var localRequested = IsPluginLocalMode(host, port);
            _isCurrentlyLocal = localRequested && !connected;

            _status.Text = connected ? "● Подключено" : "● Автономная работа";
            _status.Foreground = connected ? Brushes.DarkGreen : Brushes.DimGray;
            _current.Text = _isCurrentlyLocal
                ? "Текущий сервер: не используется (локальный режим)"
                : string.IsNullOrWhiteSpace(host) ? "Текущий сервер: не задан" : $"Текущий сервер: {host}:{port}";
            _user.Text = _isCurrentlyLocal
                ? $"Пользователь: {(string.IsNullOrWhiteSpace(currentUser) ? Environment.UserName : currentUser)}"
                : string.IsNullOrWhiteSpace(currentUser) ? "Пользователь: —" : $"Пользователь: {currentUser}";

            var selectedId = (_grid.SelectedItem as ServerProfile)?.Id;
            var list = _profiles.LoadAll();
            foreach (var p in list)
            {
                p.IsCurrent = connected &&
                              !string.IsNullOrWhiteSpace(host) &&
                              string.Equals(p.Host.Trim(), host.Trim(), StringComparison.OrdinalIgnoreCase) &&
                              p.Port == port &&
                              string.Equals(p.UserName.Trim(), currentUser.Trim(), StringComparison.OrdinalIgnoreCase);
                if (_probeStates.TryGetValue(p.Id, out var probeText))
                    p.ProbeText = probeText;
            }

            _items.Clear();
            foreach (var p in list) _items.Add(p);

            var selectId = selectedId;
            if (_preferences.RememberLastSelectedProfile && _preferences.LastSelectedProfileId.HasValue)
                selectId ??= _preferences.LastSelectedProfileId;
            selectId ??= list.FirstOrDefault(x => x.IsCurrent)?.Id;
            _grid.SelectedItem = selectId.HasValue ? _items.FirstOrDefault(x => x.Id == selectId.Value) : _items.FirstOrDefault();
            if (_grid.SelectedItem != null)
                _grid.ScrollIntoView(_grid.SelectedItem);

            UpdateSelectionUi();
        }
        catch (Exception ex)
        {
            _log.Error("Failed to refresh main window.", ex);
            MessageBox.Show(this, ex.Message, "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private (string Host, int Port, string UserName) ReadCollaborationFromIni()
    {
        try
        {
            var path = AppPaths.GetRengaSettingsIniPath(_app.ProductName);
            if (!File.Exists(path)) return (string.Empty, 0, string.Empty);

            var inSection = false;
            var host = string.Empty;
            var user = string.Empty;
            var port = 0;
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.StartsWith("["))
                {
                    inSection = line.Equals("[Collaboration]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!inSection) continue;

                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line[..eq].Trim();
                var value = line[(eq + 1)..].Trim();
                if (key.Equals("ServerHostName", StringComparison.OrdinalIgnoreCase)) host = value;
                else if (key.Equals("CollaborationUserName", StringComparison.OrdinalIgnoreCase)) user = value;
                else if (key.Equals("CollaborationPort", StringComparison.OrdinalIgnoreCase)) int.TryParse(value, out port);
            }
            return (host, port, user);
        }
        catch (Exception ex)
        {
            _log.Error("Cannot read Collaboration section from settings.ini.", ex);
            return (string.Empty, 0, string.Empty);
        }
    }

    private static bool IsPluginLocalMode(string host, int port)
    {
        try
        {
            if (File.Exists(AppPaths.LocalModeMarkerFile) &&
                File.ReadAllText(AppPaths.LocalModeMarkerFile).Trim().Equals("LOCAL", StringComparison.OrdinalIgnoreCase))
                return true;
            return string.IsNullOrWhiteSpace(host);
        }
        catch
        {
            return string.IsNullOrWhiteSpace(host);
        }
    }

    private void OnSelectionChanged()
    {
        if (_preferences.RememberLastSelectedProfile && _grid.SelectedItem is ServerProfile p)
        {
            _preferences.LastSelectedProfileId = p.Id;
            try { _preferencesRepo.Save(_preferences); }
            catch (Exception ex) { _log.Error("Cannot save last selected profile.", ex); }
        }
        UpdateSelectionUi();
    }

    private void UpdateSelectionUi()
    {
        var selected = _grid.SelectedItem as ServerProfile;
        var hasSelection = selected != null;
        _edit.IsEnabled = hasSelection;
        _delete.IsEnabled = hasSelection;
        _probe.IsEnabled = hasSelection;

        if (selected == null)
        {
            _selectedTitle.Text = "Профиль не выбран";
            _selectedDetails.Text = "Выберите сервер из списка или добавьте новый профиль.";
            _selectedProbe.Text = string.Empty;
            _connect.Content = "Подключиться";
            _connect.IsEnabled = false;
        }
        else
        {
            _selectedTitle.Text = selected.IsCurrent ? $"● {selected.Name} — текущий профиль" : selected.Name;
            _selectedDetails.Text = $"{selected.Endpoint}   •   Пользователь: {selected.UserName}   •   Автосинхр.: {selected.RealtimeText}   •   Ключ: {selected.KeyStateText}";
            _selectedProbe.Text = selected.ProbeText == "—" ? "Доступность: не проверялась" : $"Доступность: {selected.ProbeText}";
            _selectedProbe.Foreground = selected.ProbeText.StartsWith("Доступен", StringComparison.OrdinalIgnoreCase)
                ? Brushes.DarkGreen
                : selected.ProbeText.StartsWith("Недоступен", StringComparison.OrdinalIgnoreCase) ? Brushes.DarkRed : Brushes.DimGray;
            _connect.Content = selected.IsCurrent ? "Уже подключено" : "Подключиться";
            _connect.IsEnabled = !selected.IsCurrent;
        }

        _local.Content = _isCurrentlyLocal ? "Автономный режим активен" : "Остаться работать локально";
        _local.IsEnabled = !_isCurrentlyLocal;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var editor = new ProfileEditorWindow(new ServerProfile()) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            try { _profiles.Save(editor.Result); ReloadProfiles(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_grid.SelectedItem is not ServerProfile selected) return;
        var editor = new ProfileEditorWindow(selected) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            try
            {
                _profiles.Save(editor.Result);
                _probeStates.Remove(selected.Id);
                ReloadProfiles();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_grid.SelectedItem is not ServerProfile selected) return;
        if (selected.IsCurrent)
        {
            if (MessageBox.Show(this,
                    $"Профиль «{selected.Name}» сейчас активен.\n\nУдаление профиля не отключит текущую Renga от сервера, но сохранённые данные профиля будут удалены. Продолжить?",
                    "Удаление активного профиля",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No) != MessageBoxResult.Yes)
                return;
        }
        else if (MessageBox.Show(this,
                     $"Удалить профиль «{selected.Name}»?\n\n{selected.Endpoint}\nПользователь: {selected.UserName}\n\nСохранённый ключ доступа также будет удалён.",
                     "Renga Server Switcher", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;

        try
        {
            _profiles.Delete(selected.Id);
            _probeStates.Remove(selected.Id);
            ReloadProfiles();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async Task ProbeSelectedAsync()
    {
        if (_grid.SelectedItem is not ServerProfile selected) return;
        var oldCursor = Cursor;
        try
        {
            _probeStates[selected.Id] = "Проверка...";
            selected.ProbeText = "Проверка...";
            _grid.Items.Refresh();
            UpdateSelectionUi();
            _probe.IsEnabled = false;
            Cursor = System.Windows.Input.Cursors.Wait;

            var result = await ServerProbeService.ProbeAsync(selected.Host, selected.Port, TimeSpan.FromSeconds(4));
            var state = result.Success ? $"Доступен ({result.ElapsedMilliseconds} мс)" : "Недоступен";
            _probeStates[selected.Id] = state;
            selected.ProbeText = state;
            _grid.Items.Refresh();
            UpdateSelectionUi();

            MessageBox.Show(this, result.Message,
                result.Success ? "Сервер доступен" : "Сервер недоступен",
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        finally
        {
            Cursor = oldCursor;
            _probe.IsEnabled = _grid.SelectedItem != null;
        }
    }

    private async Task ConnectSelectedAsync()
    {
        if (_grid.SelectedItem is not ServerProfile selected) return;
        if (selected.IsCurrent)
        {
            MessageBox.Show(this, "Этот профиль уже активен. Перезапуск не требуется.", "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await StartSwitchAsync(selected, false);
    }

    private async Task StartSwitchAsync(ServerProfile? profile, bool localMode)
    {
        try
        {
            if (!localMode && profile == null) return;

            if (localMode)
            {
                var ini = ReadCollaborationFromIni();
                if (IsPluginLocalMode(ini.Host, ini.Port) && !_app.ConnectedToServer)
                {
                    _log.Info("Local mode requested while Renga is already disconnected and local-mode marker is active. Restart skipped.");
                    MessageBox.Show(this, "Renga уже работает автономно. Перезапуск не требуется.", "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Information);
                    ReloadProfiles();
                    return;
                }
            }

            if (!localMode && profile!.IsCurrent)
            {
                MessageBox.Show(this, "Этот профиль уже активен. Перезапуск не требуется.", "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!localMode && _preferences.CheckServerBeforeConnect)
            {
                var probe = await ServerProbeService.ProbeAsync(profile!.Host, profile.Port, TimeSpan.FromSeconds(4));
                _probeStates[profile.Id] = probe.Success ? $"Доступен ({probe.ElapsedMilliseconds} мс)" : "Недоступен";
                profile.ProbeText = _probeStates[profile.Id];
                _grid.Items.Refresh();
                UpdateSelectionUi();

                if (!probe.Success)
                {
                    var answer = MessageBox.Show(this,
                        probe.Message + "\n\nВсё равно применить этот профиль и перезапустить Renga?",
                        "Сервер недоступен", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                    if (answer != MessageBoxResult.Yes) return;
                }
            }

            if (_preferences.ConfirmBeforeRestart)
            {
                var confirm = new RestartConfirmationDialog(profile, localMode) { Owner = this };
                if (confirm.ShowDialog() != true) return;
            }

            if (!EnsureProjectSafeForRestart()) return;

            _restart.Start(_app, profile, localMode);
            var target = localMode ? "автономный режим" : $"«{profile!.Name}» ({profile.Endpoint})";
            _log.Info($"Renga restart requested. Target={target}.");
            _app.Quit();
        }
        catch (Exception ex)
        {
            _log.Error("Restart failed to start.", ex);
            MessageBox.Show(this, ex.Message, "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool EnsureProjectSafeForRestart()
    {
        if (!_app.HasProject()) return true;
        var project = _app.Project;
        if (project == null || !project.HasUnsavedChanges()) return true;

        var dialog = new SaveBeforeRestartDialog(project.FilePath) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Choice == SaveRestartChoice.Cancel) return false;

        if (dialog.Choice == SaveRestartChoice.Discard)
        {
            var closeResult = _app.CloseProject(true);
            if (closeResult != 0)
            {
                MessageBox.Show(this, "Renga не смогла закрыть проект без сохранения. Перезапуск отменён.", "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        int saveResult;
        if (project.HasFile())
        {
            saveResult = project.Save();
        }
        else
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Сохранить проект перед перезапуском Renga",
                Filter = "Проект Renga (*.rnp)|*.rnp",
                DefaultExt = ".rnp",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (saveDialog.ShowDialog(this) != true) return false;
            saveResult = project.SaveAs(saveDialog.FileName, Renga.ProjectType.ProjectType_Project, true);
        }

        if (saveResult != 0)
        {
            MessageBox.Show(this, "Не удалось сохранить проект. Перезапуск отменён, чтобы исключить потерю данных.", "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        return true;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var copy = new PluginPreferences
        {
            AutoShowOnStartup = _preferences.AutoShowOnStartup,
            RememberLastSelectedProfile = _preferences.RememberLastSelectedProfile,
            CheckServerBeforeConnect = _preferences.CheckServerBeforeConnect,
            ConfirmBeforeRestart = _preferences.ConfirmBeforeRestart,
            LastSelectedProfileId = _preferences.LastSelectedProfileId
        };
        var win = new PreferencesWindow(copy) { Owner = this };
        if (win.ShowDialog() == true)
        {
            _preferences = win.Result;
            _preferencesRepo.Save(_preferences);
        }
    }
}
