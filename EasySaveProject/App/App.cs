using EasySaveProject.Core.Services;
using EasySaveProject.Core.Localization;
using EasyLog;
using System.Reflection.Metadata.Ecma335;
using EasySaveProject.Helpers;
using EasySaveProject.UI.Console.Components;

public class App
{
    private readonly ConfigService _configService = new();
    private readonly MainViewModel _viewModel;
    private readonly LocalizationService _loc = new();
    private readonly FileService _fileService = new();
    private readonly StateService _stateService = new();
    private readonly LogService _logService;
    private readonly PauseService _pauseService = new();
    private readonly ProgressService _progressService = new();
    private readonly MainMenuView _mainMenu;
    private readonly MenuComponent _menu;
    private readonly InteractiveMenuComponent _interactiveMenu;
    private readonly SettingsView _settingsView;
    private readonly LanguageForm _languageForm;
    private readonly RunCliFlow _runCliFlow;

    public App()
    {
        _menu = new MenuComponent();
        _interactiveMenu = new InteractiveMenuComponent(_loc);

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
            _stateService,
            _pauseService
        );

        var footer = new FooterComponent(_progressService, _pauseService); // ← nouveau

        _viewModel = new MainViewModel(backupService, footer); // ← ajouté

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

        _runCliFlow = new RunCliFlow(
            _viewModel,
            createBackupForm,
            _menu,
            _loc
        );

        // Views
        var backupMenuView = new BackupMenuView(
            _viewModel,
            _menu,
            _interactiveMenu,
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

    public void RunCli(string[] args)
    {
        _runCliFlow.Execute(args);
    }
}
