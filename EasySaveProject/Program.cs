using Avalonia;
using EasySaveProject.UI.Avalonia.Views;
using EasySaveProject.UI.Avalonia.ViewModels;

namespace EasySaveProject.UI.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // ── Mode UI Avalonia (défaut) ──────────────────────────────────────
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<EasySaveProject.UI.Avalonia.AvaloniaApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

