using EasyLog;
using EasySaveProject.Core.Services;

public class LogViewModel
{
    private readonly LogService _logService;

    public LogViewModel(LogService logService)
    {
        _logService = logService;
    }

    public LogLevel MapChoiceToLevel(int choice)
    {
        return choice switch
        {
            0 => LogLevel.INFO,
            1 => LogLevel.WARNING,
            2 => LogLevel.ERROR,
            _ => LogLevel.INFO
        };
    }

    public List<LogDto> GetTodayLogs()
    {
        return GetLogsByDate(DateTime.Today);
    }

    public List<LogDto> GetLogsByLevel(LogLevel level)
    {
        return GetLogsByDateAndLevel(DateTime.Today, level);
    }

    private List<LogDto> GetLogsByDate(DateTime date)
    {
        var logs = _logService.GetByDate(date);

        return logs.Select(e => new LogDto
        {
            Time = e.Timestamp,
            Level = e.Level.ToString(),
            Job = e.JobName,
            Message = e.Message
        }).ToList();
    }

    private List<LogDto> GetLogsByDateAndLevel(DateTime date, LogLevel level)
    {
        var logs = _logService.GetByLevel(date, level);

        return logs.Select(e => new LogDto
        {
            Time = e.Timestamp,
            Level = e.Level.ToString(),
            Job = e.JobName,
            Message = e.Message
        }).ToList();
    }
}