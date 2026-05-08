using EasySaveProject.Models;
using EasySaveProject.Core.Services;

namespace EasySaveProject.Strategies;

public abstract class BaseBackupStrategy : IBackupStrategy
{
    public void Execute(
        BackupJob job,
        FileService fileService,
        LogService logService,
        StateService stateService)
    {
        if (!Directory.Exists(job.SourcePath))
            throw new DirectoryNotFoundException($"Source not found: {job.SourcePath}");
            
        if (!Directory.Exists(job.TargetPath))
        {
            Directory.CreateDirectory(job.TargetPath); 
        }

        var files = SelectFiles(job);

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

                logService.LogInfo(
                    job.Name,
                    sourceFile,
                    targetFile,
                    fileSize,
                    stopwatch.ElapsedMilliseconds,
                    "File copied successfully"
                );
            }
            catch
            {
                stopwatch.Stop();

                logService.LogError(
                    job.Name,
                    sourceFile,
                    targetFile,
                    0,
                    "Error during file copy"
                );
            }
        }

        state.Status = "Completed";
        stateService.Update(state);
    }
    protected abstract string[] SelectFiles(BackupJob job);
}