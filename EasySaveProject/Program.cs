using EasySaveProject.Helpers;

class Program
{
    static void Main(string[] args)
    {
        var app = new App();

        if (args.Length == 0)
        {
            app.Run();
            // Console.WriteLine("Aucun args fournis");
            return;
        }
        Console.WriteLine($"Les args sont {string.Join(", ", args)}");
        app.RunCli(args);
    }
}