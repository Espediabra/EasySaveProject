<<<<<<< HEAD
using EasySave.Models;
// using EasySave.Strategies;
=======
using EasySaveProject.Models;
using EasySaveProject.Services;

namespace EasySaveProject.Strategies;
>>>>>>> 98d876b13b84b8195240683710c4b056e9a6392b

public class DifferentialBackupStrategy : IBackupStrategy
{

    public void Execute(BackupJob job, FileService fileService, LogService logService, StateService stateService)
    {
        if (!Directory.Exists(job.SourcePath))
            throw new DirectoryNotFoundException($"Source not found: {job.SourcePath}");

        var allFiles = Directory.GetFiles(job.SourcePath, "*", SearchOption.AllDirectories);

        var files = allFiles.Where(sourceFile =>
        {
            var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
            var targetFile = Path.Combine(job.TargetPath, relativePath);

            return !File.Exists(targetFile) ||
                   File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(targetFile);
        }).ToArray();

        long totalSize = files.Sum(f => new FileInfo(f).Length);

        var state = new State
        {
            BackupName = job.Name,
            Status = "Active",
            TotalFiles = files.Length,
            RemainingFiles = files.Length,
            TotalSize = totalSize,
            RemainingSize = totalSize
        };

        foreach (var sourceFile in files)
        {
            var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
            var targetFile = Path.Combine(job.TargetPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                fileService.CopyFile(sourceFile, targetFile);

                stopwatch.Stop();

                var fileSize = new FileInfo(sourceFile).Length;

                state.Timestamp = DateTime.Now;
                state.CurrentSourceFile = sourceFile;
                state.CurrentTargetFile = targetFile;
                state.RemainingFiles--;
                state.RemainingSize -= fileSize;

                stateService.Update(state);

                logService.CreateLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePath = sourceFile,
                    TargetPath = targetFile,
                    FileSize = fileSize,
                    TransferTime = stopwatch.ElapsedMilliseconds
                });
            }
            catch
            {
                stopwatch.Stop();

                logService.CreateLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePath = sourceFile,
                    TargetPath = targetFile,
                    FileSize = 0,
                    TransferTime = -1
                });
            }
        }

        state.Status = "Completed";
        stateService.Update(state);
    }
}