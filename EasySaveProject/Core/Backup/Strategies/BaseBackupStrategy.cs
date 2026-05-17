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
        // ── 1) Pre-start check: wait if business software is running ────────
        // Changed from "cancel immediately" to "wait until free" so that
        // parallel jobs launched while software is open will resume automatically.
        if (watcher.IsRunning())
        {
            var waitState = new State
            {
                BackupName        = job.Name,
                Timestamp         = DateTime.Now,
                Status            = "Paused",
                TotalFiles        = 0,
                RemainingFiles    = 0,
                TotalSize         = 0,
                RemainingSize     = 0,
                CurrentSourceFile = string.Empty,
                CurrentTargetFile = string.Empty
            };
            stateService.Update(waitState);
            watcher.WaitUntilFree(pauseService, logService, job, waitState, stateService);
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
            // .ThenBy(f => new FileInfo(f).Length)
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

        // Linked token that fires when either global stop or per-job stop is requested.
        // This lets AcquireIfLarge(ct) wake immediately instead of waiting indefinitely.
        using var linkedStopCts = CancellationTokenSource.CreateLinkedTokenSource(
            pauseService.StopToken, jobController.StopToken);

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

                // Serialize large files: cancellable so Stop() unblocks immediately.
                // OCE here means a stop was requested while waiting — handle cleanly.
                try { largeAcquired = largeFileGuard.AcquireIfLarge(fileSize, linkedStopCts.Token); }
                catch (OperationCanceledException) { stopped = true; }
                if (stopped) continue; // finally block still runs → priority/semaphore cleanup

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

                    if (pauseService.IsStopRequested || jobController.IsStopRequested)
                        throw new OperationCanceledException();

                    stopwatch.Stop();

                    int cryptoTimeMs = cryptoService.TryEncrypt(targetFile);

                    state.RemainingFiles--;
                    state.RemainingSize = Math.Max(0, state.RemainingSize);
                    stateService.Update(state);

                    string shortName = ShortFileName(sourceFile);

                    if (cryptoTimeMs < 0)
                    {
                        logService.LogError(
                            job.Name, sourceFile, targetFile,
                            fileSize,
                            $"File {shortName} encryption error (code {cryptoTimeMs})"
                        );
                    }
                    else
                    {
                        string message = cryptoTimeMs > 0
                            ? $"File {shortName} copied and encrypted in {cryptoTimeMs} ms"
                            : $"File {shortName} copied successfully";

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

    private static string ShortFileName(string path, int maxLen = 36)
    {
        var name = Path.GetFileName(path);
        if (name.Length <= maxLen) return name;
        int half = (maxLen - 3) / 2;
        return string.Concat(name.AsSpan(0, half), "...", name.AsSpan(name.Length - half));
    }
}
