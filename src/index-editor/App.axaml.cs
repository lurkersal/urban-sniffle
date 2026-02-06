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
        System.Console.WriteLine($"[TRACE] App.OnFrameworkInitializationCompleted called; Lifetime={ApplicationLifetime?.GetType().FullName}");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string? folderToOpen = null;
            if (desktop.Args is { Length: > 0 })
            {
                folderToOpen = desktop.Args[0];
                System.Console.WriteLine($"[TRACE] App args[0] = '{folderToOpen}'");
            }
            desktop.MainWindow = new MainWindow(folderToOpen);
            System.Console.WriteLine("[TRACE] MainWindow created and assigned to desktop.MainWindow");
        }

        base.OnFrameworkInitializationCompleted();
    }
}