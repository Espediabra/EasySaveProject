using EasySaveProject.Models;

namespace EasySaveProject.Core;

/// <summary>
/// Canal de communication en mémoire entre la stratégie de copie et le footer.
/// Thread-safe via lock. Aucun I/O impliqué dans la lecture par le footer.
/// </summary>
public static class BackupStateHub
{
    private static readonly object _lock = new();
    private static State? _current;

    /// <summary>
    /// Appelé par la stratégie après chaque chunk copié.
    /// </summary>
    public static void Publish(State state)
    {
        lock (_lock)
        {
            // On clone pour éviter que le footer lise un objet en cours de modification
            _current = new State
            {
                BackupName        = state.BackupName,
                Status            = state.Status,
                Timestamp         = state.Timestamp,
                TotalFiles        = state.TotalFiles,
                RemainingFiles    = state.RemainingFiles,
                TotalSize         = state.TotalSize,
                RemainingSize     = state.RemainingSize,
                CurrentSourceFile = state.CurrentSourceFile,
                CurrentTargetFile = state.CurrentTargetFile,
            };
        }
    }

    /// <summary>
    /// Appelé par le footer toutes les 200ms. Retourne null si rien n'est publié.
    /// </summary>
    public static State? Read()
    {
        lock (_lock)
        {
            return _current;
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _current = null;
        }
    }
}