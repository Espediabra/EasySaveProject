using EasySaveProject.Core.Localization;

public class ConfirmDialog
{
    private readonly MenuComponent _menu;
    private readonly LocalizationService _loc;

    public ConfirmDialog(MenuComponent menu, LocalizationService loc)
    {
        _menu = menu;
        _loc = loc;
    }

    public bool Ask(string title)
    {
        Console.Clear();
        HeaderComponent.Render(title);

        var options = new List<string>
        {
            _loc.T("Confirm.Yes"),
            _loc.T("Confirm.No")
        };

        return _menu.Select(options) == 0;
    }
}