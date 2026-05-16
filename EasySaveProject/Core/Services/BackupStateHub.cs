using EasySaveProject.Models;

namespace EasySaveProject.Core;

/// <summary>
/// In-memory publish/subscribe channel between backup strategies and the footer.
/// Thread-safe via lock. Zero I/O on the hot path.
/// Stores one State per backup job (keyed by BackupName) to support parallel execution.
/// </summary>
public static class BackupStateHub
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, State> _states = new();

    /// <summary>
    /// Called by the strategy after each file chunk. Clones the state to avoid
    /// the footer reading a partially-mutated object.
    /// </summary>
    public static void Publish(State state)
    {
        lock (_lock)
        {
            _states[state.BackupName] = new State
            {
                BackupName        = state.BackupName,
                Status            = state.Status,
                Timestamp         = state.Timestamp,
                TotalFiles        = state.TotalFiles,
                RemainingFiles    = state.RemainingFiles,
                TotalSize         = state.TotalSize,
                RemainingSize     = state.RemainingSize,
                CurrentSourceFile = state.CurrentSourceFile,
                CurrentTargetFile = state.CurrentTargetFile,
                BlockedBy         = state.BlockedBy,
            };
        }
    }

    /// <summary>
    /// Returns the state of the first active job. Kept for backward compatibility
    /// with single-job callers.
    /// </summary>
    public static State? Read()
    {
        lock (_lock)
        {
            return _states.Values.FirstOrDefault();
        }
    }

    /// <summary>
    /// Returns a snapshot of all currently tracked job states.
    /// Called every 200 ms by the footer to render one bar per job.
    /// </summary>
    public static IReadOnlyList<State> ReadAll()
    {
        lock (_lock)
        {
            return _states.Values.ToList();
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _states.Clear();
        }
    }
}
