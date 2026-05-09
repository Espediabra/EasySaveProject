using EasyLog;

public class AppConfig
{
    public string Langage { get; set; } = "en";
    public bool FirstRun { get; set; } = true;
    public LogFormat LogFormat { get; set; } = LogFormat.Json;
}