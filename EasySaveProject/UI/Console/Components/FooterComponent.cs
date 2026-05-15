using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasySaveProject.Core.Services;

namespace EasySaveProject.UI.Console.Components;

/// <summary>
/// Renders one progress bar per active backup job at the bottom of the terminal,
/// plus a navigation hint row above them.
///
/// Navigation (while jobs are running):
///   ↑ / ↓       — move cursor between bars (top = global mode, below = per-job)
///   SPACE        — pause/resume the selected job (or all jobs in global mode)
///   ESC          — stop the selected job (or all jobs in global mode)
/// </summary>
public class FooterComponent
{
    private readonly ProgressService              _progressService;
    private readonly PauseService                 _pauseService;
    private readonly Func<string, JobController?> _getController;
    private readonly Action                       _resumeAll;
    private readonly Action                       _stopAll;

    private CancellationTokenSource? _cts;
    private readonly object          _consoleLock  = new();
    private int                      _lastRowCount = 0;
    private int                      _selectedRow  = -1; // -1 = global/hint, 0..n-1 = job bar

    public FooterComponent(
        ProgressService progressService,
        PauseService pauseService,
        Func<string, JobController?> getController,
        Action resumeAll,
        Action stopAll)
    {
        _progressService = progressService;
        _pauseService    = pauseService;
        _getController   = getController;
        _resumeAll       = resumeAll;
        _stopAll         = stopAll;
    }

    public void Start()
    {
        if (_cts != null) return;

        _selectedRow = -1;
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

                for (int i = 0; i < _lastRowCount; i++)
                {
                    int row = Math.Max(0, height - 1 - i);
                    System.Console.SetCursorPosition(0, row);
                    System.Console.Write(new string(' ', width));
                }

                if (_lastRowCount > 0)
                    System.Console.SetCursorPosition(0, Math.Max(0, height - _lastRowCount));

                _lastRowCount = 0;
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
                int count  = Math.Min(snaps.Count, Math.Max(1, height - 3));

                // Clamp selection to valid range (-1 = global, 0..count-1 = bar)
                _selectedRow = Math.Max(-1, Math.Min(_selectedRow, count - 1));

                // Total rows = hint row + job bars
                int totalRows = count + 1;
                int rowsToClear = Math.Max(totalRows, _lastRowCount);

                for (int i = 0; i < rowsToClear; i++)
                {
                    int row = Math.Max(0, height - 1 - i);
                    System.Console.SetCursorPosition(0, row);
                    System.Console.Write(new string(' ', width));
                }

                // Hint row (above all bars): shows navigation instructions and current mode
                int hintRow = Math.Max(0, height - 1 - count);
                RenderHint(hintRow, width, count);

                // Job bars (bottom = index 0, going upward)
                for (int i = 0; i < count; i++)
                {
                    int row      = Math.Max(0, height - 1 - i);
                    bool selected = i == _selectedRow;
                    RenderBar(snaps[i], row, width, selected);
                }

                _lastRowCount = totalRows;
            }
            catch { }
        }
    }

    private void RenderHint(int row, int width, int count)
    {
        bool globalMode = _selectedRow < 0 || count == 0;
        string marker = globalMode ? "▶ " : "  ";
        string mode   = globalMode ? "ALL" : $"job {_selectedRow + 1}";
        string hint   = $"{marker}↑↓ Navigate  SPACE Pause/Resume  ESC Stop  [{mode}]";

        System.Console.SetCursorPosition(0, row);
        System.Console.Write(hint.PadRight(width)[..Math.Min(hint.Length > width ? width : hint.PadRight(width).Length, width)]);
    }

    private void RenderBar(ProgressSnapshot snap, int row, int width, bool selected)
    {
        if (string.IsNullOrEmpty(snap.BackupName)) return;

        var ctrl = _getController(snap.BackupName);
        bool jobPaused    = ctrl?.IsPaused == true;
        bool globalPaused = _pauseService.IsPaused;

        string pauseTag = (globalPaused || jobPaused) ? " [PAUSE]" : string.Empty;

        string etaStr = snap.Eta.HasValue
            ? $"{(int)snap.Eta.Value.TotalHours:D2}:{snap.Eta.Value.Minutes:D2}:{snap.Eta.Value.Seconds:D2}"
            : "--:--:--";

        string selector = selected ? "▶ " : "  ";
        string left  = $"{selector}[{snap.BackupName}] {snap.DoneFiles}/{snap.TotalFiles} | {snap.Fraction * 100:0.0}%  {HumanSize(snap.Transferred)}/{HumanSize(snap.Total)}";
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
        System.Console.Write(line[..Math.Min(line.Length, width)].PadRight(width));
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
                    var key   = System.Console.ReadKey(intercept: true);
                    var snaps = _progressService.CurrentAll;
                    int count = snaps.Count;

                    _selectedRow = Math.Max(-1, Math.Min(_selectedRow, count - 1));

                    switch (key.Key)
                    {
                        // Visual layout (top→bottom): hint[-1], bar[count-1], ..., bar[0]
                        // ↑ moves UP on screen → higher bar index or hint
                        case ConsoleKey.UpArrow:
                            if (_selectedRow == count - 1)
                                _selectedRow = -1;
                            else if (_selectedRow >= 0)
                                _selectedRow++;
                            // at -1 (hint), stay
                            break;

                        // ↓ moves DOWN on screen → lower bar index or from hint to top bar
                        case ConsoleKey.DownArrow:
                            if (_selectedRow == -1)
                                _selectedRow = count > 0 ? count - 1 : -1;
                            else if (_selectedRow > 0)
                                _selectedRow--;
                            // at 0 (bottom), stay
                            break;

                        case ConsoleKey.Spacebar:
                            if (_selectedRow < 0 || _selectedRow >= count)
                            {
                                // Smart global toggle: if anything is paused → resume all; else pause all
                                bool anyPaused = _pauseService.IsPaused
                                    || snaps.Any(s => _getController(s.BackupName)?.IsPaused == true);
                                if (anyPaused) _resumeAll();
                                else _pauseService.Pause();
                            }
                            else
                            {
                                _getController(snaps[_selectedRow].BackupName)?.Toggle();
                            }
                            break;

                        case ConsoleKey.Escape:
                            if (_selectedRow < 0 || _selectedRow >= count)
                            {
                                _stopAll();
                            }
                            else
                            {
                                _getController(snaps[_selectedRow].BackupName)?.Stop();
                            }
                            break;
                    }
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
        return path[..left] + "..." + file;
    }
}
