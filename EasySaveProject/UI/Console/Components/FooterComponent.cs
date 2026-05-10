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

    // ── Session ───────────────────────────────────────────────────────────
    private static DateTime _sessionStart    = DateTime.MinValue;
    private static double   _fractionAtStart = 0.0;
    private static string   _currentBackup   = string.Empty;

    // ── Historique des estimations brutes (médiane glissante) ─────────────
    // On garde les N dernières valeurs rawEta et on affiche leur médiane
    // La médiane élimine les pics sans délai de convergence
    private const  int    MedianWindow = 12; // ~2.4 secondes à 200ms/poll
    private static readonly Queue<double> _etaHistory = new();

    // ── Gel d'affichage en cas de stagnation ──────────────────────────────
    private static double   _lastFraction      = -1.0;
    private static DateTime _lastFractionChange = DateTime.MinValue;
    private static string   _frozenEta          = "--:--:--";
    private const  double   StagnationThreshold  = 0.0001; // fraction minimale de mouvement
    private const  double   FreezeAfterSeconds   = 1.5;    // gèle après 1.5s sans mouvement

    public static void Start()
    {
        if (_cts != null)
            return;

        ResetSession();
        _cts = new CancellationTokenSource();
        Task.Run(() => PollLoop(_cts.Token));
    }

    public static void Stop()
    {
        if (_cts == null)
            return;

        _cts.Cancel();
        _cts = null;
        ResetSession();

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

    private static void ResetSession()
    {
        _sessionStart       = DateTime.MinValue;
        _fractionAtStart    = 0.0;
        _currentBackup      = string.Empty;
        _etaHistory.Clear();
        _lastFraction       = -1.0;
        _lastFractionChange = DateTime.MinValue;
        _frozenEta          = "--:--:--";
    }

    private static async Task PollLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
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
                // ── Progression ───────────────────────────────────────────
                long total       = Math.Max(1, state.TotalSize);
                long remaining   = Math.Max(0, state.RemainingSize);
                long transferred = total - remaining;
                double fraction  = Math.Min(1.0, Math.Max(0.0, transferred / (double)total));
                double percent   = fraction * 100.0;

                // ── Détection nouveau job ─────────────────────────────────
                if (state.BackupName != _currentBackup || fraction < _fractionAtStart)
                {
                    _currentBackup   = state.BackupName;
                    _sessionStart    = DateTime.UtcNow;
                    _fractionAtStart = fraction;
                    _etaHistory.Clear();
                    _lastFraction       = fraction;
                    _lastFractionChange = DateTime.UtcNow;
                    _frozenEta          = "--:--:--";
                }

                // ── Détection stagnation ──────────────────────────────────
                DateTime now = DateTime.UtcNow;

                if (Math.Abs(fraction - _lastFraction) > StagnationThreshold)
                {
                    // La fraction a bougé : on réactive le calcul
                    _lastFraction       = fraction;
                    _lastFractionChange = now;
                }

                bool isStagnating = (now - _lastFractionChange).TotalSeconds > FreezeAfterSeconds;

                // ── Calcul ETA ────────────────────────────────────────────
                string eta = _frozenEta; // par défaut : valeur gelée

                double fractionDone = fraction - _fractionAtStart;

                if (!isStagnating && _sessionStart != DateTime.MinValue && fractionDone > 0.005)
                {
                    double elapsed        = (now - _sessionStart).TotalSeconds;
                    double totalEstimated = elapsed / fractionDone;
                    double rawEta         = Math.Max(0, totalEstimated - elapsed);

                    // Ajoute dans la fenêtre glissante
                    _etaHistory.Enqueue(rawEta);
                    while (_etaHistory.Count > MedianWindow)
                        _etaHistory.Dequeue();

                    // Médiane : trie la fenêtre et prend la valeur centrale
                    // Insensible aux pics contrairement à la moyenne
                    var sorted = _etaHistory.OrderBy(x => x).ToList();
                    double medianEta = sorted.Count % 2 == 1
                        ? sorted[sorted.Count / 2]
                        : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;

                    // On met à jour l'ETA gelé seulement quand on a une valeur calculée
                    _frozenEta = FormatTime(TimeSpan.FromSeconds(medianEta));
                    eta        = _frozenEta;
                }
                // Si stagnation : on garde _frozenEta tel quel, pas de "--:--:--"

                // ── Affichage ─────────────────────────────────────────────
                int width = Math.Max(10, Console.WindowWidth);
                int row   = Math.Max(0, Console.WindowHeight - 1);

                int totalFiles = Math.Max(1, state.TotalFiles);
                int doneFiles  = Math.Max(0, totalFiles - state.RemainingFiles);

                int barWidth = Math.Max(10, width - 60);
                int filled   = (int)Math.Round(barWidth * fraction);
                string bar   = "["
                    + new string('=', filled)
                    + (filled < barWidth ? ">" : "=")
                    + new string(' ', Math.Max(0, barWidth - filled - 1))
                    + "]";

                string left  = $"Files: {doneFiles}/{totalFiles} | {percent:0.0}% | {HumanSize(transferred)}/{HumanSize(total)}";
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

    private static string HumanSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatTime(TimeSpan t) =>
        $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";

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