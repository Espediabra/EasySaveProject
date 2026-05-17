using EasyLog;
using EasySaveProject.Core.Services;

public class LogViewModel
{
    private static LogService Log => LogService.Instance;

    public LogLevel MapChoiceToLevel(int choice) => choice switch
    {
        0 => LogLevel.INFO,
        1 => LogLevel.WARNING,
        2 => LogLevel.ERROR,
        _ => LogLevel.INFO
    };

    public List<LogDto> GetTodayLogs() => ToDto(Log.GetByDate(DateTime.Today));

    public List<LogDto> GetLogsByLevel(LogLevel level) => ToDto(Log.GetByLevel(DateTime.Today, level));

    public string GetLogDirectory() => Log.GetLogDirectory();

    private static List<LogDto> ToDto(List<LogEntry> entries) =>
        entries.Select(e => new LogDto
        {
            Time    = e.Timestamp,
            Level   = e.Level.ToString(),
            Job     = e.JobName,
            Message = e.Message
        }).ToList();
}
