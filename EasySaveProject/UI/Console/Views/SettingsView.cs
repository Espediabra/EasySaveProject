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
                _loc.T("Settings.PriorityExtensions"),
                _loc.T("Settings.LargeFileThreshold"),
                _loc.T("Settings.LogMode"),
                _loc.T("Settings.Back")
            };

            int choice = _menu.Select(options);

            switch (choice)
            {
                case 0: ChangeLanguage(); break;
                case 1: ChangeLogFormat(); break;
                case 2: ManageBusinessSoftware(); break;
                case 3: ManagePriorityExtensions(); break;
                case 4: ManageLargeFileThreshold(); break;
                case 5: ManageLogMode(); break;
                case 6: return;
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

        LogService.Initialize(_loc, config.LogFormat, config);

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

    // ── Priority extensions ───────────────────────────────────────────────

    private void ManagePriorityExtensions()
    {
        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("Settings.PriorityExtensions"));

            var config = _configService.Load();

            if (config.PriorityExtensions.Count == 0)
                Console.WriteLine(_loc.T("Settings.PriorityExtensions.Empty"));
            else
            {
                Console.WriteLine(_loc.T("Settings.PriorityExtensions.Current"));
                for (int i = 0; i < config.PriorityExtensions.Count; i++)
                    Console.WriteLine($"  {i + 1}. {config.PriorityExtensions[i]}");
            }
            Console.WriteLine();

            var options = new List<string>
            {
                _loc.T("Settings.PriorityExtensions.Add"),
                _loc.T("Settings.PriorityExtensions.Remove"),
                _loc.T("Settings.PriorityExtensions.Reorder"),
                _loc.T("Settings.Back")
            };

            int choice = _menu.Select(options);

            switch (choice)
            {
                case 0: AddPriorityExtension(config);    break;
                case 1: RemovePriorityExtension(config); break;
                case 2: ReorderPriorityExtensions(config); break;
                case 3: return;
            }
        }
    }

    private void AddPriorityExtension(AppConfig config)
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Settings.PriorityExtensions.Add"));
        Console.Write(_loc.T("Settings.PriorityExtensions.AddPrompt"));

        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            return;

        // Normalize: lowercase, ensure leading dot
        string ext = input.Trim().ToLowerInvariant();
        if (!ext.StartsWith('.'))
            ext = "." + ext;

        bool alreadyExists = config.PriorityExtensions
            .Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));

        if (!alreadyExists)
        {
            config.PriorityExtensions.Add(ext);
            _configService.Save(config);
            Console.WriteLine(_loc.T("Settings.PriorityExtensions.Added"));
        }
        else
        {
            Console.WriteLine(_loc.T("Settings.PriorityExtensions.AlreadyExists"));
        }

        Console.ReadKey();
    }

    private void RemovePriorityExtension(AppConfig config)
    {
        if (config.PriorityExtensions.Count == 0)
        {
            Console.WriteLine(_loc.T("Settings.PriorityExtensions.Empty"));
            Console.ReadKey();
            return;
        }

        Console.Clear();
        HeaderComponent.Render(_loc.T("Settings.PriorityExtensions.Remove"));

        var options = new List<string>(config.PriorityExtensions);
        options.Add(_loc.T("Settings.Back"));

        int choice = _menu.Select(options);

        if (choice == options.Count - 1)
            return;

        config.PriorityExtensions.RemoveAt(choice);
        _configService.Save(config);

        Console.WriteLine(_loc.T("Settings.PriorityExtensions.Removed"));
        Console.ReadKey();
    }

    private void ReorderPriorityExtensions(AppConfig config)
    {
        if (config.PriorityExtensions.Count < 2)
        {
            Console.WriteLine(_loc.T("Settings.PriorityExtensions.Empty"));
            Console.ReadKey();
            return;
        }

        int cursor = 0;

        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("Settings.PriorityExtensions.Reorder"));
            Console.WriteLine(_loc.T("Settings.PriorityExtensions.ReorderHelp"));
            Console.WriteLine();

            int count = config.PriorityExtensions.Count;
            for (int i = 0; i < count; i++)
            {
                string marker = i == cursor ? " → " : "   ";
                Console.WriteLine($"{marker}{config.PriorityExtensions[i]}");
            }

            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow when cursor > 0:
                    cursor--;
                    break;

                case ConsoleKey.DownArrow when cursor < count - 1:
                    cursor++;
                    break;

                case ConsoleKey.LeftArrow when cursor > 0:
                    (config.PriorityExtensions[cursor], config.PriorityExtensions[cursor - 1]) =
                        (config.PriorityExtensions[cursor - 1], config.PriorityExtensions[cursor]);
                    cursor--;
                    break;

                case ConsoleKey.RightArrow when cursor < count - 1:
                    (config.PriorityExtensions[cursor], config.PriorityExtensions[cursor + 1]) =
                        (config.PriorityExtensions[cursor + 1], config.PriorityExtensions[cursor]);
                    cursor++;
                    break;

                case ConsoleKey.Enter:
                    _configService.Save(config);
                    Console.WriteLine(_loc.T("Settings.PriorityExtensions.ReorderSaved"));
                    Console.ReadKey();
                    return;

                case ConsoleKey.Escape:
                    return;
            }
        }
    }

    // ── Large file threshold ──────────────────────────────────────────────

    private void ManageLargeFileThreshold()
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Settings.LargeFileThreshold"));

        var config = _configService.Load();

        Console.WriteLine(string.Format(_loc.T("Settings.LargeFileThreshold.Current"), config.LargeFileThresholdKb));
        Console.WriteLine();
        Console.Write(_loc.T("Settings.LargeFileThreshold.Prompt"));

        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            return;

        if (!long.TryParse(input.Trim(), out long value) || value < 0)
        {
            Console.WriteLine(_loc.T("Settings.LargeFileThreshold.Invalid"));
            Console.ReadKey();
            return;
        }

        config.LargeFileThresholdKb = value;
        _configService.Save(config);

        Console.WriteLine(_loc.T("Settings.LargeFileThreshold.Updated"));
        Console.ReadKey();
    }

    // ── Log centralization ────────────────────────────────────────────────

    private void ManageLogMode()
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Settings.LogMode"));

        var config = _configService.Load();

        Console.WriteLine($"{_loc.T("Settings.LogMode.Current")}: {config.LogMode}");
        Console.WriteLine($"  Host: {config.LogServerHost}:{config.LogServerPort}");
        Console.WriteLine($"  Machine ID: {config.MachineId}");
        Console.WriteLine();

        var options = new List<string>
        {
            _loc.T("Settings.LogMode.Local"),
            _loc.T("Settings.LogMode.Remote"),
            _loc.T("Settings.LogMode.Both"),
            _loc.T("Settings.LogMode.EditServer"),
            _loc.T("Settings.Back")
        };

        int choice = _menu.Select(options);

        switch (choice)
        {
            case 0:
                config.LogMode = LogMode.Local;
                _configService.Save(config);
                LogService.Initialize(_loc, config.LogFormat, config);
                Console.WriteLine(_loc.T("Settings.LogMode.Updated"));
                Console.ReadKey();
                break;

            case 1:
                config.LogMode = LogMode.Remote;
                _configService.Save(config);
                LogService.Initialize(_loc, config.LogFormat, config);
                Console.WriteLine(_loc.T("Settings.LogMode.Updated"));
                Console.ReadKey();
                break;

            case 2:
                config.LogMode = LogMode.Both;
                _configService.Save(config);
                LogService.Initialize(_loc, config.LogFormat, config);
                Console.WriteLine(_loc.T("Settings.LogMode.Updated"));
                Console.ReadKey();
                break;

            case 3:
                EditServerConfig(config);
                break;

            case 4:
                return;
        }
    }

    private void EditServerConfig(AppConfig config)
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Settings.LogMode.EditServer"));

        Console.Write($"{_loc.T("Settings.LogMode.Host")} [{config.LogServerHost}]: ");
        var host = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(host))
            config.LogServerHost = host;

        Console.Write($"{_loc.T("Settings.LogMode.Port")} [{config.LogServerPort}]: ");
        var portStr = Console.ReadLine()?.Trim();
        if (int.TryParse(portStr, out int port) && port > 0 && port < 65536)
            config.LogServerPort = port;

        Console.Write($"{_loc.T("Settings.LogMode.MachineId")} [{config.MachineId}]: ");
        var machineId = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(machineId))
            config.MachineId = machineId;

        _configService.Save(config);
        LogService.Initialize(_loc, config.LogFormat, config);

        Console.WriteLine(_loc.T("Settings.LogMode.Updated"));
        Console.ReadKey();
    }
}