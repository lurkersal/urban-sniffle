using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace IndexEditor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string? folderToOpen = null;
            if (desktop.Args is { Length: > 0 })
            {
                folderToOpen = desktop.Args[0];
            }
            desktop.MainWindow = new MainWindow(folderToOpen);
        }

        base.OnFrameworkInitializationCompleted();
    }
}