// using EasySaveProject.Helpers;
<<<<<<< HEAD
=======

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
>>>>>>> f67e00b0db0d5bd98353b478cb7c9bebfa56a41f

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

/// <summary>
/// Point d'entrée unique : lance l'UI Avalonia.
/// L'ancienne App console reste disponible dans App/App.cs si besoin.
/// Pour revenir au mode console : commentez ce bloc et décommentez l'ancien main.
/// </summary>
class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // ── Mode UI Avalonia (défaut) ──────────────────────────────────────
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
<<<<<<< HEAD

        // ── Mode console (CLI) — décommentez pour revenir à l'ancienne app ─
        // var app = new App();
        // if (args.Length == 0) app.Run();
        // else app.RunCli(args[0]);
=======
>>>>>>> f67e00b0db0d5bd98353b478cb7c9bebfa56a41f
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<EasySaveProject.UI.Avalonia.AvaloniaApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
<<<<<<< HEAD
=======

>>>>>>> f67e00b0db0d5bd98353b478cb7c9bebfa56a41f
