using EasySaveProject.Core.Localization;
using EasySaveProject.Core.Services;
using EasySaveProject.Models;


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
                _loc.T("Settings.BusinessSoftware"),
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
                    ManageBusinessSoftware();
                    break;

                case 3:
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

        config.LogFormat = choice == 1
            ? AppLogFormat.Xml
            : AppLogFormat.Json;

        _configService.Save(config);

        var easyLogFormat =
            config.LogFormat == AppLogFormat.Xml
                ? EasyLog.LogFormat.Xml
                : EasyLog.LogFormat.Json;

        LogService.Initialize(_loc, easyLogFormat);

        Console.WriteLine(_loc.T("Settings.LogFormatUpdated"));
        Console.ReadKey();
    }

    private void ManageBusinessSoftware()
    {
        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("Settings.BusinessSoftware"));

            var config = _configService.Load();

            if (config.BusinessSoftware.Count == 0)
            {
                Console.WriteLine(_loc.T("Settings.BusinessSoftware.Empty"));
            }
            else
            {
                Console.WriteLine(_loc.T("Settings.BusinessSoftware.Current"));
                for (int i = 0; i < config.BusinessSoftware.Count; i++)
                    Console.WriteLine($"  {i + 1}. {config.BusinessSoftware[i]}");
            }
            Console.WriteLine();

            var options = new List<string>
            {
                _loc.T("Settings.BusinessSoftware.Add"),
                _loc.T("Settings.BusinessSoftware.Remove"),
                _loc.T("Settings.Back")
            };

            int choice = _menu.Select(options);

            switch (choice)
            {
                case 0:
                    AddBusinessSoftware(config);
                    break;

                case 1:
                    RemoveBusinessSoftware(config);
                    break;

                case 2:
                    return;
            }
        }
    }

    private void AddBusinessSoftware(AppConfig config)
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Settings.BusinessSoftware.Add"));
        Console.Write(_loc.T("Settings.BusinessSoftware.AddPrompt"));

        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            return;

        string name = input.Trim();

        bool alreadyExists = config.BusinessSoftware
            .Any(p => p.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (!alreadyExists)
        {
            config.BusinessSoftware.Add(name);
            _configService.Save(config);
            Console.WriteLine(_loc.T("Settings.BusinessSoftware.Added"));
        }
        else
        {
            Console.WriteLine(_loc.T("Settings.BusinessSoftware.AlreadyExists"));
        }

        Console.ReadKey();
    }

    private void RemoveBusinessSoftware(AppConfig config)
    {
        if (config.BusinessSoftware.Count == 0)
        {
            Console.WriteLine(_loc.T("Settings.BusinessSoftware.Empty"));
            Console.ReadKey();
            return;
        }

        Console.Clear();
        HeaderComponent.Render(_loc.T("Settings.BusinessSoftware.Remove"));

        var options = new List<string>(config.BusinessSoftware);
        options.Add(_loc.T("Settings.Back"));

        int choice = _menu.Select(options);

        if (choice == options.Count - 1)
            return;

        config.BusinessSoftware.RemoveAt(choice);
        _configService.Save(config);

        Console.WriteLine(_loc.T("Settings.BusinessSoftware.Removed"));
        Console.ReadKey();
    }
}