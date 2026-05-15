using EasySaveProject.Models;
using EasySaveProject.Services;

namespace EasySaveProject.Strategies;

public class DifferentialBackupStrategy : BaseBackupStrategy
{
    protected override string[] SelectFiles(BackupJob job)
    {
        var allFiles = Directory.GetFiles(job.SourcePath, "*", SearchOption.AllDirectories);

        return allFiles.Where(sourceFile =>
        {
            var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
            var targetFile = Path.Combine(job.TargetPath, relativePath);

            return !File.Exists(targetFile) ||
                   File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(targetFile);
        }).ToArray();
    }
}