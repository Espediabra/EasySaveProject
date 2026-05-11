// using EasySaveProject.Helpers;

// class Program
// {
//     static void Main(string[] args)
//     {
//         var app = new App();

//         if (args.Length == 0)
//         {
//             app.Run();
//             // Console.WriteLine("Aucun args fournis");
//             return;
//         }
//         Console.WriteLine($"Les args sont {string.Join(", ", args)}");
//         app.RunCli(args);
//     }
// }

using Avalonia;
using EasySaveProject.UI.Avalonia.Views;
using EasySaveProject.UI.Avalonia.ViewModels;

class Program
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

