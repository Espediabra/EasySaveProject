using EasySave.Models;

public class LogService
{
    private static LogService _instance;

    private LogService() { }

    public static LogService GetInstance()
    {
        if (_instance == null)
            _instance = new LogService();

        return _instance;
    }

    public void CreateLog(LogEntry entry)
    {
    }
}