using EasySaveProject.Core.Services;
using EasySaveProject.Core.Localization;
using EasyLog;
using System.Reflection.Metadata.Ecma335;
using EasySaveProject.Helpers;

public class App
{
    private readonly ConfigService _configService = new();
    private readonly MainViewModel _viewModel;
    private readonly LocalizationService _loc = new();
    private readonly FileService _fileService = new();
    private readonly StateService _stateService = new();
    private readonly LogService _logService;
    private readonly MainMenuView _mainMenu;
    private readonly MenuComponent _menu;
    private readonly SettingsView _settingsView;
    private readonly LanguageForm _languageForm;

    public App()
    {
        _menu = new MenuComponent();

        // Charger la config
        var config = _configService.Load();

        // Charger la langue AVANT toute UI
        var lang = string.IsNullOrWhiteSpace(config.Langage)
            ? "en"
            : config.Langage;

        _loc.Load(lang);

        // Init logging
        LogService.Initialize(_loc);
        _logService = LogService.Instance;

        // Services métier
        var backupService = new BackupService(
            _fileService,
            _logService,
            _stateService
        );

        _viewModel = new MainViewModel(backupService);

        // Forms 
        var inputForm = new InputForm(_loc);
        var confirmDialog = new ConfirmDialog(_menu, _loc);
        var backupTypeForm = new BackupTypeForm(_menu, _loc);

        var createBackupForm = new CreateBackupForm(
            _viewModel,
            inputForm,
            backupTypeForm,
            confirmDialog,
            _loc
        );

        // Views
        var backupMenuView = new BackupMenuView(
            _viewModel,
            _menu,
            _loc,
            createBackupForm,
            confirmDialog,
            backupTypeForm
        );

        _languageForm = new LanguageForm(_menu, _loc);

        _settingsView = new SettingsView(
            _menu,
            _loc,
            _configService,
            _languageForm
        );

        var logView = new LogView(_menu, _loc, _logService);

        _mainMenu = new MainMenuView(
            _menu,
            _loc,
            backupMenuView,
            logView,
            _settingsView
        );
    }

    public void Run()
    {

        var startup = new StartupFlow(
            _configService,
            _loc,
            _languageForm
        );

        startup.Run();

        _mainMenu.Show();
    }

    // public void RunCli(string arg)
    // {
    //     var config = _configService.Load();

    //     _loc.Load(string.IsNullOrWhiteSpace(config.Langage) ? "en" : config.Langage);

    //     var indices = ArgumentParser.Parse(arg);

    //     var jobs = _viewModel.GetJobsRaw();


    //     if (jobs.Count == 0)
    //     {
    //         Console.WriteLine("No backup jobs found.");
    //         Console.WriteLine("Run without arguments to create jobs.");
    //         return;
    //     }

    //     foreach (var index in indices)
    //     {
    //         int realIndex = index - 1;

    //         if (realIndex < 0 || realIndex >= jobs.Count)
    //         {
    //             Console.WriteLine($"Job {index} does not exist, skipping.");
    //             continue;
    //         }

    //         _viewModel.ExecuteBackup(realIndex);
    //         Console.WriteLine($"Executed job {index}");
    //     }
    // }
}