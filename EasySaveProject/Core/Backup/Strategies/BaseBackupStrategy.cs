using EasySaveProject.Models;
using EasySaveProject.Core.Services;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;


namespace EasySaveProject.Core.Strategies;

public abstract class BaseBackupStrategy : IBackupStrategy
{
    public void Execute(
        BackupJob job,
        FileService fileService,
        LogService logService,
        StateService stateService,
        CryptoService cryptoService,
        BusinessSoftwareWatcher watcher,
        PauseService pauseService)
    {
        // Vérification AVANT démarrage : refus immédiat si logiciel métier actif
        if (watcher.IsRunning())
        {
            var detected = watcher.GetRunningName() ?? "unknown";
            Console.WriteLine($"[EasySave] Backup refused: business software detected ({detected})");

            logService.LogWarning(
                job.Name, job.SourcePath, job.TargetPath, 0, 0,
                $"Backup refused: business software detected ({detected})"
            );
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

        stateService.Update(state);

        foreach (var sourceFile in files)
        {
            pauseService.WaitIfPaused();

            if (watcher.IsRunning())
            {
                var detected = watcher.GetRunningName() ?? "unknown";
                Console.WriteLine($"[EasySave] Backup paused: business software detected ({detected})");

                logService.LogWarning(
                    job.Name, sourceFile, string.Empty, 0, 0,
                    $"Backup paused: business software detected ({detected})"
                );

                state.Status = "Paused";
                stateService.Update(state);

                while (watcher.IsRunning())
                    Thread.Sleep(500);

                Console.WriteLine("[EasySave] Business software closed, resuming backup...");

                logService.LogInfo(
                    job.Name, string.Empty, string.Empty, 0, 0,
                    "Business software closed, backup resumed"
                );

                state.Status = "Active";
                stateService.Update(state);
            }

            var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
            var targetFile = Path.Combine(job.TargetPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            var fileSize = new FileInfo(sourceFile).Length;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            state.Timestamp = DateTime.Now;
            state.CurrentSourceFile = sourceFile;
            state.CurrentTargetFile = targetFile;
            stateService.Update(state);

            try
            {
                fileService.CopyFileWithProgress(sourceFile, targetFile, bytesJustCopied =>
                {
                    state.RemainingSize -= bytesJustCopied;
                    state.Timestamp = DateTime.Now;
                    stateService.Update(state);
                });

                stopwatch.Stop();

                int cryptoTimeMs = cryptoService.TryEncrypt(targetFile);

                state.RemainingFiles--;
                state.RemainingSize = Math.Max(0, state.RemainingSize);
                stateService.Update(state);

                if (cryptoTimeMs < 0)
                {
                    logService.LogError(
                        job.Name, sourceFile, targetFile,
                        fileSize,
                        $"Encryption error (code {cryptoTimeMs})"
                    );
                }
                else
                {
                    string message = cryptoTimeMs > 0
                        ? $"File copied and encrypted in {cryptoTimeMs} ms"
                        : "File copied successfully";

                    logService.LogInfo(
                        job.Name, sourceFile, targetFile,
                        fileSize,
                        stopwatch.ElapsedMilliseconds,
                        message
                    );
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                state.RemainingFiles--;
                state.RemainingSize = Math.Max(0, state.RemainingSize - fileSize);
                stateService.Update(state);

                logService.LogError(
                    job.Name, sourceFile, targetFile,
                    0,
                    $"Error during file copy: {ex.Message}"
                );
            }
        }

        pauseService.Reset();

        state.Status = "Completed";
        stateService.Update(state);

        BackupStateHub.Clear();
    }

    protected abstract string[] SelectFiles(BackupJob job);
}
