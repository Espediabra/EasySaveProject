namespace EasySaveProject.Core.Services;

/// <summary>
/// Prevents simultaneous transfer of multiple files exceeding the configured threshold.
/// Uses a SemaphoreSlim(1,1) so at most one large file is copied at a time.
/// Small files are never blocked and bypass the semaphore entirely.
/// Thread-safe. Shared singleton across all concurrent strategies.
/// </summary>
public class LargeFileTransferGuard
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private long _thresholdBytes;

    /// <param name="thresholdKb">Size limit in KB. 0 disables the guard entirely.</param>
    public LargeFileTransferGuard(long thresholdKb)
    {
        _thresholdBytes = thresholdKb * 1024;
    }

    public void Update(long thresholdKb)
    {
        _thresholdBytes = thresholdKb * 1024;
    }

    public bool IsEnabled => _thresholdBytes > 0;

    /// <summary>
    /// Acquires the exclusive large-file slot if the file exceeds the threshold.
    /// Blocks the calling thread until the slot is free or cancellation is requested.
    /// Returns true if acquired (caller must call Release), false if not needed.
    /// Throws OperationCanceledException if ct is cancelled while waiting.
    /// </summary>
    public bool AcquireIfLarge(long fileSizeBytes, CancellationToken ct = default)
    {
        if (!IsEnabled || fileSizeBytes <= _thresholdBytes) return false;
        _semaphore.Wait(ct);
        return true;
    }

    /// <summary>
    /// Releases the slot. Must be called in a finally block after AcquireIfLarge.
    /// </summary>
    public void Release(bool wasAcquired)
    {
        if (wasAcquired) _semaphore.Release();
    }
}
