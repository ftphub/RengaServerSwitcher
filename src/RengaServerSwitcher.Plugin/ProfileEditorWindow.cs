using System.Windows;
using System.Windows.Controls;

namespace RengaServerSwitcher;

internal sealed class ProfileEditorWindow : Window
{
    private readonly TextBox _name = new();
    private readonly TextBox _host = new();
    private readonly TextBox _port = new();
    private readonly TextBox _user = new();
    private readonly PasswordBox _keyPassword = new();
    private readonly TextBox _keyVisible = new();
    private readonly Button _toggleKey = new() { Content = "👁", Width = 42, Height = 28, ToolTip = "Показать / скрыть ключ доступа" };
    private readonly CheckBox _realtime = new() { Content = "Автоматически синхронизировать изменения" };
    private readonly ServerProfile _profile;
    private bool _keyShown;

    public ServerProfile Result => _profile;

    public ProfileEditorWindow(ServerProfile profile)
    {
        _profile = profile.Clone();
        Title = profile.Name.Length == 0 ? "Добавить сервер" : "Изменить сервер";
        Width = 570;
        Height = 455;
        MinWidth = 570;
        MinHeight = 455;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Content = Build();

        _name.Text = _profile.Name;
        _host.Text = _profile.Host;
        _port.Text = _profile.Port.ToString();
        _user.Text = _profile.UserName;
        _keyPassword.Password = _profile.AccessKey;
        _keyVisible.Text = _profile.AccessKey;
        _realtime.IsChecked = _profile.RealtimeCollaboration;
        _keyVisible.Visibility = Visibility.Collapsed;
        _toggleKey.Click += (_, _) => ToggleKey();
    }

    private UIElement Build()
    {
        var root = new Grid { Margin = new Thickness(20) };
        for (var i = 0; i < 7; i++) root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        AddField(root, 0, "Название профиля:", _name);
        AddField(root, 1, "IP / DNS:", _host);
        AddField(root, 2, "Порт:", _port);
        AddField(root, 3, "Пользователь:", _user);

        var keyGrid = new Grid();
        keyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        keyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _keyPassword.Height = 28;
        _keyVisible.Height = 28;
        keyGrid.Children.Add(_keyPassword);
        keyGrid.Children.Add(_keyVisible);
        Grid.SetColumn(_toggleKey, 1);
        _toggleKey.Margin = new Thickness(8, 0, 0, 0);
        keyGrid.Children.Add(_toggleKey);
        AddField(root, 4, "Ключ доступа:", keyGrid);

        var keyHint = new TextBlock
        {
            Text = "Ключ хранится локально в зашифрованном виде (Windows DPAPI).",
            Opacity = 0.7,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(keyHint, 5); Grid.SetColumn(keyHint, 1);
        root.Children.Add(keyHint);

        Grid.SetRow(_realtime, 6); Grid.SetColumn(_realtime, 1);
        _realtime.Margin = new Thickness(0, 6, 0, 6);
        root.Children.Add(_realtime);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var save = new Button { Content = "Сохранить", Width = 115, Height = 32, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
        var cancel = new Button { Content = "Отмена", Width = 100, Height = 32, IsCancel = true };
        save.Click += Save_Click;
        buttons.Children.Add(save); buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 8); Grid.SetColumnSpan(buttons, 2);
        root.Children.Add(buttons);
        return root;
    }

    private static void AddField(Grid grid, int row, string label, UIElement control)
    {
        var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 7, 12, 7) };
        Grid.SetRow(text, row); Grid.SetColumn(text, 0); grid.Children.Add(text);
        if (control is FrameworkElement fe) fe.Margin = new Thickness(0, 7, 0, 7);
        Grid.SetRow(control, row); Grid.SetColumn(control, 1); grid.Children.Add(control);
    }

    private void ToggleKey()
    {
        _keyShown = !_keyShown;
        if (_keyShown)
        {
            _keyVisible.Text = _keyPassword.Password;
            _keyVisible.Visibility = Visibility.Visible;
            _keyPassword.Visibility = Visibility.Collapsed;
            _toggleKey.ToolTip = "Скрыть ключ доступа";
        }
        else
        {
            _keyPassword.Password = _keyVisible.Text;
            _keyVisible.Visibility = Visibility.Collapsed;
            _keyPassword.Visibility = Visibility.Visible;
            _toggleKey.ToolTip = "Показать ключ доступа";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(_port.Text.Trim(), out var port) || port is < 1 or > 65535)
                throw new InvalidOperationException("Порт должен быть числом от 1 до 65535.");

            _profile.Name = _name.Text.Trim();
            _profile.Host = _host.Text.Trim();
            _profile.Port = port;
            _profile.UserName = _user.Text.Trim();
            _profile.AccessKey = _keyShown ? _keyVisible.Text : _keyPassword.Password;
            _profile.RealtimeCollaboration = _realtime.IsChecked == true;

            if (string.IsNullOrWhiteSpace(_profile.Name)) throw new InvalidOperationException("Введите название профиля.");
            if (string.IsNullOrWhiteSpace(_profile.Host)) throw new InvalidOperationException("Введите IP-адрес или DNS-имя сервера.");
            if (string.IsNullOrWhiteSpace(_profile.UserName)) throw new InvalidOperationException("Введите имя пользователя.");

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Renga Server Switcher", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
