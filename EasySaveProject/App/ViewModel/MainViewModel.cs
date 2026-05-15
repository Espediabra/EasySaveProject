using EasySaveProject.Core.Services;
using EasySaveProject.Models;
using EasySaveProject.UI.Console.Components;

public class MainViewModel
{
    private readonly BackupService _backupService;

    public bool IsRunning { get; private set; }
    public event Action<bool>? OnExecutionStateChanged;

    public MainViewModel(BackupService backupService)
    {
        _backupService = backupService;
    }

    private void SetRunning(bool state)
    {
        IsRunning = state;
        OnExecutionStateChanged?.Invoke(state);
    }

    public List<string> GetBackupNames()
    {
        return _backupService.GetJobs().Select(j => j.Name).ToList();
    }

    public void ExecuteBackup(int index)
    {
        SetRunning(true);
        try
        {
            _backupService.RunJob(index);
        }
        finally
        {
            SetRunning(false);
        }
    }

    public void ExecuteMultipleBackups(List<int> indices)
    {
        SetRunning(true);
        try
        {
            // V3: jobs run in parallel; blocks the calling thread until all complete.
            _backupService.RunJobsParallelAsync(indices).GetAwaiter().GetResult();
        }
        finally
        {
            SetRunning(false);
        }
    }

    public void CreateJob(string name, string source, string target, BackupType type)
    {
        if (_backupService.GetJobs().Count >= 5)
            throw new InvalidOperationException("Max jobs reached");

        _backupService.AddJob(new BackupJob(name, source, target, type));
    }

    public void DeleteJob(int index) => _backupService.DeleteJob(index);

    public void ChangeJobType(int index, BackupType type)
        => _backupService.UpdateJobType(index, type);

    public BackupJob GetJob(int index) => _backupService.GetJobs()[index];

    public List<BackupJob> GetJobsRaw() => _backupService.GetJobs().ToList();

    // ── Pause / Stop controls (delegated to BackupService) ────────────────
    // GUI will bind per-job buttons and global buttons to these methods.

    public void PauseAll()                  => _backupService.PauseAll();
    public void ResumeAll()                 => _backupService.ResumeAll();
    public void StopAll()                   => _backupService.StopAll();
    public void TogglePauseJob(string name) => _backupService.TogglePauseJob(name);
    public void StopJob(string name)        => _backupService.StopJob(name);

    /// <summary>Returns the live controller for a running job, or null if not active.</summary>
    public JobController? GetActiveController(string name) => _backupService.GetControllerByName(name);
}