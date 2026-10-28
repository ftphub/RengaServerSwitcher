using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace RengaServerSwitcher;

internal sealed class PreferencesWindow : Window
{
    private readonly CheckBox _autoShow = new() { Content = "Показывать Server Switcher при запуске Renga" };
    private readonly CheckBox _remember = new() { Content = "Запоминать последний выбранный профиль" };
    private readonly CheckBox _check = new() { Content = "Проверять доступность сервера перед подключением" };
    private readonly CheckBox _confirm = new() { Content = "Показывать подтверждение перед перезапуском" };
    private readonly PluginPreferences _preferences;

    public PluginPreferences Result => _preferences;

    public PreferencesWindow(PluginPreferences preferences)
    {
        _preferences = preferences;
        Title = "Настройки Renga Server Switcher";
        Width = 640;
        SizeToContent = SizeToContent.Height;
        MinHeight = 340;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _autoShow.IsChecked = preferences.AutoShowOnStartup;
        _remember.IsChecked = preferences.RememberLastSelectedProfile;
        _check.IsChecked = preferences.CheckServerBeforeConnect;
        _confirm.IsChecked = preferences.ConfirmBeforeRestart;

        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var stack = new StackPanel();
        foreach (var cb in new[] { _autoShow, _remember, _check, _confirm })
        {
            cb.Margin = new Thickness(0, 0, 0, 12);
            stack.Children.Add(cb);
        }
        stack.Children.Add(new TextBlock
        {
            Text = "Отключите первый пункт, если окно должно открываться только по кнопке Renga Server Switcher на панели Renga.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            Margin = new Thickness(0, 0, 0, 12)
        });
        root.Children.Add(stack);

        var utility = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 20) };
        var openLogs = new Button { Content = "Открыть папку журналов", Width = 190, Height = 32, Margin = new Thickness(0, 0, 10, 0) };
        var openData = new Button { Content = "Открыть папку данных", Width = 180, Height = 32 };
        openLogs.Click += (_, _) => OpenFolder(AppPaths.LogsDirectory);
        openData.Click += (_, _) => OpenFolder(AppPaths.Root);
        utility.Children.Add(openLogs);
        utility.Children.Add(openData);
        Grid.SetRow(utility, 1);
        root.Children.Add(utility);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var save = new Button { Content = "Сохранить", Width = 115, Height = 34, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
        var cancel = new Button { Content = "Отмена", Width = 100, Height = 34, IsCancel = true };

        save.Click += (_, _) =>
        {
            _preferences.AutoShowOnStartup = _autoShow.IsChecked == true;
            _preferences.RememberLastSelectedProfile = _remember.IsChecked == true;
            _preferences.CheckServerBeforeConnect = _check.IsChecked == true;
            _preferences.ConfirmBeforeRestart = _confirm.IsChecked == true;
            DialogResult = true;
        };

        buttons.Children.Add(save);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);
        Content = root;
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }
}
