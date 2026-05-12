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
                    ShowBusinessSoftwareMenu();
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

        config.LogFormat = choice == 1 ? LogFormat.Xml : LogFormat.Json;
        _configService.Save(config);

        LogService.Initialize(_loc, config.LogFormat);

        Console.WriteLine(_loc.T("Settings.LogFormatUpdated"));
        Console.ReadKey();
    }

    private void ShowBusinessSoftwareMenu()
    {
        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("Settings.BusinessSoftware"));

            var config = _configService.Load();
            var list = config.BusinessSoftwareList;

            if (list.Count == 0)
            {
                Console.WriteLine(_loc.T("Business.Empty"));
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine(_loc.T("Business.CurrentList"));
                for (int i = 0; i < list.Count; i++)
                    Console.WriteLine($"  {i + 1}. {list[i]}");
                Console.WriteLine();
            }

            var options = new List<string>
            {
                _loc.T("Business.Add"),
                _loc.T("Business.Remove"),
                _loc.T("Settings.Back")
            };

            int choice = _menu.Select(options);

            switch (choice)
            {
                case 0:
                    AddBusinessSoftware();
                    break;

                case 1:
                    RemoveBusinessSoftware();
                    break;

                case 2:
                    return;
            }
        }
    }

    private void AddBusinessSoftware()
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Business.Add"));

        Console.Write(_loc.T("Business.AddPrompt"));
        string? input = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine(_loc.T("Form.EmptyError"));
            Console.ReadKey();
            return;
        }

        if (!input.EndsWith(".exe"))
            input += ".exe";

        var config = _configService.Load();

        if (config.BusinessSoftwareList.Contains(input, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine(_loc.T("Business.AlreadyExists"));
            Console.ReadKey();
            return;
        }

        config.BusinessSoftwareList.Add(input);
        _configService.Save(config);

        Console.WriteLine(string.Format(_loc.T("Business.Added"), input));
        Console.ReadKey();
    }

    private void RemoveBusinessSoftware()
    {
        var config = _configService.Load();
        var list = config.BusinessSoftwareList;

        if (list.Count == 0)
        {
            Console.WriteLine(_loc.T("Business.Empty"));
            Console.ReadKey();
            return;
        }

        Console.Clear();
        HeaderComponent.Render(_loc.T("Business.Remove"));

        var options = list.ToList();
        options.Add(_loc.T("Settings.Back"));

        int choice = _menu.Select(options);

        if (choice == options.Count - 1)
            return;

        string removed = list[choice];
        list.RemoveAt(choice);
        _configService.Save(config);

        Console.WriteLine(string.Format(_loc.T("Business.Removed"), removed));
        Console.ReadKey();
    }
}
