using EasySaveProject.Services;

public class ExecuteJobsCommand
{
    private readonly BackupService _backupService;

    public ExecuteJobsCommand(BackupService backupService)
    {
        _backupService = backupService;
    }

    public void Execute(int index)
    {
        _backupService.RunJob(index);
    }
}
