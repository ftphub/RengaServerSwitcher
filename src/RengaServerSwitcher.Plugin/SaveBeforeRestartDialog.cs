using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RengaServerSwitcher;

internal enum SaveRestartChoice { Save, Discard, Cancel }

internal sealed class SaveBeforeRestartDialog : Window
{
    public SaveRestartChoice Choice { get; private set; } = SaveRestartChoice.Cancel;

    public SaveBeforeRestartDialog(string projectPath)
    {
        Title = "Сохранить изменения в проекте?";
        Width = 650;
        SizeToContent = SizeToContent.Height;
        MinHeight = 250;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var root = new Grid { Margin = new Thickness(24) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(new TextBlock
        {
            Text = "В проекте есть несохранённые изменения.",
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var textStack = new StackPanel();
        textStack.Children.Add(new TextBlock
        {
            Text = "Сохранить изменения перед перезапуском Renga?",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        });
        if (!string.IsNullOrWhiteSpace(projectPath))
            textStack.Children.Add(new TextBlock
            {
                Text = projectPath,
                Margin = new Thickness(0, 12, 0, 0),
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            });
        Grid.SetRow(textStack, 1);
        root.Children.Add(textStack);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 24, 0, 0)
        };
        var save = new Button { Content = "Сохранить и перезапустить", Width = 205, Height = 36, IsDefault = true };
        var discard = new Button { Content = "Продолжить без сохранения", Width = 195, Height = 36, Margin = new Thickness(10, 0, 0, 0) };
        var cancel = new Button { Content = "Отмена", Width = 100, Height = 36, Margin = new Thickness(10, 0, 0, 0), IsCancel = true };

        save.Click += (_, _) => { Choice = SaveRestartChoice.Save; DialogResult = true; };
        discard.Click += (_, _) =>
        {
            if (MessageBox.Show(this,
                    "Несохранённые изменения будут потеряны.\n\nВы действительно хотите продолжить без сохранения?",
                    "Renga Server Switcher",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                Choice = SaveRestartChoice.Discard;
                DialogResult = true;
            }
        };
        cancel.Click += (_, _) => { Choice = SaveRestartChoice.Cancel; DialogResult = false; };

        buttons.Children.Add(save);
        buttons.Children.Add(discard);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);
        Content = root;
    }
}
