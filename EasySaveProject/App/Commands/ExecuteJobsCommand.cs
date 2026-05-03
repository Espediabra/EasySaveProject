using EasySaveProject.Services;

public class ExecuteJobsCommand
{
    private readonly BackupService _backupService;

    public ExecuteJobsCommand(BackupService backupService)
    {
        _backupService = backupService;
    }

    public void Execute(List<int> ids)
    {
        _backupService.RunJobs(ids);
    }
}