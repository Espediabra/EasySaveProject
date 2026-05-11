using EasySaveProject.Core.Localization;

public class MainMenuView
{
    private readonly MenuComponent _menu;
    private readonly LocalizationService _loc;
    private readonly BackupMenuView _backupMenuView;
    private readonly LogView _logView;
    private readonly SettingsView _settingsView;

    public MainMenuView(
    MenuComponent menu,
    LocalizationService loc,
    BackupMenuView backupMenuView,
    LogView logView,
    SettingsView settingsView)
    {
        _menu = menu;
        _loc = loc;
        _backupMenuView = backupMenuView;
        _logView = logView;
        _settingsView = settingsView;
    }

    public void Show()
    {
        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("MainMenu.Title"));

            var options = new List<string>
            {
                _loc.T("MainMenu.LaunchBackup"),
                _loc.T("MainMenu.ViewLogs"),
                _loc.T("MainMenu.Settings"),
                _loc.T("MainMenu.Exit")
            };

            int choice = _menu.Select(options);

            switch (choice)
            {
                case 0:
                    _backupMenuView.Show();
                    break;

                case 1:
                    _logView.Show();
                    break;

                case 2:
                    _settingsView.Show();
                    break;

                case 3:
                    return;
            }
        }
    }
}