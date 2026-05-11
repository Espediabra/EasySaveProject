using EasySaveProject.Models;
using EasySaveProject.Services;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;

namespace EasySaveProject.Strategies
{
    public interface IBackupStrategy
    {
        void Execute(
            BackupJob job,
            FileService fileService,
            LogService logService,
            StateService stateService,
            CryptoService cryptoService,
            BusinessSoftwareWatcher watcher
        );
    }
}
