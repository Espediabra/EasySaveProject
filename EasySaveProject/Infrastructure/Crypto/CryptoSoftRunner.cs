namespace EasySaveProject.Infrastructure.Crypto;

// XOR encryption — one file at a time (global semaphore enforces serialization).
// Returns: milliseconds elapsed (>= 1) on success, 0 if skipped, -1 on error.

public class CryptoSoftRunner
{
    private static readonly SemaphoreSlim _guard = new(1, 1);

    public CryptoSoftRunner(string _exePath) { }

    public int Encrypt(string filePath, string key)
    {
        _guard.Wait();
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length == 0) return 0;

            byte[] data = File.ReadAllBytes(filePath);
            for (int i = 0; i < data.Length; i++)
                data[i] ^= keyBytes[i % keyBytes.Length];
            File.WriteAllBytes(filePath, data);

            sw.Stop();
            int ms = (int)sw.ElapsedMilliseconds;
            return ms > 0 ? ms : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CryptoSoftRunner] XOR encryption failed: {ex.Message}");
            return -1;
        }
        finally
        {
            _guard.Release();
        }
    }
}
