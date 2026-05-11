using System;
using System.Threading;
using System.Threading.Tasks;
using EasySaveProject.Core.Services;

namespace EasySaveProject.UI.Console.Components;

/// <summary>
/// Affiche la barre de progression en bas du terminal.
/// Ne contient aucune logique de calcul — délègue à ProgressService.
/// Écoute aussi la touche Espace pour pause/reprise via PauseService.
/// </summary>
public class FooterComponent
{
    private readonly ProgressService _progressService;
    private readonly PauseService    _pauseService;

    private CancellationTokenSource? _cts;
    private readonly object          _consoleLock = new();

    public FooterComponent(ProgressService progressService, PauseService pauseService)
    {
        _progressService = progressService;
        _pauseService    = pauseService;
    }

    public void Start()
    {
        if (_cts != null) return;

        _cts = new CancellationTokenSource();

        // Thread d'affichage : rafraîchit le footer toutes les 200ms
        Task.Run(() => RenderLoop(_cts.Token));

        // Thread d'écoute clavier : capte Espace pour pause/reprise
        Task.Run(() => KeyboardLoop(_cts.Token));
    }

    public void Stop()
    {
        if (_cts == null) return;

        _cts.Cancel();
        _cts = null;

        lock (_consoleLock)
        {
            try
            {
                int row = Math.Max(0, System.Console.WindowHeight - 1);
                System.Console.SetCursorPosition(0, row);
                System.Console.Write(new string(' ', System.Console.WindowWidth));
                System.Console.SetCursorPosition(0, row);
            }
            catch { }
        }
    }

    // ── Boucle d'affichage ────────────────────────────────────────────────

    private async Task RenderLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _progressService.Tick();
                Render(_progressService.Current);
            }
            catch { }

            try { await Task.Delay(200, ct); }
            catch (TaskCanceledException) { break; }
        }
    }

    private void Render(ProgressSnapshot snap)
    {
        if (string.IsNullOrEmpty(snap.BackupName)) return;

        lock (_consoleLock)
        {
            try
            {
                int width    = Math.Max(10, System.Console.WindowWidth);
                int row      = Math.Max(0, System.Console.WindowHeight - 1);
                int barWidth = Math.Max(10, width - 60);
                int filled   = (int)Math.Round(barWidth * snap.Fraction);

                string bar = "["
                    + new string('=', filled)
                    + (filled < barWidth ? ">" : "=")
                    + new string(' ', Math.Max(0, barWidth - filled - 1))
                    + "]";

                string etaStr = snap.Eta.HasValue
                    ? $"{(int)snap.Eta.Value.TotalHours:D2}:{snap.Eta.Value.Minutes:D2}:{snap.Eta.Value.Seconds:D2}"
                    : "--:--:--";

                // Indicateur pause visible dans le footer
                string pauseTag = _pauseService.IsPaused ? " [PAUSE]" : string.Empty;

                string left  = $"Files: {snap.DoneFiles}/{snap.TotalFiles} | {snap.Fraction * 100:0.0}% | {HumanSize(snap.Transferred)}/{HumanSize(snap.Total)}";
                string right = $"ETA: {etaStr}{pauseTag} | {TruncatePath(snap.CurrentFile, 30)}";
                string line  = left.PadRight(2) + " "
                    + bar.PadRight(barWidth + 2) + " "
                    + right.PadLeft(Math.Max(0, width - (left.Length + bar.Length + 4)));

                System.Console.SetCursorPosition(0, row);
                System.Console.Write(line.Substring(0, Math.Min(line.Length, width)).PadRight(width));
            }
            catch { }
        }
    }

    // ── Boucle clavier ────────────────────────────────────────────────────

    private async Task KeyboardLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // KeyAvailable évite de bloquer sur ReadKey quand il n'y a rien
                if (System.Console.KeyAvailable)
                {
                    var key = System.Console.ReadKey(intercept: true); // intercept: ne pas afficher la touche
                    if (key.Key == ConsoleKey.Spacebar)
                        _pauseService.Toggle();
                }
            }
            catch { }

            try { await Task.Delay(50, ct); } // poll clavier plus réactif que le render
            catch (TaskCanceledException) { break; }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string HumanSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string TruncatePath(string path, int maxLen)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        if (path.Length <= maxLen) return path;
        var file = System.IO.Path.GetFileName(path);
        if (file.Length + 4 >= maxLen)
            return "..." + file[^Math.Min(file.Length, maxLen - 3)..];
        int left = maxLen - file.Length - 3;
        return path.Substring(0, left) + "..." + file;
    }
}