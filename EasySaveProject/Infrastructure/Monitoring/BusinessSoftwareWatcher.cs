using EasySaveProject.Core.Services;
using EasySaveProject.Infrastructure.Process;
using EasySaveProject.Models;

namespace EasySaveProject.Infrastructure.Monitoring;

public class BusinessSoftwareWatcher
{
    private readonly ConfigService _configService;

    public BusinessSoftwareWatcher(ConfigService configService)
    {
        _configService = configService;
    }

    public bool IsRunning()
    {
        foreach (var name in GetCurrentList())
        {
            if (ProcessHelper.IsProcessRunning(name))
                return true;
        }
        return false;
    }

    public List<string> GetRunningProcesses()
    {
        var running = new List<string>();
        foreach (var name in GetCurrentList())
        {
            if (ProcessHelper.IsProcessRunning(name))
                running.Add(name);
        }
        return running;
    }

    public void WaitUntilFree(
        PauseService pauseService,
        LogService logService,
        BackupJob job,
        State state,
        StateService stateService)
    {
        var running = GetRunningProcesses();
        if (running.Count == 0)
            return;

        string processList = string.Join(", ", running);

        logService.LogWarning(
            job.Name, job.SourcePath, job.TargetPath, 0, 0,
            $"Backup paused: business software detected ({processList})"
        );

        pauseService.Pause();
        state.Status   = "Paused";
        state.BlockedBy = processList;
        state.Timestamp = DateTime.Now;
        stateService.Update(state);

        while (IsRunning())
        {
            Thread.Sleep(1000);
        }

        logService.LogInfo(
            job.Name, job.SourcePath, job.TargetPath, 0, 0,
            "Backup resumed: business software closed"
        );

        pauseService.Resume();
        state.Status   = "Active";
        state.BlockedBy = string.Empty;
        state.Timestamp = DateTime.Now;
        stateService.Update(state);
    }

    private List<string> GetCurrentList()
    {
        return _configService.Load().BusinessSoftware
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }
}
