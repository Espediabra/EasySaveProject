using EasySaveProject.Core.Localization;
using EasyLog;

public class LogView
{
    private readonly MenuComponent _menu;
    private readonly LocalizationService _loc;
    private readonly LogViewModel _vm;

    public LogView(MenuComponent menu, LocalizationService loc, LogViewModel vm)
    {
        _menu = menu;
        _loc = loc;
        _vm = vm;
    }

    public void Show()
    {
        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("Logs.Title"));

            var options = new List<string>
            {
                _loc.T("Logs.Today"),
                _loc.T("Logs.ByLevel"),
                _loc.T("Return")
            };

            int choice = _menu.Select(options);

            switch (choice)
            {
                case 0:
                    ShowTodayLogs();
                    break;

                case 1:
                    ShowLogsByLevel();
                    break;

                case 2:
                    return;
            }
        }
    }

    private void ShowTodayLogs()
    {
        var logs = _vm.GetTodayLogs();

        Console.Clear();
        HeaderComponent.Render(_loc.T("Logs.TodayHeader"));

        PrintSimple(logs);

        Console.ReadKey();
    }

    private void ShowLogsByLevel()
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Logs.SelectLevel"));

        var options = new List<string>
        {
            _loc.T("Log.Level.Info"),
            _loc.T("Log.Level.Warning"),
            _loc.T("Log.Level.Error"),
            _loc.T("Return")
        };

        int choice = _menu.Select(options);

        if (choice == 3)
            return;

        var level = _vm.MapChoiceToLevel(choice);

        var logs = _vm.GetLogsByLevel(level);

        Console.Clear();
        HeaderComponent.Render(
            string.Format(_loc.T("Logs.LevelHeader"), level)
        );

        PrintSimple(logs);

        Console.ReadKey();
    }

    private void PrintSimple(List<LogDto> entries)
    {
        Console.WriteLine($"Time       Level      Job                Message");
        Console.WriteLine(new string('-', 80));

        foreach (var e in entries)
        {
            Console.WriteLine(
                $"{e.Time:HH:mm:ss}   {e.Level,-10} {e.Job,-18} {e.Message}"
            );
        }
    }
}