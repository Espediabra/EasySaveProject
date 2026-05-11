using EasySaveProject.Core.Services;
using EasySaveProject.Models;
using EasySaveProject.UI.Console.Components;

public class MainViewModel
{
    private readonly BackupService    _backupService;
    private readonly FooterComponent  _footer;

    public MainViewModel(
        BackupService   backupService,
        FooterComponent footer)
    {
        _backupService = backupService;
        _footer        = footer;
    }

    public List<string> GetBackupNames()
    {
        return _backupService.GetJobs()
                             .Select(j => j.Name)
                             .ToList();
    }

    public void ExecuteBackup(int index)
    {
        _footer.Start();
        try
        {
            _backupService.RunJob(index);
        }
        finally
        {
            _footer.Stop();
        }
    }

    public void CreateJob(string name, string source, string target, BackupType type)
    {
        var job = new BackupJob(name, source, target, type);
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

    public void ExecuteMultipleBackups(List<int> indices)
    {
        _footer.Start();
        try
        {
            foreach (int index in indices)
                _backupService.RunJob(index);
        }
        finally
        {
            _footer.Stop();
        }
    }
}