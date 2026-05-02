using EasySaveProject.Models;
using EasySaveProject.Strategies;

namespace EasySaveProject.Factories
{
    public class BackupStrategyFactory
    {
        public static IBackupStrategy Create(BackupType type)
        {
            return type switch
            {
                BackupType.Full => new FullBackupStrategy(),
                BackupType.Differential => new DifferentialBackupStrategy(),
                _ => throw new ArgumentException("Invalid backup type")
            };
        }
    }
}