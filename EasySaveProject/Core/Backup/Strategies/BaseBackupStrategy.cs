using EasySaveProject.Models;
using EasySaveProject.Services;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;

namespace EasySaveProject.Strategies;

public abstract class BaseBackupStrategy : IBackupStrategy
{
    public void Execute(
        BackupJob job,
        FileService fileService,
        LogService logService,
        StateService stateService,
        CryptoService cryptoService,
        BusinessSoftwareWatcher watcher)
    {
        // Vérification avant démarrage
        if (watcher.IsRunning())
        {
            logService.LogWarning(job.Name, job.SourcePath, job.TargetPath, 0, 0,
                "Backup cancelled: business software detected");
            return;
        }

        if (!Directory.Exists(job.SourcePath))
            throw new DirectoryNotFoundException($"Source not found: {job.SourcePath}");

        if (!Directory.Exists(job.TargetPath))
            Directory.CreateDirectory(job.TargetPath);

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
            // Vérification entre chaque fichier 
            if (watcher.IsRunning())
            {
                logService.LogWarning(job.Name, sourceFile, string.Empty, 0, 0,
                    "Backup interrupted: business software detected");

                state.Status = "Interrupted";
                stateService.Update(state);
                return;
            }

            var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
            var targetFile = Path.Combine(job.TargetPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                fileService.CopyFile(sourceFile, targetFile);
                stopwatch.Stop();

                var fileSize = new FileInfo(sourceFile).Length;

                // Chiffrement après la copie 
                int cryptoTimeMs = cryptoService.TryEncrypt(targetFile);

                state.Timestamp = DateTime.Now;
                state.CurrentSourceFile = sourceFile;
                state.CurrentTargetFile = targetFile;
                state.RemainingFiles--;
                state.RemainingSize -= fileSize;

                stateService.Update(state);

                if (cryptoTimeMs < 0)
                {
                    logService.LogError(job.Name, sourceFile, targetFile, fileSize,
                        $"Encryption error (code {cryptoTimeMs})");
                }
                else
                {
                    string message = cryptoTimeMs > 0
                        ? $"File copied and encrypted in {cryptoTimeMs} ms"
                        : "File copied successfully";

                    logService.LogInfo(job.Name, sourceFile, targetFile,
                        fileSize, stopwatch.ElapsedMilliseconds, message);
                }
            }
            catch
            {
                stopwatch.Stop();
                logService.LogError(job.Name, sourceFile, targetFile, 0,
                    "Error during file copy");
            }
        }

        state.Status = "Completed";
        stateService.Update(state);
    }

    protected abstract string[] SelectFiles(BackupJob job);
}
