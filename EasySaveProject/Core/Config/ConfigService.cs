using System.Text.Json;
using System.Text.Json.Serialization;

public class ConfigService
{
    private readonly string FilePath;

    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ConfigService()
    {
        FilePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "Data", "Config", "appsettings.json"
        ));
    }

    public AppConfig Load()
    {
        if (!File.Exists(FilePath))
            return new AppConfig();

        var json = File.ReadAllText(FilePath);

        if (string.IsNullOrWhiteSpace(json))
            return new AppConfig();

        try
        {
            return JsonSerializer.Deserialize<AppConfig>(json, _options)
                   ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(config, _options));
    }
}
