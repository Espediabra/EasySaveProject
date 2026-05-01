using EasySave.Models;

public interface IBackupStrategy
{
    void Execute(BackupJob job);
}