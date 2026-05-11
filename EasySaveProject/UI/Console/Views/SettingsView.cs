using EasySaveProject.Core.Localization;
using EasySaveProject.Core.Services;
using EasyLog;

public class SettingsView
{
    private readonly MenuComponent _menu;
    private readonly LocalizationService _loc;
    private readonly ConfigService _configService;
    private readonly LanguageForm _languageForm;

    public SettingsView(
    MenuComponent menu,
    LocalizationService loc,
    ConfigService configService,
    LanguageForm languageForm)
    {
        _menu = menu;
        _loc = loc;
        _configService = configService;
        _languageForm = languageForm;
    }

    public void Show()
    {
        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("Settings.Title"));

            var options = new List<string>
            {
                _loc.T("Settings.ChangeLanguage"),
                _loc.T("Settings.ChangeLogFormat"),
                _loc.T("Settings.Back")
            };

            int choice = _menu.Select(options);

            switch (choice)
            {
                case 0:
                    ChangeLanguage();
                    break;

                case 1:
                    ChangeLogFormat();
                    break;

                case 2:
                    return;
            }
        }
    }

    private void ChangeLanguage()
    {
        var config = _configService.Load();

        string newLang = _languageForm.AskLanguage();

        config.Langage = newLang;
        _configService.Save(config);

        _loc.Load(newLang);

        Console.Clear();
        ConsoleHelper.WriteLineWithWrap(_loc.T("Settings.LanguageUpdated"));
        Console.ReadKey();
    }

    private void ChangeLogFormat()
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Settings.ChooseLogFormat"));

        var options = new List<string> { "JSON", "XML" };
        int choice = _menu.Select(options);

        var config = _configService.Load();

        config.LogFormat = choice == 1 ? LogFormat.Xml : LogFormat.Json;
        _configService.Save(config);

        LogService.Initialize(_loc, config.LogFormat);

        Console.WriteLine(_loc.T("Settings.LogFormatUpdated"));
        Console.ReadKey();
    }
}