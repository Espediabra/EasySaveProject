using EasySaveProject.Infrastructure.Process;

namespace EasySaveProject.Infrastructure.Monitoring;

public class BusinessSoftwareWatcher
{
    private readonly List<string> _processNames;

    public BusinessSoftwareWatcher(List<string> processNames)
    {
        _processNames = processNames
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    public bool IsRunning()
    {
        return _processNames.Any(name => ProcessHelper.IsProcessRunning(name));
    }

    public string? GetRunningName()
    {
        return _processNames.FirstOrDefault(name => ProcessHelper.IsProcessRunning(name));
    }
}
