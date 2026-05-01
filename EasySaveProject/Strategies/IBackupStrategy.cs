using EasySave.Models;
using EasySave.Services;

namespace EasySave.Strategies
{
    public interface IBackupStrategy
    {
        void Execute(
            BackupJob job,
            FileService fileService,
            LogService logService,
            StateService stateService
        );
    }
}