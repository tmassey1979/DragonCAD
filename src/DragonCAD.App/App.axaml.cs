using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace DragonCAD.App;

public partial class App : Application
{
    public override void Initialize()
    {
        ConfigureThemeAndResources();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureThemeAndResources()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());

        Resources["DragonShellBackground"] = Brush("#0B0F14");
        Resources["DragonPanelBackground"] = Brush("#121820");
        Resources["DragonPanelHeader"] = Brush("#18212B");
        Resources["DragonBorder"] = Brush("#2A3542");
        Resources["DragonText"] = Brush("#E9EEF5");
        Resources["DragonMutedText"] = Brush("#A7B2C0");
        Resources["DragonAccent"] = Brush("#E9413A");
        Resources["DragonGreen"] = Brush("#36C275");
    }

    private static SolidColorBrush Brush(string color) =>
        new(Color.Parse(color));
}
