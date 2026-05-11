using EasySaveProject.Infrastructure.Process;

namespace EasySaveProject.Infrastructure.Crypto;

public class CryptoSoftRunner
{
    private readonly string _exePath;

    public CryptoSoftRunner(string exePath)
    {
        _exePath = exePath;
    }

    // Retourne l'exit code de CryptoSoft : temps en ms, 0 = vide, négatif = erreur
    public int Encrypt(string filePath, string key)
    {
        return ProcessHelper.Run(_exePath, $"\"{filePath}\" \"{key}\"");
    }
}
