using EasySaveProject.Core.Localization;
using EasySaveProject.Core.Services;
using EasyLog;
using System.ComponentModel;

public class LogView
{
    private readonly MenuComponent _menu;
    private readonly LocalizationService _loc;
    private readonly LogService _logService;

    public LogView(MenuComponent menu, LocalizationService loc, LogService logService)
    {
        _menu = menu;
        _loc = loc;
        _logService = logService;
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
        var logs = _logService.GetByDate(DateTime.Today);

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

        var level = choice switch
        {
            0 => LogLevel.INFO,
            1 => LogLevel.WARNING,
            2 => LogLevel.ERROR,
            _ => LogLevel.INFO
        };

        var logs = _logService.GetByLevel(DateTime.Today, level);

        Console.Clear();
        HeaderComponent.Render(
            string.Format(_loc.T("Logs.LevelHeader"), level)
        );

        PrintSimple(logs);

        Console.ReadKey();
    }

    private void PrintSimple(List<LogEntry> entries)
    {
        Console.WriteLine(
            $"Time       Level      Job                Message"
        );

        Console.WriteLine(new string('-', 80));

        foreach (var e in entries)
        {
            Console.WriteLine(
                $"{e.Timestamp:HH:mm:ss}  {e.Level,-10} {e.JobName,-18} {e.Message}"
            );
        }
    }

    public void PrintDetailed(List<LogEntry> entries)
    {
        foreach (var e in entries)
        {
            Console.WriteLine(new string('═', 60));

            Console.WriteLine($"  {_loc.T("Logs.Details.Timestamp"),-15}: {e.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Level"),-15}: {_loc.T($"Log.Level.{e.Level}")}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Job"),-15}: {e.JobName}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Source"),-15}: {e.SourcePath}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Destination"),-15}: {e.TargetPath}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Size"),-15}: {e.FileSizeBytes} {_loc.T("Logs.Bytes")}");
            Console.WriteLine(
                $"  {_loc.T("Logs.Details.TransferTime"),-15}: " +
                $"{(e.TransferTimeMs < 0
                    ? _loc.T("Logs.Error")
                    : $"{e.TransferTimeMs} {_loc.T("Logs.Milliseconds")}")}"
            );
            Console.WriteLine($"  {_loc.T("Logs.Details.Message"),-15}: {e.Message}");
        }

        Console.WriteLine(new string('═', 60));
    }
}