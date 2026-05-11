using EasySaveProject.Models;

namespace EasySaveProject.Core.Services;

/// <summary>
/// Calcule et maintient l'ETA à partir des states publiés par BackupStateHub.
/// Indépendant de toute UI — utilisable en console comme en GUI.
/// </summary>
public class ProgressService
{
    // ── Snapshot de session ───────────────────────────────────────────────
    private DateTime _sessionStart    = DateTime.MinValue;
    private double   _fractionAtStart = 0.0;
    private string   _currentBackup   = string.Empty;

    // ── Lissage exponentiel ───────────────────────────────────────────────
    private double        _smoothedEtaSeconds = -1.0;
    private const double  Alpha               = 0.05;

    // ── Snapshot public lisible par la vue ───────────────────────────────
    public ProgressSnapshot Current { get; private set; } = ProgressSnapshot.Empty;

    /// <summary>
    /// À appeler à chaque tick (ex: toutes les 200ms depuis la vue).
    /// Lit le state depuis BackupStateHub et met à jour Current.
    /// </summary>
    public void Tick()
    {
        var state = BackupStateHub.Read();
        if (state == null) return;

        Current = Compute(state);
    }

    private ProgressSnapshot Compute(State state)
    {
        long total       = Math.Max(1, state.TotalSize);
        long remaining   = Math.Max(0, state.RemainingSize);
        long transferred = total - remaining;
        double fraction  = Math.Min(1.0, Math.Max(0.0, transferred / (double)total));

        int totalFiles = Math.Max(1, state.TotalFiles);
        int doneFiles  = Math.Max(0, totalFiles - state.RemainingFiles);

        // Réinitialise la session si nouveau job ou régression
        if (state.BackupName != _currentBackup || fraction < _fractionAtStart)
        {
            _currentBackup      = state.BackupName;
            _sessionStart       = DateTime.UtcNow;
            _fractionAtStart    = fraction;
            _smoothedEtaSeconds = -1.0;
        }

        // Calcul ETA
        TimeSpan? eta       = null;
        double fractionDone = fraction - _fractionAtStart;

        if (_sessionStart != DateTime.MinValue && fractionDone > 0.005)
        {
            double elapsed        = (DateTime.UtcNow - _sessionStart).TotalSeconds;
            double totalEstimated = elapsed / fractionDone;
            double rawEta         = totalEstimated - elapsed;

            if (rawEta > 0)
            {
                _smoothedEtaSeconds = _smoothedEtaSeconds < 0
                    ? rawEta
                    : (_smoothedEtaSeconds * (1.0 - Alpha)) + (rawEta * Alpha);

                eta = TimeSpan.FromSeconds(_smoothedEtaSeconds);
            }
        }

        return new ProgressSnapshot(
            BackupName:      state.BackupName,
            Fraction:        fraction,
            Transferred:     transferred,
            Total:           total,
            DoneFiles:       doneFiles,
            TotalFiles:      totalFiles,
            Eta:             eta,
            CurrentFile:     state.CurrentSourceFile,
            Status:          state.Status
        );
    }
}

/// <summary>
/// Snapshot immutable de l'état de progression — passé à la vue pour affichage.
/// Aucune logique ici, que des données.
/// </summary>
public record ProgressSnapshot(
    string    BackupName,
    double    Fraction,
    long      Transferred,
    long      Total,
    int       DoneFiles,
    int       TotalFiles,
    TimeSpan? Eta,
    string    CurrentFile,
    string    Status)
{
    public static ProgressSnapshot Empty => new(
        BackupName:  string.Empty,
        Fraction:    0,
        Transferred: 0,
        Total:       1,
        DoneFiles:   0,
        TotalFiles:  1,
        Eta:         null,
        CurrentFile: string.Empty,
        Status:      string.Empty);
}