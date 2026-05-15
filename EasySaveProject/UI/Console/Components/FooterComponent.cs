using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasySaveProject.Core.Services;

namespace EasySaveProject.UI.Console.Components;

/// <summary>
/// Renders one progress bar per active backup job at the bottom of the terminal.
/// Bars are stacked upward: the first job is at the last row, the second one row above, etc.
/// Delegates all progress computation to ProgressService.
/// Listens for Spacebar to toggle global pause via PauseService.
/// </summary>
public class FooterComponent
{
    private readonly ProgressService _progressService;
    private readonly PauseService    _pauseService;

    private CancellationTokenSource? _cts;
    private readonly object          _consoleLock    = new();
    private int                      _lastBarCount   = 0;

    public FooterComponent(ProgressService progressService, PauseService pauseService)
    {
        _progressService = progressService;
        _pauseService    = pauseService;
    }

    public void Start()
    {
        if (_cts != null) return;

        _cts = new CancellationTokenSource();
        Task.Run(() => RenderLoop(_cts.Token));
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
                int width  = System.Console.WindowWidth;
                int height = System.Console.WindowHeight;
                int rows   = Math.Max(_lastBarCount, 1);

                for (int i = 0; i < rows; i++)
                {
                    int row = Math.Max(0, height - 1 - i);
                    System.Console.SetCursorPosition(0, row);
                    System.Console.Write(new string(' ', width));
                }
                System.Console.SetCursorPosition(0, Math.Max(0, height - rows));
                _lastBarCount = 0;
            }
            catch { }
        }
    }

    // ── Render loop ───────────────────────────────────────────────────────

    private async Task RenderLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _progressService.Tick();
                RenderAll(_progressService.CurrentAll);
            }
            catch { }

            try { await Task.Delay(200, ct); }
            catch (TaskCanceledException) { break; }
        }
    }

    private void RenderAll(IReadOnlyList<ProgressSnapshot> snaps)
    {
        lock (_consoleLock)
        {
            try
            {
                int height = System.Console.WindowHeight;
                int width  = Math.Max(10, System.Console.WindowWidth);
                int count  = Math.Min(snaps.Count, Math.Max(1, height - 2));

                // Clear as many rows as we rendered last tick (handles job completions)
                int rowsToClear = Math.Max(count, _lastBarCount);
                for (int i = 0; i < rowsToClear; i++)
                {
                    int row = Math.Max(0, height - 1 - i);
                    System.Console.SetCursorPosition(0, row);
                    System.Console.Write(new string(' ', width));
                }

                // Render each active job bar (bottom = index 0, going upward)
                for (int i = 0; i < count; i++)
                {
                    int row = Math.Max(0, height - 1 - i);
                    RenderBar(snaps[i], row, width);
                }

                _lastBarCount = count;
            }
            catch { }
        }
    }

    private void RenderBar(ProgressSnapshot snap, int row, int width)
    {
        if (string.IsNullOrEmpty(snap.BackupName)) return;

        string etaStr    = snap.Eta.HasValue
            ? $"{(int)snap.Eta.Value.TotalHours:D2}:{snap.Eta.Value.Minutes:D2}:{snap.Eta.Value.Seconds:D2}"
            : "--:--:--";
        string pauseTag  = _pauseService.IsPaused ? " [PAUSE]" : string.Empty;

        string left  = $"[{snap.BackupName}] {snap.DoneFiles}/{snap.TotalFiles} | {snap.Fraction * 100:0.0}% | {HumanSize(snap.Transferred)}/{HumanSize(snap.Total)}";
        string right = $"ETA: {etaStr}{pauseTag} | {TruncatePath(snap.CurrentFile, 25)}";

        int barWidth = Math.Max(5, width - left.Length - right.Length - 4);
        int filled   = (int)Math.Round(barWidth * snap.Fraction);

        string bar = "["
            + new string('=', filled)
            + (filled < barWidth ? ">" : "=")
            + new string(' ', Math.Max(0, barWidth - filled - 1))
            + "]";

        string line = $"{left} {bar} {right}";

        System.Console.SetCursorPosition(0, row);
        System.Console.Write(line.Substring(0, Math.Min(line.Length, width)).PadRight(width));
    }

    // ── Keyboard loop ─────────────────────────────────────────────────────

    private async Task KeyboardLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (System.Console.KeyAvailable)
                {
                    var key = System.Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Spacebar)
                        _pauseService.Toggle();
                }
            }
            catch { }

            try { await Task.Delay(50, ct); }
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
