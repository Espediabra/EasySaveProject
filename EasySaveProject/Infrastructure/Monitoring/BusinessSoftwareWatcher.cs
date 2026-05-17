using EasySaveProject.Core.Services;
using EasySaveProject.Infrastructure.Process;
using EasySaveProject.Models;

namespace EasySaveProject.Infrastructure.Monitoring;

public class BusinessSoftwareWatcher
{
    private readonly ConfigService _configService;

    // 0 = not notified, 1 = paused notification already sent.
    // Interlocked ensures only ONE toast fires even when multiple parallel jobs
    // all hit WaitUntilFree at the same time.
    private int _pausedNotificationSent = 0;

    /// <summary>Fired once (thread-safe) when backups first pause due to business software.</summary>
    public event Action<string>? OnJobPaused;

    /// <summary>Fired once (thread-safe) when the blocking software closes and backups resume.</summary>
    public event Action<string>? OnJobResumed;

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

        // Fire the pause event only once across all parallel jobs
        if (System.Threading.Interlocked.CompareExchange(ref _pausedNotificationSent, 1, 0) == 0)
            OnJobPaused?.Invoke(processList);

        pauseService.Pause();
        state.Status   = "Paused";
        state.BlockedBy = processList;
        state.Timestamp = DateTime.Now;
        stateService.Update(state);

        while (IsRunning())
            Thread.Sleep(1000);

        logService.LogInfo(
            job.Name, job.SourcePath, job.TargetPath, 0, 0,
            "Backup resumed: business software closed"
        );

        pauseService.Resume();
        state.Status   = "Active";
        state.BlockedBy = string.Empty;
        state.Timestamp = DateTime.Now;
        stateService.Update(state);

        // Fire the resume event only once
        if (System.Threading.Interlocked.CompareExchange(ref _pausedNotificationSent, 0, 1) == 1)
            OnJobResumed?.Invoke(processList);
    }

    private List<string> GetCurrentList()
    {
        return _configService.Load().BusinessSoftware
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }
}
