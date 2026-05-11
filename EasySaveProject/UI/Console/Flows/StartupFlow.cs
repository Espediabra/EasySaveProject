using EasySaveProject.Core.Localization;
public class StartupFlow
{
    private readonly ConfigService _configService;
    private readonly LocalizationService _loc;
    private readonly LanguageForm _languageForm;

    public StartupFlow(
        ConfigService configService,
        LocalizationService loc,
        LanguageForm languageForm)
    {
        _configService = configService;
        _loc = loc;
        _languageForm = languageForm;
    }

    public void Run()
    {
        var config = _configService.Load();

        if (!config.FirstRun)
        {
            _loc.Load(config.Langage);
            return;
        }

        string selectedLang = _languageForm.AskLanguage();

        config.Langage = selectedLang;
        config.FirstRun = false;

        _configService.Save(config);

        _loc.Load(selectedLang);
    }
}