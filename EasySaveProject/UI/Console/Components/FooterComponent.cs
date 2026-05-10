using System;
using System.Threading;
using System.Threading.Tasks;
using EasySaveProject.Core;
using EasySaveProject.Models;

public static class FooterComponent
{
    private static CancellationTokenSource? _cts;
    private static readonly object _consoleLock = new();

    // ── ETA par progression linéaire ─────────────────────────────────────
    // On mémorise l'instant et la fraction complétée au démarrage de la session
    private static DateTime _sessionStart   = DateTime.MinValue;
    private static double   _fractionAtStart = 0.0;
    private static string   _currentBackup  = string.Empty;

    // Lissage exponentiel de l'ETA (coefficient bas = très stable)
    private static double _smoothedEtaSeconds = -1.0;
    private const  double Alpha = 0.05; // 5% nouveau, 95% historique

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
        _sessionStart    = DateTime.MinValue;
        _fractionAtStart = 0.0;
        _currentBackup   = string.Empty;
        _smoothedEtaSeconds = -1.0;
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
                // ── Progression globale ───────────────────────────────────
                long total       = Math.Max(1, state.TotalSize);
                long remaining   = Math.Max(0, state.RemainingSize);
                long transferred = total - remaining;
                double fraction  = Math.Min(1.0, Math.Max(0.0, transferred / (double)total));
                double percent   = fraction * 100.0;

                // ── Détection d'un nouveau job ────────────────────────────
                // Si le backup change ou si la progression repart de zéro,
                // on réinitialise la session pour repartir sur une base saine
                if (state.BackupName != _currentBackup || fraction < _fractionAtStart)
                {
                    _currentBackup      = state.BackupName;
                    _sessionStart       = DateTime.UtcNow;
                    _fractionAtStart    = fraction;
                    _smoothedEtaSeconds = -1.0;
                }

                // ── Calcul ETA par progression linéaire ──────────────────
                // Formule : elapsed / fractionFaiteDepuisDépart = tempsTotal
                // ETA     = tempsTotal - elapsed
                string eta = "--:--:--";

                double fractionDone = fraction - _fractionAtStart;

                if (_sessionStart != DateTime.MinValue && fractionDone > 0.005)
                {
                    // On attend 0.5% de progression avant de calculer
                    // pour éviter une estimation délirante au tout début
                    double elapsed       = (DateTime.UtcNow - _sessionStart).TotalSeconds;
                    double totalEstimated = elapsed / fractionDone;
                    double rawEta         = totalEstimated - elapsed;

                    if (rawEta > 0)
                    {
                        // Lissage exponentiel très conservateur
                        // Premier calcul : on initialise directement sans lisser
                        _smoothedEtaSeconds = _smoothedEtaSeconds < 0
                            ? rawEta
                            : (_smoothedEtaSeconds * (1.0 - Alpha)) + (rawEta * Alpha);

                        eta = FormatTime(TimeSpan.FromSeconds(_smoothedEtaSeconds));
                    }
                }

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