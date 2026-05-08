using EasySaveProject.Core.Localization;
using EasySaveProject.Models;

public class BackupTypeForm
{
    private readonly MenuComponent _menu;
    private readonly LocalizationService _loc;

    public BackupTypeForm(MenuComponent menu, LocalizationService loc)
    {
        _menu = menu;
        _loc = loc;
    }

    public BackupType Select(string title)
    {
        Console.Clear();
        HeaderComponent.Render(title);

        var options = new List<string>
        {
            _loc.T("Backup.TypeFull"),
            _loc.T("Backup.TypeDifferential")
        };

        int choice = _menu.Select(options);

        return choice == 1
            ? BackupType.Differential
            : BackupType.Full;
    }
}