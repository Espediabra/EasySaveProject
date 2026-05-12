using EasyLog;

public class AppConfig
{
    public string Langage { get; set; } = "en";
    public bool FirstRun { get; set; } = true;
    public LogFormat LogFormat { get; set; } = LogFormat.Json;

    public string CryptoKey { get; set; } = string.Empty;
    public List<string> CryptoExtensions { get; set; } = new();
    public List<string> BusinessSoftwareList { get; set; } = new();
}