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
        PauseService pauseService,
        PriorityCoordinator priorityCoordinator,
        LargeFileTransferGuard largeFileGuard)
    {
        // ── 1) Pre-start check ──────────────────────────────────────────────
        if (watcher.IsRunning())
        {
            string blockers = string.Join(", ", watcher.GetRunningProcesses());

            Console.WriteLine();
            Console.WriteLine($"[!] Backup '{job.Name}' cancelled: business software running ({blockers})");
            Console.WriteLine();

            logService.LogWarning(
                job.Name, job.SourcePath, job.TargetPath, 0, 0,
                $"Backup cancelled: business software already running ({blockers})"
            );

            stateService.Update(new State
            {
                BackupName = job.Name,
                Timestamp = DateTime.Now,
                Status = "Cancelled",
                TotalFiles = 0,
                RemainingFiles = 0,
                TotalSize = 0,
                RemainingSize = 0,
                CurrentSourceFile = string.Empty,
                CurrentTargetFile = string.Empty
            });

            return;
        }

        if (!Directory.Exists(job.SourcePath))
            throw new DirectoryNotFoundException($"Source not found: {job.SourcePath}");

        if (!Directory.Exists(job.TargetPath))
            Directory.CreateDirectory(job.TargetPath);

        var files = SelectFiles(job);
        long totalSize = files.Sum(f => new FileInfo(f).Length);

        // ── 2) Sort: priority files first within this job ───────────────────
        // Required to avoid self-deadlock: if a job's own non-priority file
        // blocks on WaitIfNonPriority, it would never reach its priority files
        // and the global count would never reach zero.
        var sorted = files
            .OrderBy(f => priorityCoordinator.GetPriorityRank(f))
            .ToArray();

        // ── 3) Register this job's pending priority files globally ──────────
        int priorityCount = sorted.Count(f => priorityCoordinator.IsPriorityFile(f));
        priorityCoordinator.RegisterPendingPriorityFiles(priorityCount);

        var state = new State
        {
            BackupName = job.Name,
            Status = "Active",
            TotalFiles = sorted.Length,
            RemainingFiles = sorted.Length,
            TotalSize = totalSize,
            RemainingSize = totalSize
        };

        stateService.Update(state);

        foreach (var sourceFile in sorted)
        {
            bool isPriority = priorityCoordinator.IsPriorityFile(sourceFile);
            bool largeAcquired = false;

            try
            {
                // ── 4) Synchronization gates (order matters) ────────────────
                watcher.WaitUntilFree(pauseService, logService, job, state, stateService);
                pauseService.WaitIfPaused();

                // Block non-priority file if any priority file is still pending globally.
                priorityCoordinator.WaitIfNonPriority(sourceFile);

                var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
                var targetFile = Path.Combine(job.TargetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

                var fileSize = new FileInfo(sourceFile).Length;

                // Serialize large files: at most one large file copied at a time.
                largeAcquired = largeFileGuard.AcquireIfLarge(fileSize);

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
            finally
            {
                // Always release regardless of success or error, to prevent deadlocks.
                largeFileGuard.Release(largeAcquired);
                if (isPriority) priorityCoordinator.OnPriorityFileCompleted();
            }
        }

        state.Status = "Completed";
        stateService.Update(state);

        // pauseService.Reset() and BackupStateHub.Clear() are now the
        // responsibility of BackupService, which controls job lifecycle.
    }

    protected abstract string[] SelectFiles(BackupJob job);
}
