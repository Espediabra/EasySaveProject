using EasySaveProject.Core.Localization;

public class LanguageForm
{
    private readonly MenuComponent _menu;
    private readonly LocalizationService _loc;

    public LanguageForm(MenuComponent menu, LocalizationService loc)
    {
        _menu = menu;
        _loc = loc;
    }

    public string AskLanguage()
    {
        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("Language.Choose"));

            var options = new List<string>
            {
                _loc.T("Language.English"),
                _loc.T("Language.French")
            };

            int choice = _menu.Select(options);

            string selectedLang = choice switch
            {
                0 => "en",
                1 => "fr",
                _ => "en"
            };

            Console.Clear();
            HeaderComponent.Render(_loc.T("Language.ConfirmationTitle"));

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

            int confirmChoice = _menu.Select(confirmOptions);

            if (confirmChoice == 0)
                return selectedLang;
        }
    }
}