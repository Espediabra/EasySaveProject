using EasySaveProject.Core.Services;
using EasySaveProject.Core.Localization;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;
using EasyLog;
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

    private readonly AppConfig _config;

    private static readonly string CryptoSoftExePath =
    Path.Combine(AppContext.BaseDirectory, "CryptoSoft.exe");

    public App()
    {
        _menu = new MenuComponent();
        _interactiveMenu = new InteractiveMenuComponent(_loc);

        _config = _configService.Load();

        var lang = string.IsNullOrWhiteSpace(_config.Langage)
            ? "en"
            : _config.Langage;

        _loc.Load(lang);

        LogService.Initialize(_loc, _config.LogFormat);
        _logService = LogService.Instance;

        var cryptoService = new CryptoService(
            _config.CryptoExtensions,
            _config.CryptoKey,
            CryptoSoftExePath
        );

        // Passe la LISTE des logiciels métier (plus un seul string)
        var watcher = new BusinessSoftwareWatcher(
            _config.BusinessSoftwareList
        );

        var backupService = new BackupService(
            _fileService,
            _logService,
            _stateService,
            cryptoService,
            watcher,
            _pauseService
        );
        var footer = new FooterComponent(_progressService, _pauseService);

        _viewModel = new MainViewModel(backupService, footer);

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