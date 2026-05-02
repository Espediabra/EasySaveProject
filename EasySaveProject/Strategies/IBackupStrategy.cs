using EasySaveProject.Models;
using EasySaveProject.Services;

namespace EasySaveProject.Strategies
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