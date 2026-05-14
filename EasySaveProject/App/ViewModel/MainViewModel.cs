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
            foreach (var i in indices)
                _backupService.RunJob(i);
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
    public BackupJob GetJob(int index)
    {
        return _backupService.GetJobs()[index];
    }

    public List<BackupJob> GetJobsRaw()
        => _backupService.GetJobs().ToList();
}