namespace EasySaveProject.Core.Services;

/// <summary>
/// Coordinates file priority across parallel backup jobs.
/// Guarantees: no non-priority file starts while any priority file is pending globally.
/// Thread-safe. Shared singleton across all concurrent strategies.
/// </summary>
public class PriorityCoordinator
{
    private readonly object _lock = new();
    // Set = no priority files pending (non-priority threads may proceed).
    // Reset = at least one priority file pending (non-priority threads block).
    private readonly ManualResetEventSlim _noPriorityPending = new(initialState: true);
    private int _pendingCount = 0;
    private List<string> _extensions;

    public PriorityCoordinator(IEnumerable<string> priorityExtensions)
    {
        _extensions = BuildList(priorityExtensions);
    }

    public void Update(IEnumerable<string> priorityExtensions)
    {
        _extensions = BuildList(priorityExtensions);
    }

    private static List<string> BuildList(IEnumerable<string> extensions)
        => extensions.Select(e => e.ToLowerInvariant()).ToList();

    public bool IsEnabled => _extensions.Count > 0;

    /// <summary>
    /// Returns the 0-based position of the file's extension in the priority list.
    /// Lower = higher priority. Returns int.MaxValue for non-priority files.
    /// </summary>
    public int GetPriorityRank(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var idx = _extensions.IndexOf(ext);
        return idx < 0 ? int.MaxValue : idx;
    }

    public bool IsPriorityFile(string filePath) => GetPriorityRank(filePath) < int.MaxValue;

    /// <summary>
    /// Called once per job at the start of its execution, after scanning files.
    /// Blocks non-priority processing until all registered priority files complete.
    /// </summary>
    public void RegisterPendingPriorityFiles(int count)
    {
        if (count <= 0) return;
        lock (_lock)
        {
            _pendingCount += count;
            _noPriorityPending.Reset();
        }
    }

    /// <summary>
    /// Called in a finally block after each priority file finishes (success or error).
    /// When count reaches zero, unblocks all waiting non-priority threads.
    /// </summary>
    public void OnPriorityFileCompleted()
    {
        lock (_lock)
        {
            _pendingCount = Math.Max(0, _pendingCount - 1);
            if (_pendingCount == 0)
                _noPriorityPending.Set();
        }
    }

    /// <summary>
    /// Blocks the calling thread if the file is non-priority and any priority file
    /// is still pending globally (across all parallel jobs).
    /// Returns immediately for priority files or when no priority files are pending.
    /// </summary>
    public void WaitIfNonPriority(string filePath)
    {
        if (!IsPriorityFile(filePath))
            _noPriorityPending.Wait();
    }
}
