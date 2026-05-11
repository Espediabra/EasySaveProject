using EasySaveProject.Services;
using EasySaveProject.Core.Localization;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;
using EasyLog;
using EasySaveProject.Helpers;

public class App
{
    private readonly ConfigService _configService = new();
    private readonly MainViewModel _viewModel;
    private readonly MenuService _menuService;
    private readonly ConsoleView _view;
    private readonly LocalizationService _loc = new();
    private readonly FileService _fileService = new();
    private readonly StateService _stateService = new();

    private LogService _logService;
    private AppConfig _config;

    private static readonly string CryptoSoftExePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "External", "CryptoSoft", "CryptoSoft.exe")
    );

    public App()
    {
        _config = _configService.Load();
        _menuService = new MenuService();

        _loc.Load(string.IsNullOrWhiteSpace(_config.Langage) ? "en" : _config.Langage);

        LogService.Initialize(_loc, _config.LogFormat);
        _logService = LogService.Instance;

        var cryptoService = new CryptoService(
            _config.CryptoExtensions,
            _config.CryptoKey,
            CryptoSoftExePath
        );

        var watcher = new BusinessSoftwareWatcher(_config.BusinessSoftware);

        var backupService = new BackupService(
            _fileService,
            _logService,
            _stateService,
            cryptoService,
            watcher
        );

        _viewModel = new MainViewModel(backupService);
        _view = new ConsoleView(_viewModel, _menuService, _loc);
    }

    public void Run()
    {
        _config = _configService.Load();
        _loc.Load(string.IsNullOrWhiteSpace(_config.Langage) ? "en" : _config.Langage);

        if (_config.FirstRun)
        {
            string selectedLang = AskLanguage();
            _config.Langage = selectedLang;
            _config.FirstRun = false;
            _configService.Save(_config);
        }

        _loc.Load(_config.Langage);
        MainLoop();
    }

    private string AskLanguage()
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.Header(_loc.T("Language.Choose"));

            var options = new List<string>
            {
                _loc.T("Language.English"),
                _loc.T("Language.French")
            };

            int choice = _menuService.ShowMenu(options);

            string selectedLang = choice switch
            {
                0 => "en",
                1 => "fr",
                _ => "en"
            };

            Console.Clear();
            ConsoleHelper.Header(_loc.T("Language.ConfirmationTitle"));

            string langLabel = selectedLang == "fr"
                ? _loc.T("Language.French")
                : _loc.T("Language.English");

            ConsoleHelper.WriteLineWithWrap(
                string.Format(_loc.T("Language.YouChose"), langLabel)
            );

            var confirmOptions = new List<string>
            {
                _loc.T("Confirm.Yes"),
                _loc.T("Confirm.No")
            };

            if (_menuService.ShowMenu(confirmOptions) == 0)
                return selectedLang;
        }
    }

    private void MainLoop()
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.Header(_loc.T("MainMenu.Title"));

            var options = new List<string>
            {
                _loc.T("MainMenu.LaunchBackup"),
                _loc.T("MainMenu.ViewLogs"),
                _loc.T("MainMenu.Settings"),
                _loc.T("MainMenu.Exit")
            };

            switch (_menuService.ShowMenu(options))
            {
                case 0: _view.ShowBackupMenu(); break;
                case 1: ShowLogsMenu(); break;
                case 2: OpenSettings(); break;
                case 3: return;
            }
        }
    }

    private void OpenSettings()
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.Header(_loc.T("Settings.Title"));

            var options = new List<string>
            {
                _loc.T("Settings.ChangeLanguage"),
                _loc.T("Settings.ChangeLogFormat"),
                _loc.T("Settings.Back")
            };

            switch (_menuService.ShowMenu(options))
            {
                case 0: ChangeLanguage(); break;
                case 1: ChangeLogFormat(); break;
                case 2: return;
            }
        }
    }

    private void ChangeLanguage()
    {
        string newLang = AskLanguage();
        _config.Langage = newLang;
        _configService.Save(_config);
        _loc.Load(newLang);

        Console.Clear();
        ConsoleHelper.WriteLineWithWrap(_loc.T("Settings.LanguageUpdated"));
        Console.ReadKey();
    }

    private void ChangeLogFormat()
    {
        Console.Clear();
        ConsoleHelper.Header(_loc.T("Settings.ChooseLogFormat"));

        int choice = _menuService.ShowMenu(new List<string> { "JSON", "XML" });

        _config.LogFormat = choice == 1 ? LogFormat.Xml : LogFormat.Json;
        _configService.Save(_config);

        LogService.Initialize(_loc, _config.LogFormat);
        _logService = LogService.Instance;

        Console.WriteLine(_loc.T("Settings.LogFormatUpdated"));
        Console.ReadKey();
    }

    private void ShowLogsMenu()
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.Header(_loc.T("Logs.Title"));

            var options = new List<string>
            {
                _loc.T("Logs.Today"),
                _loc.T("Logs.ByLevel"),
                _loc.T("Return")
            };

            switch (_menuService.ShowMenu(options))
            {
                case 0: ShowTodayLogs(); break;
                case 1: ShowLogsByLevel(); break;
                case 2: return;
            }
        }
    }

    private void ShowTodayLogs()
    {
        var logs = _logService.GetByDate(DateTime.Today);

        Console.Clear();
        ConsoleHelper.Header(_loc.T("Logs.TodayHeader"));
        _logService.PrintSimple(logs);
        Console.ReadKey();
    }

    private void ShowLogsByLevel()
    {
        Console.Clear();
        ConsoleHelper.Header(_loc.T("Logs.SelectLevel"));

        var options = new List<string>
        {
            _loc.T("Log.Level.Info"),
            _loc.T("Log.Level.Warning"),
            _loc.T("Log.Level.Error"),
            _loc.T("Return")
        };

        int choice = _menuService.ShowMenu(options);

        if (choice == 3) return;

        var level = choice switch
        {
            0 => LogLevel.INFO,
            1 => LogLevel.WARNING,
            2 => LogLevel.ERROR,
            _ => LogLevel.INFO
        };

        var logs = _logService.GetByLevel(DateTime.Today, level);

        Console.Clear();
        ConsoleHelper.Header(string.Format(_loc.T("Logs.LevelHeader"), level));
        _logService.PrintSimple(logs);
        Console.ReadKey();
    }

    public void RunCli(string arg)
    {
        _config = _configService.Load();
        _loc.Load(string.IsNullOrWhiteSpace(_config.Langage) ? "en" : _config.Langage);

        var indices = ArgumentParser.Parse(arg);
        var jobs = _viewModel.GetJobsRaw();

        if (jobs.Count == 0)
        {
            Console.WriteLine("No backup jobs found.");
            Console.WriteLine("Run without arguments to create jobs.");
            return;
        }

        foreach (var index in indices)
        {
            int realIndex = index - 1;

            if (realIndex < 0 || realIndex >= jobs.Count)
            {
                Console.WriteLine($"Job {index} does not exist, skipping.");
                continue;
            }

            _viewModel.ExecuteBackup(realIndex);
            Console.WriteLine($"Executed job {index}");
        }
    }
}
