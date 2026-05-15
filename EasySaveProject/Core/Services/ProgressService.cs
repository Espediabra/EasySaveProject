using EasySaveProject.Models;

namespace EasySaveProject.Core.Services;

/// <summary>
/// Computes progress snapshots (including ETA) for all active backup jobs.
/// Reads from BackupStateHub every tick; one ETA tracker is maintained per job.
/// Independent of any UI — usable in console and GUI.
/// </summary>
public class ProgressService
{
    private readonly Dictionary<string, JobEtaTracker> _trackers = new();

    /// <summary>First active job snapshot — backward-compatible single-job callers.</summary>
    public ProgressSnapshot Current { get; private set; } = ProgressSnapshot.Empty;

    /// <summary>One snapshot per currently active/paused job, ordered by job name.</summary>
    public IReadOnlyList<ProgressSnapshot> CurrentAll { get; private set; } = Array.Empty<ProgressSnapshot>();

    /// <summary>
    /// Call every 200 ms from the view. Reads all job states from the hub,
    /// removes trackers for jobs no longer tracked, and recomputes snapshots.
    /// </summary>
    public void Tick()
    {
        var states = BackupStateHub.ReadAll();
        var activeNames = states.Select(s => s.BackupName).ToHashSet();

        // Remove trackers for jobs cleared from the hub
        foreach (var key in _trackers.Keys.Except(activeNames).ToList())
            _trackers.Remove(key);

        if (states.Count == 0) return;

        var snaps = states
            .Where(s => s.Status is "Active" or "Paused")
            .Select(Compute)
            .ToList();

        CurrentAll = snaps;
        Current = snaps.FirstOrDefault() ?? ProgressSnapshot.Empty;
    }

    private ProgressSnapshot Compute(State state)
    {
        if (!_trackers.TryGetValue(state.BackupName, out var tracker))
        {
            tracker = new JobEtaTracker();
            _trackers[state.BackupName] = tracker;
        }
        return tracker.Compute(state);
    }

    // ── Per-job ETA tracker ───────────────────────────────────────────────

    private sealed class JobEtaTracker
    {
        private DateTime _sessionStart       = DateTime.MinValue;
        private double   _fractionAtStart    = 0.0;
        private string   _currentBackup      = string.Empty;
        private double   _smoothedEtaSeconds = -1.0;
        private const double Alpha           = 0.05;

        public ProgressSnapshot Compute(State state)
        {
            long total       = Math.Max(1, state.TotalSize);
            long remaining   = Math.Max(0, state.RemainingSize);
            long transferred = total - remaining;
            double fraction  = Math.Min(1.0, Math.Max(0.0, transferred / (double)total));

            int totalFiles = Math.Max(1, state.TotalFiles);
            int doneFiles  = Math.Max(0, totalFiles - state.RemainingFiles);

            if (state.BackupName != _currentBackup || fraction < _fractionAtStart)
            {
                _currentBackup      = state.BackupName;
                _sessionStart       = DateTime.UtcNow;
                _fractionAtStart    = fraction;
                _smoothedEtaSeconds = -1.0;
            }

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
                BackupName:  state.BackupName,
                Fraction:    fraction,
                Transferred: transferred,
                Total:       total,
                DoneFiles:   doneFiles,
                TotalFiles:  totalFiles,
                Eta:         eta,
                CurrentFile: state.CurrentSourceFile,
                Status:      state.Status
            );
        }
    }
}

/// <summary>
/// Immutable progress snapshot passed to the view for rendering.
/// No logic here — data only.
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
