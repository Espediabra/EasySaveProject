namespace EasySaveProject.Core.Services;

/// <summary>
/// Per-job pause/resume/stop control. Thread-safe. One instance per active backup run.
/// The GUI will bind per-job Play/Pause/Stop buttons directly to Toggle() and Stop().
/// </summary>
public class JobController
{
    private readonly ManualResetEventSlim _resumeEvent = new(initialState: true);
    private readonly object _lock = new();
    private bool _isPaused;
    private volatile bool _stopRequested;

    public string JobName { get; }

    public JobController(string jobName) { JobName = jobName; }

    public bool IsPaused { get { lock (_lock) return _isPaused; } }
    public bool IsStopRequested => _stopRequested;

    public void Pause()
    {
        lock (_lock) { _isPaused = true; _resumeEvent.Reset(); }
    }

    public void Resume()
    {
        lock (_lock) { _isPaused = false; _resumeEvent.Set(); }
    }

    public void Toggle()
    {
        lock (_lock) { if (_isPaused) Resume(); else Pause(); }
    }

    /// <summary>
    /// Signals stop and unblocks any pending WaitIfPaused.
    /// The strategy checks IsStopRequested after waking.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            _stopRequested = true;
            _isPaused = false;
            _resumeEvent.Set();
        }
    }

    /// <summary>Blocks the calling thread while this job is paused.</summary>
    public void WaitIfPaused() => _resumeEvent.Wait();
}
