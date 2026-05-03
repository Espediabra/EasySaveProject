using EasySaveProject.Services;
using EasySaveProject.Models;

public class MainViewModel
{
    private readonly BackupService _backupService;

    public MainViewModel(BackupService backupService)
    {
        _backupService = backupService;
    }

    public List<string> GetBackupNames()
    {
        return _backupService.GetJobs()
                              .Select(j => j.Name)
                              .ToList();
    }

    public void ExecuteBackup(int index)
    {
        _backupService.RunJob(index);
    }

    public void CreateJob(string name, string source, string target, BackupType type)
    {
        var job = new BackupJob(
            name,
            source,
            target,
            type
        );

        _backupService.AddJob(job);
    }
    public void DeleteJob(int index)
    {
        _backupService.DeleteJob(index);
    }

    public void ChangeJobType(int index, BackupType type)
    {
        _backupService.UpdateJobType(index, type);
    }

    public BackupJob GetJob(int index)
    {
        return _backupService.GetJobs()[index];
    }

    public List<BackupJob> GetJobsRaw()
    {
        return _backupService.GetJobs().ToList();
    }
}