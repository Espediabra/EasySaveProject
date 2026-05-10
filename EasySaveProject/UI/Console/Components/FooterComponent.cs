using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasySaveProject.Core;
using EasySaveProject.Models;

public static class FooterComponent
{
    private static CancellationTokenSource? _cts;
    private static readonly object _consoleLock = new();

    private static readonly Queue<(DateTime time, long bytes)> _speedWindow = new();
    private const double WindowSeconds = 4.0;

    public static void Start()
    {
        if (_cts != null)
            return;

        _speedWindow.Clear();
        _cts = new CancellationTokenSource();
        Task.Run(() => PollLoop(_cts.Token));
    }

    public static void Stop()
    {
        if (_cts == null)
            return;

        _cts.Cancel();
        _cts = null;
        _speedWindow.Clear();

        lock (_consoleLock)
        {
            try
            {
                int row = Math.Max(0, Console.WindowHeight - 1);
                Console.SetCursorPosition(0, row);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, row);
            }
            catch { }
        }
    }

    private static async Task PollLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Lecture depuis la mémoire — aucun I/O, aucun conflit possible
                var state = BackupStateHub.Read();
                if (state != null)
                    Render(state);
            }
            catch { }

            try { await Task.Delay(200, ct); }
            catch (TaskCanceledException) { break; }
        }
    }

    private static void Render(State state)
    {
        lock (_consoleLock)
        {
            try
            {
                int width = Math.Max(10, Console.WindowWidth);
                int row   = Math.Max(0, Console.WindowHeight - 1);

                long total       = Math.Max(1, state.TotalSize);
                long remaining   = Math.Max(0, state.RemainingSize);
                long transferred = total - remaining;

                double percent = Math.Min(100.0,
                    Math.Max(0.0, transferred / (double)total * 100.0));

                // Vitesse sur fenêtre glissante
                DateTime now = DateTime.UtcNow;
                _speedWindow.Enqueue((now, transferred));

                while (_speedWindow.Count > 1 &&
                       (now - _speedWindow.Peek().time).TotalSeconds > WindowSeconds)
                    _speedWindow.Dequeue();

                double speedBytesPerSec = 0.0;

                if (_speedWindow.Count >= 2)
                {
                    var oldest     = _speedWindow.Peek();
                    var dt         = (now - oldest.time).TotalSeconds;
                    var deltaBytes = transferred - oldest.bytes;

                    if (dt > 0 && deltaBytes > 0)
                        speedBytesPerSec = deltaBytes / dt;
                }

                string eta = (speedBytesPerSec > 0 && remaining > 0)
                    ? FormatTime(TimeSpan.FromSeconds(remaining / speedBytesPerSec))
                    : "--:--:--";

                int totalFiles = Math.Max(1, state.TotalFiles);
                int doneFiles  = Math.Max(0, totalFiles - state.RemainingFiles);

                int barWidth = Math.Max(10, width - 60);
                int filled   = (int)Math.Round(barWidth * percent / 100.0);
                string bar   = "["
                    + new string('=', filled)
                    + (filled < barWidth ? ">" : "=")
                    + new string(' ', Math.Max(0, barWidth - filled - 1))
                    + "]";

                string left  = $"Files: {doneFiles}/{totalFiles} | {Percent(percent)} | {HumanSize(transferred)}/{HumanSize(total)}";
                string right = $"ETA: {eta} | {TruncatePath(state.CurrentSourceFile, 30)}";
                string line  = left.PadRight(2) + " "
                    + bar.PadRight(barWidth + 2) + " "
                    + right.PadLeft(Math.Max(0, width - (left.Length + bar.Length + 4)));

                Console.SetCursorPosition(0, row);
                Console.Write(line.Substring(0, Math.Min(line.Length, width)).PadRight(width));
            }
            catch { }
        }
    }

    private static string Percent(double p) => p.ToString("0.0") + "%";

    private static string HumanSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return string.Format("{0:0.##} {1}", len, sizes[order]);
    }

    private static string FormatTime(TimeSpan t) =>
        string.Format("{0:D2}:{1:D2}:{2:D2}", (int)t.TotalHours, t.Minutes, t.Seconds);

    private static string TruncatePath(string path, int maxLen)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        if (path.Length <= maxLen) return path;
        var file = Path.GetFileName(path);
        if (file.Length + 4 >= maxLen)
            return "..." + file[^Math.Min(file.Length, maxLen - 3)..];
        int left = maxLen - file.Length - 3;
        return path.Substring(0, left) + "..." + file;
    }
}