using System.Text.Json;
using EasySaveProject.Models;

namespace EasySaveProject.Services;

public class StateService
{
    private readonly string _filePath = "state.json";
    private readonly object _lock = new();

    public void Update(State state)
    {
        lock (_lock)
        {
            List<State> states;

            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);

                if (string.IsNullOrWhiteSpace(json))
                    states = new List<State>();
                else
                    states = JsonSerializer.Deserialize<List<State>>(json) ?? new List<State>();
            }
            else
            {
                states = new List<State>();
            }

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