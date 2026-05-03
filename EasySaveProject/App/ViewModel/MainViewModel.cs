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
        _backupService.RunJobs(new List<int> { index + 1 });
    }
}