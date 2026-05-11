using System.Text.Json;

public class ConfigService
{
    private readonly string FilePath;

    public ConfigService()
    {
        FilePath = Path.Combine(AppContext.BaseDirectory,
        "Data", "Config", "appsettings.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppConfig();
        }

        var json = File.ReadAllText(FilePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new AppConfig();
        }

        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig(); // "??" -> Null-coalescing operator : If the left side is null, use the right side instead
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}