using EasySaveProject.Infrastructure.Process;

namespace EasySaveProject.Infrastructure.Monitoring;

public class BusinessSoftwareWatcher
{
    private readonly string _processName;

    public BusinessSoftwareWatcher(string processName)
    {
        _processName = processName?.Trim() ?? string.Empty;
    }

    public bool IsRunning() => ProcessHelper.IsProcessRunning(_processName);
}
