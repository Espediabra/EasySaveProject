using System.Text.Json;
using EasySaveProject.Models;

namespace EasySaveProject.Core.Services;

public class StateService
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public StateService()
    {
        _filePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "Data", "State", "state.json"
        ));

        // Assure que le dossier existe
        var directory = Path.GetDirectoryName(_filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }
    }

    public void Update(State state)
    {
        lock (_lock)
        {
            List<State> states;

            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);

                states = string.IsNullOrWhiteSpace(json)
                    ? new List<State>()
                    : JsonSerializer.Deserialize<List<State>>(json) ?? new List<State>();
            }
            else
            {
                states = new List<State>();
            }

            // Remplacement de l’état du même job
            var existing = states.FirstOrDefault(s => s.BackupName == state.BackupName);
            if (existing != null)
            {
                states.Remove(existing);
            }

            states.Add(state);

            var updatedJson = JsonSerializer.Serialize(states, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, updatedJson);
        }
    }
}