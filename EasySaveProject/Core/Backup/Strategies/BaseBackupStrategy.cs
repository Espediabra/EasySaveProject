using EasySaveProject.Core;
using EasySaveProject.Models;
using EasySaveProject.Core.Services;

namespace EasySaveProject.Strategies;

public abstract class BaseBackupStrategy : IBackupStrategy
{
    public void Execute(
        BackupJob     job,
        FileService   fileService,
        LogService    logService,
        StateService  stateService,
        PauseService  pauseService)   // ← injecté
    {
        if (!Directory.Exists(job.SourcePath))
            throw new DirectoryNotFoundException($"Source not found: {job.SourcePath}");

        if (!Directory.Exists(job.TargetPath))
            Directory.CreateDirectory(job.TargetPath);

        var files     = SelectFiles(job);
        long totalSize = files.Sum(f => new FileInfo(f).Length);

        var state = new State
        {
            BackupName     = job.Name,
            Status         = "Active",
            TotalFiles     = files.Length,
            RemainingFiles = files.Length,
            TotalSize      = totalSize,
            RemainingSize  = totalSize,
        };

        stateService.Update(state);

        foreach (var sourceFile in files)
        {
            // ── Point de pause entre chaque fichier ───────────────────────
            // Si pause demandée, on bloque ici jusqu'à la reprise.
            // Le fichier en cours n'est jamais interrompu à mi-copie.
            pauseService.WaitIfPaused();

            var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
            var targetFile   = Path.Combine(job.TargetPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            var fileSize  = new FileInfo(sourceFile).Length;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            state.Timestamp         = DateTime.Now;
            state.CurrentSourceFile = sourceFile;
            state.CurrentTargetFile = targetFile;
            stateService.Update(state);

            try
            {
                fileService.CopyFileWithProgress(sourceFile, targetFile, bytesJustCopied =>
                {
                    state.RemainingSize -= bytesJustCopied;
                    state.Timestamp      = DateTime.Now;
                    stateService.Update(state);
                });

                stopwatch.Stop();
                state.RemainingFiles--;
                state.RemainingSize = Math.Max(0, state.RemainingSize);
                stateService.Update(state);

                logService.LogInfo(
                    job.Name, sourceFile, targetFile,
                    fileSize, stopwatch.ElapsedMilliseconds,
                    "File copied successfully");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                state.RemainingFiles--;
                state.RemainingSize = Math.Max(0, state.RemainingSize - fileSize);
                stateService.Update(state);

                logService.LogError(
                    job.Name, sourceFile, targetFile,
                    0, $"Error during file copy: {ex.Message}");
            }
        }

        pauseService.Reset();
        state.Status = "Completed";
        stateService.Update(state);
        BackupStateHub.Clear();
    }

    protected abstract string[] SelectFiles(BackupJob job);
}