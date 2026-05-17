namespace EasySaveProject.Infrastructure.Crypto;

public class CryptoService
{
    private readonly CryptoSoftRunner _runner;
    private readonly object _lock = new();
    private string _key;
    private HashSet<string> _extensions;

    public CryptoService(List<string> extensions, string key, string cryptoSoftExePath)
    {
        _key = key;
        _extensions = new HashSet<string>(
            extensions.Select(e => e.ToLowerInvariant().Trim()),
            StringComparer.OrdinalIgnoreCase
        );
        _runner = new CryptoSoftRunner(cryptoSoftExePath);
    }

    public void Update(List<string> extensions, string key)
    {
        lock (_lock)
        {
            _key = key;
            _extensions = new HashSet<string>(
                extensions.Select(e => e.ToLowerInvariant().Trim()),
                StringComparer.OrdinalIgnoreCase
            );
        }
    }

    // Retourne 0 = pas de chiffrement, >0 = ms, <0 = erreur
    public int TryEncrypt(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        string key;
        HashSet<string> extensions;
        lock (_lock) { key = _key; extensions = _extensions; }

        if (!extensions.Contains(ext) || string.IsNullOrWhiteSpace(key))
            return 0;

        try
        {
            return _runner.Encrypt(filePath, key);
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"[CryptoService] {ex.Message}");
            return -1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CryptoService] Erreur : {ex.Message}");
            return -1;
        }
    }
}
