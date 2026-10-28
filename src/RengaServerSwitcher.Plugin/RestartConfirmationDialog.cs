using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RengaServerSwitcher;

internal sealed class RestartConfirmationDialog : Window
{
    public RestartConfirmationDialog(ServerProfile? profile, bool localMode)
    {
        Title = "Перезапуск Renga";
        Width = 560;
        SizeToContent = SizeToContent.Height;
        MinHeight = 260;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new Grid { Margin = new Thickness(24) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = localMode ? "Перейти в автономный режим?" : $"Подключиться к профилю «{profile!.Name}»?",
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        root.Children.Add(title);

        var details = new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var stack = new StackPanel();
        if (localMode)
        {
            stack.Children.Add(new TextBlock { Text = "Renga будет перезапущена без подключения к сохранённым серверным профилям." });
        }
        else
        {
            stack.Children.Add(new TextBlock { Text = $"Сервер: {profile!.Endpoint}" });
            stack.Children.Add(new TextBlock { Text = $"Пользователь: {profile.UserName}", Margin = new Thickness(0, 4, 0, 0) });
            stack.Children.Add(new TextBlock { Text = $"Автосинхронизация: {profile.RealtimeText}", Margin = new Thickness(0, 4, 0, 0) });
        }
        details.Child = stack;
        Grid.SetRow(details, 1);
        root.Children.Add(details);

        var bottom = new Grid();
        bottom.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        bottom.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var note = new TextBlock
        {
            Text = "Для применения параметров Renga необходимо закрыть и запустить заново. Если в проекте есть несохранённые изменения, перед закрытием будет предложено их сохранить.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.75,
            Margin = new Thickness(0, 0, 0, 18)
        };
        bottom.Children.Add(note);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var restart = new Button { Content = "Перезапустить сейчас", Width = 165, Height = 34, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
        var cancel = new Button { Content = "Отмена", Width = 100, Height = 34, IsCancel = true };
        restart.Click += (_, _) => DialogResult = true;
        buttons.Children.Add(restart);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 1);
        bottom.Children.Add(buttons);

        Grid.SetRow(bottom, 2);
        root.Children.Add(bottom);
        Content = root;
    }
}
