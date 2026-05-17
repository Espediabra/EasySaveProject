using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyLog;
using EasySaveProject.Core.Services;
using EasySaveProject.Core.Localization;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;

namespace EasySaveProject.UI.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public LocalizationManager Loc => LocalizationManager.Instance;
    private readonly BackupService _backupService;
    private readonly LogService _logService;

    public bool IsRunning { get; private set; }

    public event Action<bool>? OnExecutionStateChanged;

    private void SetRunning(bool state)
    {
        IsRunning = state;
        OnExecutionStateChanged?.Invoke(state);
    }


    // ── Navigation ───────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLanguageSelectPageVisible))]
    [NotifyPropertyChangedFor(nameof(IsJobsPageVisible))]
    [NotifyPropertyChangedFor(nameof(IsLogsPageVisible))]
    [NotifyPropertyChangedFor(nameof(IsSettingsPageVisible))]
    private string _currentPage = "Jobs";

    public bool IsLanguageSelectPageVisible => CurrentPage == "LanguageSelect";
    public bool IsJobsPageVisible => CurrentPage == "Jobs";
    public bool IsLogsPageVisible => CurrentPage == "Logs";
    public bool IsSettingsPageVisible => CurrentPage == "Settings";

    // Page Jobs
    [ObservableProperty] private ObservableCollection<BackupJobViewModel> _jobs = new();
    [ObservableProperty] private BackupJobViewModel? _selectedJob;
    [ObservableProperty] private bool _showJobPanel = false;

    // Form création sauvegarde
    [ObservableProperty] private bool _showCreateForm = false;
    [ObservableProperty] private string _formName = "";
    [ObservableProperty] private string _formSource = "";
    [ObservableProperty] private string _formTarget = "";
    [ObservableProperty] private int _formTypeIndex = 0;
    [ObservableProperty] private string _formType = "Full";
    [ObservableProperty] private string _formError = "";

    // Prompt "Exécuter maintenant?" après création
    [ObservableProperty] private bool _showRunNowPrompt = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RunNowJobTitle))]
    private string _runNowJobName = "";

    private int _pendingRunJobIndex = -1;

    public string RunNowJobTitle =>
    string.Format(
        LocalizationManager.Instance["RunNow.CreatedTitle"],
        RunNowJobName
    );

    // Multi-sélection
    public bool HasSelectedJobs => Jobs.Any(j => j.IsSelected);

    // Page Logs
    [ObservableProperty] private ObservableCollection<LogEntryViewModel> _logEntries = new();
    [ObservableProperty] private string _selectedLogLevel = "All";

    [ObservableProperty] private string _searchLogs = "";

    public bool HasFilteredLogs => FilteredLogs.Any();
    public bool HasNoFilteredLogs => !FilteredLogs.Any();

    // ── Settings ───────────────────────────────────────────────────────────

    // Page Settings
    [ObservableProperty] private string _selectedLanguage = "English";

    // Format logs
    [ObservableProperty]
    private string _selectedLogFormat = "JSON";

    public List<string> LogFormats { get; } =
    [
        "JSON",
    "XML"
    ];

    // Business software
    [ObservableProperty]
    private ObservableCollection<string> _businessSoftware = new();

    [ObservableProperty]
    private string _newBusinessSoftware = "";

    // Priority extensions (V3)
    [ObservableProperty]
    private ObservableCollection<string> _priorityExtensions = new();

    [ObservableProperty]
    private string _newPriorityExtension = "";

    // Large file threshold (V3)
    [ObservableProperty]
    private long _largeFileThresholdKb = 0;

    // Log centralization (V3)
    [ObservableProperty]
    private string _selectedLogMode = "Local";

    [ObservableProperty]
    private string _logServerHost = "localhost";

    [ObservableProperty]
    private int _logServerPort = 9000;

    public List<string> LogModes { get; } = ["Local", "Remote", "Both"];

    // ── Toast ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _toastMessage = "";
    [ObservableProperty] private bool _showToast = false;

    public MainWindowViewModel()
    {
        var fileService   = new FileService();
        var stateService  = new StateService();
        var configService = new ConfigService();
        var config        = configService.Load();

        LanguageIndex = config.Langage == "fr" ? 1 : 0;
        CurrentPage   = config.FirstRun ? "LanguageSelect" : "Jobs";
        SelectedLanguage  = config.Langage == "fr" ? "Français" : "English";
        SelectedLogFormat = config.LogFormat == LogFormat.Xml ? "XML" : "JSON";

        BusinessSoftware      = new ObservableCollection<string>(config.BusinessSoftware);
        PriorityExtensions    = new ObservableCollection<string>(config.PriorityExtensions);
        LargeFileThresholdKb  = config.LargeFileThresholdKb;
        SelectedLogMode       = config.LogMode.ToString();
        LogServerHost         = config.LogServerHost;
        LogServerPort         = config.LogServerPort;

        var lang = string.IsNullOrWhiteSpace(config.Langage) ? "en" : config.Langage;
        LocalizationManager.Instance.CurrentLanguage = lang;

        LogService.Initialize(new LocalizationService(), config.LogFormat, config);
        _logService = LogService.Instance;

        var pauseService = new PauseService();

        var cryptoService = new CryptoService(
            config.CryptoExtensions,
            config.CryptoKey,
            Path.Combine(AppContext.BaseDirectory, "CryptoSoft.exe")
        );

        var watcher              = new BusinessSoftwareWatcher(config.BusinessSoftware);
        var priorityCoordinator  = new PriorityCoordinator(config.PriorityExtensions);
        var largeFileGuard       = new LargeFileTransferGuard(config.LargeFileThresholdKb);

        _backupService = new BackupService(
            fileService,
            _logService,
            stateService,
            cryptoService,
            watcher,
            pauseService,
            priorityCoordinator,
            largeFileGuard,
            configService
        );

        Jobs.CollectionChanged += OnJobsCollectionChanged;
        LoadJobsFromService();
    }

    private void OnJobsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (BackupJobViewModel job in e.NewItems)
                job.PropertyChanged += OnJobPropertyChanged;

        if (e.OldItems != null)
            foreach (BackupJobViewModel job in e.OldItems)
                job.PropertyChanged -= OnJobPropertyChanged;

        OnPropertyChanged(nameof(HasSelectedJobs));

        OnPropertyChanged(nameof(FilteredJobs));
    }

    private void OnJobPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BackupJobViewModel.IsSelected))
            OnPropertyChanged(nameof(HasSelectedJobs));
    }

    private void LoadJobsFromService()
    {
        Jobs.Clear();
        foreach (var job in _backupService.GetJobs())
        {
            Jobs.Add(BackupJobViewModel.FromJob(
                job.Name,
                job.SourcePath,
                job.TargetPath,
                job.Type.ToString()
            ));
        }
    }

    private void LoadTodayLogs()
    {
        LogEntries.Clear();
        try
        {
            var entries = _logService.GetByDate(DateTime.Today);
            foreach (var e in entries)
            {
                LogEntries.Add(new LogEntryViewModel
                {
                    Timestamp = e.Timestamp,
                    Level = e.Level.ToString() switch
                    {
                        "INFO" => "Info",
                        "WARNING" => "Warning",
                        "ERROR" => "Error",
                        _ => e.Level.ToString()
                    },
                    JobName = e.JobName,
                    MessageKey = e.Message,
                    FileSizeBytes = e.FileSizeBytes,
                    TransferTimeMs = e.TransferTimeMs
                });
            }
        }
        catch { }

        OnPropertyChanged(nameof(FilteredLogs));
        OnPropertyChanged(nameof(HasFilteredLogs));
        OnPropertyChanged(nameof(HasNoFilteredLogs));
    }

    partial void OnSearchJobsChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredJobs));
    }

    // ── Navigation ────────────────────────────────────────────────────────
    [RelayCommand]
    void NavigateTo(string page)
    {
        CurrentPage = page;
        if (page == "Logs") LoadTodayLogs();
    }

    // ── Sélection d'un job ────────────────────────────────────────────────
    [RelayCommand]
    void SelectJob(BackupJobViewModel job)
    {
        SelectedJob = job;
        ShowCreateForm = false;
        ShowRunNowPrompt = false;
        ShowJobPanel = true;
    }

    [RelayCommand]
    void CloseJobPanel()
    {
        SelectedJob = null;
        ShowJobPanel = false;
    }

    // ── Lancer une sauvegarde ─────────────────────────────────────────────
    [RelayCommand]
    async Task RunJob(BackupJobViewModel job)
    {
        if (job.IsRunning) return;

        int index = Jobs.IndexOf(job);
        if (index < 0) return;

        job.Status = "Active";
        job.Progress = 0;

        var timer = StartProgressTimer();
        SetRunning(true);

        try
        {
            await Task.Run(() => _backupService.RunJob(index));

            job.Status = "Completed";
            job.Progress = 100;
            ShowToastMessage(string.Format(LocalizationManager.Instance["Jobs.Toast.Completed"], job.Name));
        }
        catch (Exception ex)
        {
            job.Status = "Error";
            ShowToastMessage($"Erreur : {ex.Message}");
        }
        finally
        {
            timer.Stop();
            SetRunning(false);
        }

        LoadTodayLogs();
    }

    // ── Lancer les sauvegardes sélectionnées (parallèle) ──────────────────
    [RelayCommand]
    async Task RunSelectedJobs()
    {
        var selected = Jobs.Where(j => j.IsSelected).ToList();
        if (!selected.Any()) return;

        var indices = selected.Select(j => Jobs.IndexOf(j)).Where(i => i >= 0).ToList();

        foreach (var job in selected)
        {
            job.IsSelected = false;
            job.Status = "Active";
            job.Progress = 0;
        }

        var timer = StartProgressTimer();
        SetRunning(true);

        try
        {
            await _backupService.RunJobsParallelAsync(indices);

            foreach (var job in selected) { job.Status = "Completed"; job.Progress = 100; }
            ShowToastMessage($"{selected.Count} sauvegarde(s) terminée(s) ✓");
        }
        catch (Exception ex)
        {
            foreach (var job in selected) job.Status = "Error";
            ShowToastMessage($"Erreur : {ex.Message}");
        }
        finally
        {
            timer.Stop();
            SetRunning(false);
        }

        LoadTodayLogs();
    }

    // ── Pause / Stop ──────────────────────────────────────────────────────
    [RelayCommand]
    void PauseAll() => _backupService.PauseAll();

    [RelayCommand]
    void ResumeAll() => _backupService.ResumeAll();

    [RelayCommand]
    void StopAll() => _backupService.StopAll();

    [RelayCommand]
    void TogglePauseJob(BackupJobViewModel job) => _backupService.TogglePauseJob(job.Name);

    [RelayCommand]
    void StopJob(BackupJobViewModel job) => _backupService.StopJob(job.Name);

    // ── Progress timer (UI thread — no Dispatcher.Invoke needed) ──────────
    private DispatcherTimer StartProgressTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) =>
        {
            foreach (var snap in BackupStateHub.Read())
            {
                var jobVm = Jobs.FirstOrDefault(j => j.Name == snap.BackupName);
                if (jobVm == null) continue;

                jobVm.Progress = (int)(snap.Fraction * 100);
                if (snap.Status is "Active" or "Paused" or "Completed" or "Error" or "Cancelled")
                    jobVm.Status = snap.Status;
            }
        };
        timer.Start();
        return timer;
    }

    // ── Supprimer un job ──────────────────────────────────────────────────
    [RelayCommand]
    void DeleteJob(BackupJobViewModel job)
    {
        int index = Jobs.IndexOf(job);
        if (index < 0) return;

        _backupService.DeleteJob(index);
        Jobs.Remove(job);

        if (SelectedJob == job) CloseJobPanel();
        ShowToastMessage("Sauvegarde supprimée.");
    }

    // ── Formulaire de création ────────────────────────────────────────────
    [RelayCommand]
    void OpenCreateForm()
    {
        // if (Jobs.Count >= 5)
        // {
        //     ShowToastMessage("Maximum 5 sauvegardes atteint. Supprimez-en une avant d'en créer une nouvelle.");
        //     return;
        // }

        FormName = FormSource = FormTarget = FormError = "";
        FormType = "Full";
        FormTypeIndex = 0;
        ShowJobPanel = false;
        SelectedJob = null;
        ShowRunNowPrompt = false;
        ShowCreateForm = true;
    }

    [RelayCommand]
    void CancelCreate()
    {
        ShowCreateForm = false;
        FormError = "";
    }

    [RelayCommand]
    void SubmitCreate()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { FormError = "Le nom est requis."; return; }
        if (string.IsNullOrWhiteSpace(FormSource)) { FormError = "Le chemin source est requis."; return; }
        if (string.IsNullOrWhiteSpace(FormTarget)) { FormError = "Le chemin cible est requis."; return; }
        // if (Jobs.Count >= 5) { FormError = "Maximum 5 sauvegardes atteint."; return; }

        var type = FormTypeIndex == 1 ? BackupType.Differential : BackupType.Full;

        FormType = FormTypeIndex == 1 ? "Differential" : "Full";
        _backupService.AddJob(
            new BackupJob(FormName, FormSource, FormTarget, type)
        );

        var vm = BackupJobViewModel.FromJob(FormName, FormSource, FormTarget, FormType);
        Jobs.Add(vm);

        _pendingRunJobIndex = Jobs.IndexOf(vm);
        RunNowJobName = FormName;

        ShowCreateForm = false;
        FormError = "";
        ShowRunNowPrompt = true;
    }

    public string JobsCountText =>
    string.Format(
        LocalizationManager.Instance["Nav.JobsCount"],
        Jobs.Count
    );

    // ── Prompt "Exécuter maintenant?" ─────────────────────────────────────
    [RelayCommand]
    async Task RunNow()
    {
        ShowRunNowPrompt = false;
        if (_pendingRunJobIndex < 0 || _pendingRunJobIndex >= Jobs.Count) return;

        var job = Jobs[_pendingRunJobIndex];
        _pendingRunJobIndex = -1;
        await RunJob(job);
    }

    [RelayCommand]
    void DismissRunNow()
    {
        ShowRunNowPrompt = false;
        _pendingRunJobIndex = -1;
        ShowToastMessage(
            string.Format(
                LocalizationManager.Instance["RunNow.CreatedToast"],
                RunNowJobName
            )
        );
    }

    // ── Modifier le type d'un job ─────────────────────────────────────────
    [RelayCommand]
    void SaveJobType(BackupJobViewModel job)
    {
        int index = Jobs.IndexOf(job);
        if (index < 0) return;

        var type = job.PendingTypeIndex == 1 ? BackupType.Differential : BackupType.Full;
        job.Type = job.PendingTypeIndex == 1 ? "Differential" : "Full";
        _backupService.UpdateJobType(index, type);
        ShowToastMessage("Type de sauvegarde mis à jour.");
    }

    // ── Filtrage des logs ──────────────────────────────────────────────────
    public IEnumerable<LogEntryViewModel> FilteredLogs =>
     LogEntries.Where(l =>
         (SelectedLogLevel == "All" || l.Level == SelectedLogLevel)
         &&
         (
             string.IsNullOrWhiteSpace(SearchLogs)
             || l.JobName.Contains(SearchLogs, StringComparison.OrdinalIgnoreCase)
             || l.Message.Contains(SearchLogs, StringComparison.OrdinalIgnoreCase)
             || l.Level.Contains(SearchLogs, StringComparison.OrdinalIgnoreCase)
         )
     );

    partial void OnSearchLogsChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredLogs));
        OnPropertyChanged(nameof(HasFilteredLogs));
        OnPropertyChanged(nameof(HasNoFilteredLogs));
    }

    partial void OnSelectedLogLevelChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredLogs));
        OnPropertyChanged(nameof(HasFilteredLogs));
        OnPropertyChanged(nameof(HasNoFilteredLogs));
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        if (CanChangeLanguage)
            ShowSaveLanguageButton = true;
    }

    // ── Langue ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _showSaveLanguageButton = true;
    [ObservableProperty] private bool _canChangeLanguage = true;
    [ObservableProperty] private int _languageIndex = 0;

    [RelayCommand]
    void SaveLanguage()
    {
        var langCode = LanguageIndex == 1 ? "fr" : "en";
        var configService = new ConfigService();
        var config = configService.Load();
        config.Langage = langCode;
        config.FirstRun = false;
        configService.Save(config);

        LocalizationManager.Instance.CurrentLanguage = langCode;

        CanChangeLanguage = false;

        if (CurrentPage == "LanguageSelect")
            CurrentPage = "Jobs";
        else
            ShowToastMessage(Loc["Settings.LanguageSaved"]);
    }

    // ── Toast ─────────────────────────────────────────────────────────────
    private async void ShowToastMessage(string msg)
    {
        ToastMessage = msg;
        ShowToast = true;
        await Task.Delay(2800);
        ShowToast = false;
    }

    public List<string> GetBackupNames()
    {
        return _backupService.GetJobs()
            .Select(j => j.Name)
            .ToList();
    }

    public BackupJob GetJob(int index)
    {
        return _backupService.GetJobs()[index];
    }

    public List<BackupJob> GetJobsRaw()
    {
        return _backupService.GetJobs().ToList();
    }

    // ── Changement format logs─────────────────────────────────────────────
    [RelayCommand]
    void SaveLogFormat()
    {
        var configService = new ConfigService();
        var config = configService.Load();

        config.LogFormat = SelectedLogFormat == "XML" ? LogFormat.Xml : LogFormat.Json;

        configService.Save(config);

        LogService.Initialize(new LocalizationService(), config.LogFormat, config);

        ShowToastMessage("Format des logs enregistré.");
    }

    // ── Log Mode / Log Server settings ───────────────────────────────────
    [RelayCommand]
    void SaveLogMode()
    {
        var cs = new ConfigService();
        var cfg = cs.Load();

        cfg.LogMode = SelectedLogMode switch
        {
            "Remote" => LogMode.Remote,
            "Both"   => LogMode.Both,
            _        => LogMode.Local
        };

        cs.Save(cfg);

        LogService.Initialize(new LocalizationService(), cfg.LogFormat, cfg);

        ShowToastMessage("Mode de log enregistré.");
    }

    [RelayCommand]
    void SaveLogServerSettings()
    {
        var cs = new ConfigService();
        var cfg = cs.Load();

        cfg.LogServerHost = LogServerHost;
        cfg.LogServerPort = LogServerPort;

        cs.Save(cfg);

        LogService.Initialize(new LocalizationService(), cfg.LogFormat, cfg);

        ShowToastMessage("Serveur de log enregistré.");
    }

    // ── Add Business Software ─────────────────────────────────────────────
    [RelayCommand]
    void AddBusinessSoftware()
    {
        if (string.IsNullOrWhiteSpace(NewBusinessSoftware))
            return;

        string name = NewBusinessSoftware.Trim();

        bool exists = BusinessSoftware.Any(x =>
            x.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            ShowToastMessage("Ce logiciel existe déjà.");
            return;
        }

        BusinessSoftware.Add(name);

        var configService = new ConfigService();
        var config = configService.Load();

        config.BusinessSoftware = BusinessSoftware.ToList();

        configService.Save(config);

        NewBusinessSoftware = "";

        ShowToastMessage("Logiciel ajouté.");
    }

    // ── Delete Business Software ──────────────────────────────────────────
    [RelayCommand]
    void RemoveBusinessSoftware(string software)
    {
        if (string.IsNullOrWhiteSpace(software))
            return;

        BusinessSoftware.Remove(software);

        var configService = new ConfigService();
        var config = configService.Load();

        config.BusinessSoftware = BusinessSoftware.ToList();

        configService.Save(config);

        ShowToastMessage("Logiciel supprimé.");
    }

    // ── Priority Extensions ───────────────────────────────────────────────
    [RelayCommand]
    void AddPriorityExtension()
    {
        if (string.IsNullOrWhiteSpace(NewPriorityExtension)) return;

        var ext = NewPriorityExtension.Trim();
        if (!ext.StartsWith('.')) ext = "." + ext;

        if (PriorityExtensions.Any(x => x.Equals(ext, StringComparison.OrdinalIgnoreCase)))
        {
            ShowToastMessage("Cette extension existe déjà.");
            return;
        }

        PriorityExtensions.Add(ext);
        var cs = new ConfigService(); var cfg = cs.Load();
        cfg.PriorityExtensions = PriorityExtensions.ToList();
        cs.Save(cfg);
        NewPriorityExtension = "";
        ShowToastMessage("Extension ajoutée.");
    }

    [RelayCommand]
    void RemovePriorityExtension(string ext)
    {
        PriorityExtensions.Remove(ext);
        var cs = new ConfigService(); var cfg = cs.Load();
        cfg.PriorityExtensions = PriorityExtensions.ToList();
        cs.Save(cfg);
        ShowToastMessage("Extension supprimée.");
    }

    // ── Large File Threshold ──────────────────────────────────────────────
    [RelayCommand]
    void SaveLargeFileThreshold()
    {
        var cs = new ConfigService(); var cfg = cs.Load();
        cfg.LargeFileThresholdKb = LargeFileThresholdKb;
        cs.Save(cfg);
        ShowToastMessage("Seuil enregistré.");
    }

    // ── Research settings ─────────────────────────────────────────────────
    private string _searchSettings = "";

    public string SearchSettings
    {
        get => _searchSettings;
        set
        {
            SetProperty(ref _searchSettings, value);
            UpdateSettingsVisibility();
        }
    }

    private void UpdateSettingsVisibility()
    {
        var search = SearchSettings?.ToLower() ?? "";

        ShowLanguageSetting =
            string.IsNullOrEmpty(search) || "langue language".Contains(search);

        ShowLogFormatSetting =
            string.IsNullOrEmpty(search) || "log format journaux".Contains(search);

        ShowBusinessSoftwareSetting =
            string.IsNullOrEmpty(search) || "logiciel business software".Contains(search);

        ShowPriorityExtensionsSetting =
            string.IsNullOrEmpty(search) || "priority extensions prioritaires".Contains(search);

        ShowLargeFileThresholdSetting =
            string.IsNullOrEmpty(search) || "large file threshold gros fichier seuil".Contains(search);

        ShowLogModeSetting =
            string.IsNullOrEmpty(search) || "log mode docker remote serveur centralization".Contains(search);
    }

    private bool _showLanguageSetting = true;
    public bool ShowLanguageSetting
    {
        get => _showLanguageSetting;
        set => SetProperty(ref _showLanguageSetting, value);
    }

    private bool _showLogFormatSetting = true;
    public bool ShowLogFormatSetting
    {
        get => _showLogFormatSetting;
        set => SetProperty(ref _showLogFormatSetting, value);
    }

    private bool _showBusinessSoftwareSetting = true;
    public bool ShowBusinessSoftwareSetting
    {
        get => _showBusinessSoftwareSetting;
        set => SetProperty(ref _showBusinessSoftwareSetting, value);
    }

    private bool _showPriorityExtensionsSetting = true;
    public bool ShowPriorityExtensionsSetting
    {
        get => _showPriorityExtensionsSetting;
        set => SetProperty(ref _showPriorityExtensionsSetting, value);
    }

    private bool _showLargeFileThresholdSetting = true;
    public bool ShowLargeFileThresholdSetting
    {
        get => _showLargeFileThresholdSetting;
        set => SetProperty(ref _showLargeFileThresholdSetting, value);
    }

    private bool _showLogModeSetting = true;
    public bool ShowLogModeSetting
    {
        get => _showLogModeSetting;
        set => SetProperty(ref _showLogModeSetting, value);
    }

    [ObservableProperty] private string _searchJobs = "";

    public IEnumerable<BackupJobViewModel> FilteredJobs =>
    Jobs.Where(j =>
        string.IsNullOrWhiteSpace(SearchJobs)
        || j.Name.Contains(SearchJobs, StringComparison.OrdinalIgnoreCase)
        || j.SourcePath.Contains(SearchJobs, StringComparison.OrdinalIgnoreCase)
        || j.TargetPath.Contains(SearchJobs, StringComparison.OrdinalIgnoreCase)
    );
}
