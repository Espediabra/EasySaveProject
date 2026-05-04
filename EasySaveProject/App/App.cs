using EasySaveProject.Services;
using EasySaveProject.Core.Localization;
using EasyLog;
using System.Reflection.Metadata.Ecma335;

public class App
{
    private readonly ConfigService _configService = new();
    private readonly MainViewModel _viewModel;
    private readonly MenuService _menuService;
    private readonly ConsoleView _view;
    private readonly LocalizationService _loc = new();
    private readonly FileService _fileService = new();
    private readonly StateService _stateService = new();
    private readonly LogService _logService;

    public App()
    {
        _menuService = new MenuService();

        LogService.Initialize(_loc);
        _logService = LogService.Instance;

        var backupService = new BackupService(
            _fileService,
            _logService,
            _stateService
        );

        _viewModel = new MainViewModel(backupService);

        _view = new ConsoleView(_viewModel, _menuService, _loc);
    }

    public void Run()
    {
        var config = _configService.Load();

        _loc.Load(string.IsNullOrWhiteSpace(config.Langage) ? "en" : config.Langage);

        if (config.FirstRun)
        {
            string selectedLang = AskLanguage();

            config.Langage = selectedLang;
            config.FirstRun = false;

            _configService.Save(config);
        }

        _loc.Load(config.Langage);

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

            int confirmChoice = _menuService.ShowMenu(confirmOptions);

            if (confirmChoice == 0)
            {
                return selectedLang;
            }
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

            int choice = _menuService.ShowMenu(options);

            switch (choice)
            {
                case 0:
                    _view.ShowBackupMenu();
                    break;

                case 1:
                    ShowLogsMenu();
                    break;

                case 2:
                    OpenSettings();
                    break;

                case 3:
                    return;
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
                _loc.T("Settings.Back")
            };

            int choice = _menuService.ShowMenu(options);

            switch (choice)
            {
                case 0:
                    ChangeLanguage();
                    break;

                case 1:
                    return;
            }
        }
    }

    private void ChangeLanguage()
    {
        var config = _configService.Load();

        string newLang = AskLanguage();

        config.Langage = newLang;
        _configService.Save(config);

        _loc.Load(newLang);

        Console.Clear();
        ConsoleHelper.WriteLineWithWrap(_loc.T("Settings.LanguageUpdated"));
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

            int choice = _menuService.ShowMenu(options);

            switch (choice)
            {
                case 0:
                    ShowTodayLogs();
                    break;

                case 1:
                    ShowLogsByLevel();
                    break;

                case 2:
                    return;
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

        // gestion du retour
        if (choice == 3)
            return;

        var level = choice switch
        {
            0 => LogLevel.INFO,
            1 => LogLevel.WARNING,
            2 => LogLevel.ERROR,
            _ => LogLevel.INFO
        };

        var logs = _logService.GetByLevel(DateTime.Today, level);

        Console.Clear();
        ConsoleHelper.Header(
            string.Format(_loc.T("Logs.LevelHeader"), level)
        );

        _logService.PrintSimple(logs);

        Console.ReadKey();
    }
}