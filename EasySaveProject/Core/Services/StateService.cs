using System.Text.Json;
using EasySaveProject.Models;
using EasySaveProject.Core;

namespace EasySaveProject.Core.Services;

public class StateService
{
    private readonly string _filePath;
    private readonly object _listLock = new();  // protège _states en mémoire
    private readonly List<State> _states = new(); // cache mémoire de la liste

    // Sémaphore : une seule écriture disque à la fois, abandon si déjà en cours
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    public StateService()
    {
        _filePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "Data", "State", "state.json"));

        var directory = Path.GetDirectoryName(_filePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        // Charge l'état persisté au démarrage dans le cache mémoire
        LoadFromDisk();
    }

    public void Update(State state)
    {
        // 1. Mise à jour du cache mémoire (fusion exacte comme avant)
        lock (_listLock)
        {
            var existing = _states.FirstOrDefault(s => s.BackupName == state.BackupName);
            if (existing != null)
                _states.Remove(existing);

            _states.Add(state);
        }

        // 2. Publication immédiate vers le footer (zéro I/O)
        BackupStateHub.Publish(state);

        // 3. Persistance disque en arrière-plan, non-bloquante
        _ = WriteJsonAsync();
    }

    private async Task WriteJsonAsync()
    {
        // Si une écriture est déjà en cours on abandonne :
        // le prochain Update() persistera une version plus récente de toute façon
        if (!await _writeSemaphore.WaitAsync(0))
            return;

        try
        {
            // Snapshot thread-safe de la liste pour l'écriture
            List<State> snapshot;
            lock (_listLock)
            {
                snapshot = _states.ToList();
            }

            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Écriture atomique via fichier temporaire
            // → jamais de fichier JSON corrompu à mi-écriture
            var tempPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch { /* non critique, le footer lit depuis la mémoire */ }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath)) return;

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            var loaded = JsonSerializer.Deserialize<List<State>>(json);
            if (loaded != null)
                _states.AddRange(loaded);
        }
        catch { /* fichier corrompu ou absent, on repart de zéro */ }
    }
}