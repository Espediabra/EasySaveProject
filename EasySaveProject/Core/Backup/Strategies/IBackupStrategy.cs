using EasySaveProject.Models;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;
using EasySaveProject.Core.Services;

namespace EasySaveProject.Core.Strategies;

public interface IBackupStrategy
{
    void Execute(
        BackupJob job,
        FileService fileService,
        LogService logService,
        StateService stateService,
        CryptoService cryptoService,
        BusinessSoftwareWatcher watcher,
        PauseService pauseService,
        PriorityCoordinator priorityCoordinator,
        LargeFileTransferGuard largeFileGuard
    );
}