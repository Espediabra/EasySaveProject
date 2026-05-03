using EasySaveProject.Services;
using EasySaveProject.Core.Localization;

public class App
{
    private readonly ConfigService _configService = new();
    private readonly MainViewModel _viewModel;
    private readonly MenuService _menuService;
    private readonly ConsoleView _view;
    private readonly FileService _fileService = new();
    private readonly LogService _logService = LogService.Instance;
    private readonly StateService _stateService = new();
    private readonly LocalizationService _loc = new();

    public App()
    {
        _menuService = new MenuService();

        var backupService = new BackupService(
            _fileService,
            _logService,
            _stateService
        );

        _viewModel = new MainViewModel(backupService);

        _view = new ConsoleView(_viewModel, _menuService);
    }
    public void Run()
    {
        var config = _configService.Load();

        if (string.IsNullOrWhiteSpace(config.Langage))
        {
            config.Langage = AskLanguage();
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
            ConsoleHelper.Header("Choose Language");

            var options = new List<string> { "English", "Français" };
            int choice = _menuService.ShowMenu(options);

            string selectedLang = choice switch
            {
                0 => "en",
                1 => "fr",
                _ => "en"
            };

            Console.Clear();
            ConsoleHelper.Header("Confirmation");

            string langLabel = selectedLang == "fr" ? "Français" : "English";

            ConsoleHelper.WriteLineWithWrap($"You have chosen: {langLabel}. Do you confirm?");

            var confirmOptions = new List<string> { "Yes", "No" };

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
                    Console.Write(_loc.T("Logs coming soon..."));
                    Console.ReadKey();
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

        _loc.Load(newLang); // reload translations immediately

        Console.Clear();
        ConsoleHelper.WriteLineWithWrap(_loc.T("Settings.LanguageUpdated"));
        Console.ReadKey();
    }
}