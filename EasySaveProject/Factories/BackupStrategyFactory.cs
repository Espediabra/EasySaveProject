using EasySave.Models;
using EasySave.Strategies;

public class BackupStrategyFactory
{
    public IBackupStrategy Create(BackupType type)
    {
        return type switch
        {
            BackupType.Full => new FullBackupStrategy(),
            BackupType.Differential => new DifferentialBackupStrategy(),
            _ => throw new Exception("Unknown type")
        };
    }
}