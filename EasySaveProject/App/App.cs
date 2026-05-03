using EasySaveProject.Services;

public class App
{
    private readonly ConfigService _configService = new();
    private readonly MainViewModel _viewModel;
    private readonly MenuService _menuService;
    private readonly ConsoleView _view;
    private readonly FileService _fileService = new();
    private readonly LogService _logService = new();
    private readonly StateService _stateService = new();

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
            ConsoleHelper.Header("Main Menu");

            var options = new List<string>
                {
                    "Launch a backup",
                    "View logs",
                    "Settings",
                    "Exit"
                };

            int choice = _menuService.ShowMenu(options);

            switch (choice)
            {
                case 0:
                    Console.Write("hbsjdfkjsf");
                    Console.ReadKey();
                    // _view.ShowBackupMenu();
                    break;

                case 1:
                    Console.Write("Logs coming soon...");
                    Console.ReadKey();
                    break;

                case 2:
                    Console.Write("Settings coming soon...");
                    Console.ReadKey();
                    break;

                case 3:
                    return;
            }
        }
    }
}