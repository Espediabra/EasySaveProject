using EasySaveProject.Infrastructure.Process;

namespace EasySaveProject.Infrastructure.Crypto;

public class CryptoSoftRunner
{
    // V3: CryptoSoft.exe is mono-instance. This static semaphore ensures only one
    // instance runs at a time across all parallel jobs in the current process.
    private static readonly SemaphoreSlim _monoInstanceGuard = new(1, 1);

    private readonly string _exePath;

    public CryptoSoftRunner(string exePath)
    {
        _exePath = exePath;
    }

    // Returns CryptoSoft exit code: elapsed ms (>0), no-op (0), or error (<0).
    public int Encrypt(string filePath, string key)
    {
        _monoInstanceGuard.Wait();
        try
        {
            return ProcessHelper.Run(_exePath, $"\"{filePath}\" \"{key}\"");
        }
        finally
        {
            _monoInstanceGuard.Release();
        }
    }
}
