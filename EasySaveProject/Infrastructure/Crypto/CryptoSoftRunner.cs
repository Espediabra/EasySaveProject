using EasySaveProject.Infrastructure.Process;

namespace EasySaveProject.Infrastructure.Crypto;

// Lance CryptoSoft.exe en mono-instance

public class CryptoSoftRunner
{
    private static readonly SemaphoreSlim _guard = new(1, 1);

    private readonly string _exePath;

    public CryptoSoftRunner(string exePath)
    {
        _exePath = exePath;
    }

    public int Encrypt(string filePath, string key)
    {
        _guard.Wait();
        try
        {
            return ProcessHelper.Run(_exePath, $"\"{filePath}\" \"{key}\"");
        }
        finally
        {
            _guard.Release();
        }
    }
}
