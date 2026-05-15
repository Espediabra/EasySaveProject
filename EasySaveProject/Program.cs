using Avalonia;
using System;

namespace EasySaveProject.UI.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
#if DEBUG
            .WithDeveloperTools()
#endif
            ;
}