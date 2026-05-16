namespace EasySaveProject.Models;

public class State
{
    // J'ai ajouté les string.Empty pour éviter les warnings de nullabilité
    public string BackupName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;

    public int TotalFiles { get; set; }
    public int RemainingFiles { get; set; }

    public long TotalSize { get; set; }
    public long RemainingSize { get; set; }

    public string CurrentSourceFile { get; set; } = string.Empty;
    public string CurrentTargetFile { get; set; } = string.Empty;

    // Non-empty when blocked by business software — comma-separated process names.
    // Empty = not blocked. The UI layer translates and formats this for display.
    public string BlockedBy { get; set; } = string.Empty;
}