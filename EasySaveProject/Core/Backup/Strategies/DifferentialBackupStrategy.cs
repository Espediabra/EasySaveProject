using EasySaveProject.Models;
using EasySaveProject.Core.Services;


namespace EasySaveProject.Core.Strategies;

public class DifferentialBackupStrategy : BaseBackupStrategy
{
    protected override string[] SelectFiles(BackupJob job)
    {
        var allFiles = Directory.GetFiles(job.SourcePath, "*", SearchOption.AllDirectories);

        return allFiles.Where(sourceFile =>
<<<<<<< HEAD
        {
            var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
            var targetFile = Path.Combine(job.TargetPath, relativePath);

            return !File.Exists(targetFile) ||
                   File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(targetFile);
        }).ToArray();
=======
            {
                var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
                var targetFile = Path.Combine(job.TargetPath, relativePath);

                return !File.Exists(targetFile) ||
                       File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(targetFile);
            }).ToArray();
>>>>>>> f67e00b0db0d5bd98353b478cb7c9bebfa56a41f
    }
}