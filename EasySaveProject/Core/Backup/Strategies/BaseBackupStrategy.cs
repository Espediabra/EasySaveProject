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
        JobController jobController,
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
        int priorityRemaining = priorityCount;

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

        bool stopped = false;

        foreach (var sourceFile in sorted)
        {
            // ── 4) Stop check before each file ─────────────────────────────
            if (pauseService.IsStopRequested || jobController.IsStopRequested)
            {
                stopped = true;
                break;
            }

            bool isPriority = priorityCoordinator.IsPriorityFile(sourceFile);
            bool largeAcquired = false;

            try
            {
                // ── 5) Synchronization gates (order matters) ────────────────
                watcher.WaitUntilFree(pauseService, logService, job, state, stateService);
                pauseService.WaitIfPaused();
                jobController.WaitIfPaused();

                // Re-check stop after waking from any pause
                if (pauseService.IsStopRequested || jobController.IsStopRequested)
                {
                    stopped = true;
                    break;
                }

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

                        // Mid-copy business software check: blocks the copy thread at each
                        // 1 MB chunk boundary until the offending process closes.
                        if (watcher.IsRunning())
                            watcher.WaitUntilFree(pauseService, logService, job, state, stateService);

                        pauseService.WaitIfPaused();
                        jobController.WaitIfPaused();

                        if (pauseService.IsStopRequested || jobController.IsStopRequested)
                            throw new OperationCanceledException();
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
                catch (OperationCanceledException)
                {
                    stopwatch.Stop();
                    stopped = true;
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
                if (isPriority)
                {
                    priorityCoordinator.OnPriorityFileCompleted();
                    priorityRemaining--;
                }
            }

            if (stopped) break;
        }

        // ── 6) Drain remaining priority files if stopped early ──────────────
        // Prevents other parallel jobs from blocking forever on WaitIfNonPriority.
        if (stopped && priorityRemaining > 0)
        {
            for (int i = 0; i < priorityRemaining; i++)
                priorityCoordinator.OnPriorityFileCompleted();
        }

        state.Status = stopped ? "Stopped" : "Completed";
        stateService.Update(state);

        // pauseService.Reset() and BackupStateHub.Clear() are now the
        // responsibility of BackupService, which controls job lifecycle.
    }

    protected abstract string[] SelectFiles(BackupJob job);
}
