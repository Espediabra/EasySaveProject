using System.Text.Json;

namespace EasySaveProject.Core.Localization;

public class LocalizationService
{
    private Dictionary<string, string> _translations = new();

    public void Load(string language)
    {

        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "Core", "Localization", $"{language}.json"));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Language file not found: {path}");

        var json = File.ReadAllText(path);

        _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                        ?? new Dictionary<string, string>();
    }

    public string T(string key)
    {
        return _translations.TryGetValue(key, out var value)
            ? value
            : key;
    }
}