using EasyLog;

namespace EasySaveProject.Infrastructure.Logging;

public class CompositeLogProvider : ILogProvider
{
    private readonly IReadOnlyList<ILogProvider> _providers;

    public CompositeLogProvider(params ILogProvider[] providers)
    {
        _providers = providers;
    }

    public void Write(LogEntry entry)
    {
        foreach (var provider in _providers)
        {
            try { provider.Write(entry); }
            catch { }
        }
    }

    public List<LogEntry> ReadByDate(DateTime date)
    {
        foreach (var provider in _providers)
        {
            try
            {
                var result = provider.ReadByDate(date);
                if (result.Count > 0) return result;
            }
            catch { }
        }
        return new List<LogEntry>();
    }
}
