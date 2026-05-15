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
using System;

namespace EasySaveProjectUI;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
