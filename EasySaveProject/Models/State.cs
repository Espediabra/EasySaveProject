namespace EasySave.Models;

public class State
{
    public string BackupName { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; }

    public int TotalFiles { get; set; }
    public int RemainingFiles { get; set; }

    public long TotalSize { get; set; }
    public long RemainingSize { get; set; }

    public string CurrentSourceFile { get; set; }
    public string CurrentTargetFile { get; set; }
}