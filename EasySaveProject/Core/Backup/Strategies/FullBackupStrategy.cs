using EasySaveProject.Models;
using EasySaveProject.Core.Services;
using EasySaveProject.Strategies;

namespace EasySaveProject.Strategies;

/// <summary>
/// Full backup strategy: copies all files from source to target.
/// </summary>
public class FullBackupStrategy : BaseBackupStrategy
{
    protected override string[] SelectFiles(BackupJob job)
    {
        return Directory.GetFiles(job.SourcePath, "*", SearchOption.AllDirectories);
    }
}
