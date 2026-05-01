namespace EasySave.Models;

public class BackupJob
{
    public string Name { get; set; }
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public BackupType Type { get; set; }
}