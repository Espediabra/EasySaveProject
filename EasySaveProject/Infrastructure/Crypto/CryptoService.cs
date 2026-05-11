namespace EasySaveProject.Infrastructure.Crypto;

public class CryptoService
{
    private readonly CryptoSoftRunner _runner;
    private readonly string _key;
    private readonly HashSet<string> _extensions;

    public CryptoService(List<string> extensions, string key, string cryptoSoftExePath)
    {
        _key = key;
        _extensions = new HashSet<string>(
            extensions.Select(e => e.ToLowerInvariant().Trim()),
            StringComparer.OrdinalIgnoreCase
        );
        _runner = new CryptoSoftRunner(cryptoSoftExePath);
    }

    // Retourne 0 = pas de chiffrement, >0 = ms, <0 = erreur
    public int TryEncrypt(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (!_extensions.Contains(ext) || string.IsNullOrWhiteSpace(_key))
            return 0;

        try
        {
            return _runner.Encrypt(filePath, _key);
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
