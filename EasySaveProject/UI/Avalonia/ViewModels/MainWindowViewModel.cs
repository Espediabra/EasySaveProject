using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyLog;
using EasySaveProject.Models;
using EasySaveProject.Core.Services;
using EasySaveProject.Core.Localization;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;
using EasySaveProject.Core;

namespace EasySaveProject.UI.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public LocalizationManager Loc => LocalizationManager.Instance;
    private readonly BackupService _backupService;
    private readonly LogService _logService;
    private readonly ProgressService _progressService = new();
    private readonly CryptoService _cryptoService;

    public bool IsRunning { get; private set; }

    public event Action<bool>? OnExecutionStateChanged;

    private void SetRunning(bool state)
    {
        IsRunning = state;
        OnPropertyChanged(nameof(IsRunning));
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLogs))]
    [NotifyPropertyChangedFor(nameof(HasFilteredLogs))]
    [NotifyPropertyChangedFor(nameof(HasNoFilteredLogs))]
    private int _selectedLogLevelIndex = 0;

    [ObservableProperty] private string _searchLogs = "";

    // Date picker for log browsing — default to today
    [ObservableProperty] private int _logsDay   = DateTime.Today.Day;
    [ObservableProperty] private int _logsMonth = DateTime.Today.Month;
    [ObservableProperty] private int _logsYear  = DateTime.Today.Year;

    [ObservableProperty] private string _logsDateLabel = "";
    [ObservableProperty] private bool   _logsHasNoEntries = false;

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

    public ObservableCollection<PriorityExtensionItem> PriorityExtensionsView { get; } = new();

    private void RefreshPriorityExtensionsView()
    {
        PriorityExtensionsView.Clear();
        int count = PriorityExtensions.Count;
        for (int i = 0; i < count; i++)
        {
            PriorityExtensionsView.Add(new PriorityExtensionItem
            {
                Extension  = PriorityExtensions[i],
                CanMoveUp  = i > 0,
                CanMoveDown = i < count - 1
            });
        }
    }

    // Large file threshold (V3)
    [ObservableProperty]
    private long _largeFileThresholdKb = 0;

    // CryptoSoft (V3)
    [ObservableProperty]
    private string _cryptoKey = "";

    [ObservableProperty]
    private ObservableCollection<string> _cryptoExtensions = new();

    [ObservableProperty]
    private string _newCryptoExtension = "";

    // Log centralization (V3) — index-based so ComboBox items can be translated
    [ObservableProperty]
    private int _logModeIndex = 0;

    public List<string> LogModeOptions => new()
    {
        Loc["Settings.LogMode.Local"],
        Loc["Settings.LogMode.Remote"],
        Loc["Settings.LogMode.Both"]
    };

    public List<string> LogLevelOptions => new()
    {
        Loc["Log.Filter.All"],
        Loc["Log.Level.Info"],
        Loc["Log.Level.Warning"],
        Loc["Log.Level.Error"]
    };

    [ObservableProperty]
    private string _logServerHost = "localhost";

    [ObservableProperty]
    private int _logServerPort = 9000;

    // ── Toast ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _toastMessage = "";
    [ObservableProperty] private bool _showToast = false;

    // ── Global pause ──────────────────────────────────────────────────────
    public bool IsGloballyPaused => _backupService.IsGloballyPaused;
    public string GlobalPauseContent => IsGloballyPaused ? "▶" : "⏸";

    // ── Sidebar job tier ──────────────────────────────────────────────────
    public int CurrentTierMax =>
        Jobs.Count switch
        {
            < 10  => 10,
            < 20  => 20,
            < 50  => 50,
            _     => 100
        };

    public string BackupTier =>
        Jobs.Count switch
        {
            < 10  => "Starter",
            < 20  => "Advanced",
            < 50  => "Power User",
            _     => "Archive Master"
        };

    public string JobsCountText =>
        string.Format(
            LocalizationManager.Instance["Nav.JobsCount"],
            Jobs.Count
        );

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

        BusinessSoftware     = new ObservableCollection<string>(config.BusinessSoftware);
        PriorityExtensions   = new ObservableCollection<string>(config.PriorityExtensions);
        LargeFileThresholdKb = config.LargeFileThresholdKb;
        LogModeIndex = config.LogMode switch { LogMode.Remote => 1, LogMode.Both => 2, _ => 0 };
        LogServerHost = config.LogServerHost;
        LogServerPort = config.LogServerPort;
        CryptoKey        = config.CryptoKey;
        CryptoExtensions = new ObservableCollection<string>(config.CryptoExtensions);

        RefreshPriorityExtensionsView();

        var lang = string.IsNullOrWhiteSpace(config.Langage) ? "en" : config.Langage;
        LocalizationManager.Instance.CurrentLanguage = lang;

        LogService.Initialize(new LocalizationService(), config.LogFormat, config);
        _logService = LogService.Instance;

        var pauseService = new PauseService();

        _cryptoService = new CryptoService(
            config.CryptoExtensions,
            config.CryptoKey,
            Path.Combine(AppContext.BaseDirectory, "CryptoSoft.exe")
        );

        var watcher             = new BusinessSoftwareWatcher(configService);
        var priorityCoordinator = new PriorityCoordinator(config.PriorityExtensions);
        var largeFileGuard      = new LargeFileTransferGuard(config.LargeFileThresholdKb);

        watcher.OnJobPaused += processes =>
            Dispatcher.UIThread.Post(() =>
                ShowToastMessage(string.Format(Loc["Jobs.Business.Paused"], processes)));

        watcher.OnJobResumed += processes =>
            Dispatcher.UIThread.Post(() =>
                ShowToastMessage(string.Format(Loc["Jobs.Business.Resumed"], processes)));

        _backupService = new BackupService(
            fileService,
            _logService,
            stateService,
            _cryptoService,
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
        OnPropertyChanged(nameof(CurrentTierMax));
        OnPropertyChanged(nameof(BackupTier));
        OnPropertyChanged(nameof(JobsCountText));
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

    private void LoadTodayLogs() => LoadLogsForDate(DateTime.Today);

    [RelayCommand]
    void LoadLogsByDate()
    {
        try
        {
            var date = new DateTime(LogsYear, LogsMonth, LogsDay);
            LoadLogsForDate(date);
        }
        catch
        {
            // Invalid date combination — ignore
        }
    }

    private void LoadLogsForDate(DateTime date)
    {
        // Sync picker fields when called programmatically (e.g. on first navigation)
        LogsDay   = date.Day;
        LogsMonth = date.Month;
        LogsYear  = date.Year;

        LogEntries.Clear();
        try
        {
            var entries = _logService.GetByDate(date);
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

        string dateStr = date.ToString("d");
        if (LogEntries.Count > 0)
            LogsDateLabel = string.Format(Loc["Logs.Date.Showing"], dateStr);
        else
            LogsDateLabel = string.Format(Loc["Logs.Date.NoLogs"], dateStr);

        LogsHasNoEntries = LogEntries.Count == 0;

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
        job.EtaText = "";
        job.CurrentFile = "";

        var timer = StartProgressTimer();
        SetRunning(true);

        string finalStatus = "Completed";
        try
        {
            await Task.Run(() => { finalStatus = _backupService.RunJob(index); });

            job.Status = finalStatus;
            if (finalStatus == "Completed")
            {
                job.Progress = 100;
                ShowToastMessage(string.Format(LocalizationManager.Instance["Jobs.Toast.Completed"], job.Name));
            }
        }
        catch (Exception ex)
        {
            job.Status = "Error";
            ShowToastMessage(string.Format(Loc["Jobs.Toast.Error"], ex.Message));
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
            job.EtaText = "";
            job.CurrentFile = "";
        }

        var timer = StartProgressTimer();
        SetRunning(true);

        try
        {
            var results = await _backupService.RunJobsParallelAsync(indices);

            foreach (var job in selected)
            {
                job.Status = results.TryGetValue(job.Name, out var s) ? s : "Completed";
                if (job.Status == "Completed") job.Progress = 100;
            }

            int completed = results.Values.Count(s => s == "Completed");
            ShowToastMessage(string.Format(Loc["Jobs.Toast.MultiCompleted"], completed, selected.Count));
        }
        catch (Exception ex)
        {
            foreach (var job in selected) job.Status = "Error";
            ShowToastMessage(string.Format(Loc["Jobs.Toast.Error"], ex.Message));
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
    void ToggleGlobalPause()
    {
        if (_backupService.IsGloballyPaused)
            _backupService.ResumeAll();
        else
            _backupService.PauseAll();

        OnPropertyChanged(nameof(IsGloballyPaused));
        OnPropertyChanged(nameof(GlobalPauseContent));
    }

    [RelayCommand]
    void PauseAll() => _backupService.PauseAll();

    [RelayCommand]
    void ResumeAll() => _backupService.ResumeAll();

    [RelayCommand]
    void StopAll()
    {
        _backupService.StopAll();
        foreach (var job in Jobs.Where(j => j.IsRunning))
            job.Status = "Cancelled";
        OnPropertyChanged(nameof(IsGloballyPaused));
        OnPropertyChanged(nameof(GlobalPauseContent));
    }

    [RelayCommand]
    void TogglePauseJob(BackupJobViewModel job) => _backupService.TogglePauseJob(job.Name);

    [RelayCommand]
    void StopJob(BackupJobViewModel job)
    {
        _backupService.StopJob(job.Name);
    }

    // ── Progress timer (UI thread — no Dispatcher.Invoke needed) ──────────
    private DispatcherTimer StartProgressTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) =>
        {
            _progressService.Tick();

            var activeNames = _progressService.CurrentAll
                .Select(s => s.BackupName)
                .ToHashSet(StringComparer.Ordinal);

            // Update jobs that have active progress snapshots
            foreach (var snap in _progressService.CurrentAll)
            {
                var jobVm = Jobs.FirstOrDefault(j => j.Name == snap.BackupName);
                if (jobVm == null) continue;

                jobVm.TotalFiles     = snap.TotalFiles;
                jobVm.RemainingFiles = snap.TotalFiles - snap.DoneFiles; // fires ProgressText with fresh counts
                jobVm.Progress       = (int)(snap.Fraction * 100);
                jobVm.CurrentFile    = Path.GetFileName(snap.CurrentFile);

                jobVm.EtaText = snap.Eta.HasValue
                    ? $"ETA {(int)snap.Eta.Value.TotalMinutes:D2}:{snap.Eta.Value.Seconds:D2}"
                    : "";

                var controller = _backupService.GetControllerByName(snap.BackupName);
                bool isPaused  = _backupService.IsGloballyPaused || controller?.IsPaused == true;
                jobVm.Status   = isPaused ? "Paused" : snap.Status;
            }

            // Detect jobs that were running but have stopped/completed individually
            foreach (var jobVm in Jobs.Where(j => j.IsRunning))
            {
                if (activeNames.Contains(jobVm.Name)) continue;

                var finalState = BackupStateHub.ReadAll()
                    .FirstOrDefault(s => s.BackupName == jobVm.Name);
                if (finalState == null) continue;

                jobVm.Status = finalState.Status switch
                {
                    "Stopped"   => "Cancelled",
                    "Completed" => "Completed",
                    "Error"     => "Error",
                    _           => "Cancelled"
                };
                if (jobVm.Status == "Completed") jobVm.Progress = 100;
            }

            OnPropertyChanged(nameof(IsGloballyPaused));
            OnPropertyChanged(nameof(GlobalPauseContent));
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
        ShowToastMessage(Loc["Jobs.Toast.Deleted"]);
    }

    // ── Formulaire de création ────────────────────────────────────────────
    [RelayCommand]
    void OpenCreateForm()
    {
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
        if (string.IsNullOrWhiteSpace(FormName))   { FormError = Loc["Form.EmptyError"]; return; }
        if (string.IsNullOrWhiteSpace(FormSource)) { FormError = Loc["Form.EmptyError"]; return; }
        if (string.IsNullOrWhiteSpace(FormTarget)) { FormError = Loc["Form.EmptyError"]; return; }

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
        ShowToastMessage(Loc["Jobs.Toast.TypeUpdated"]);
    }

    // ── Filtrage des logs ──────────────────────────────────────────────────
    public IEnumerable<LogEntryViewModel> FilteredLogs
    {
        get
        {
            string levelFilter = SelectedLogLevelIndex switch
            {
                1 => "Info",
                2 => "Warning",
                3 => "Error",
                _ => "All"
            };

            return LogEntries
                .Where(l =>
                    (levelFilter == "All" || l.Level == levelFilter)
                    &&
                    (
                        string.IsNullOrWhiteSpace(SearchLogs)
                        || l.JobName.Contains(SearchLogs, StringComparison.OrdinalIgnoreCase)
                        || l.Message.Contains(SearchLogs, StringComparison.OrdinalIgnoreCase)
                        || l.Level.Contains(SearchLogs, StringComparison.OrdinalIgnoreCase)
                    )
                )
                .OrderByDescending(l => l.Timestamp);
        }
    }

    partial void OnSearchLogsChanged(string value)
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

    partial void OnLanguageIndexChanged(int value) => CanChangeLanguage = true;

    [RelayCommand]
    void SaveLanguage()
    {
        var langCode = LanguageIndex == 1 ? "fr" : "en";
        var configService = new ConfigService();
        var config = configService.Load();
        config.Langage = langCode;
        config.FirstRun = false;
        configService.Save(config);

        if (CurrentPage == "LanguageSelect")
        {
            // First-run flow: just navigate, no restart needed
            LocalizationManager.Instance.CurrentLanguage = langCode;
            CanChangeLanguage = false;
            CurrentPage = "Jobs";
        }
        else
        {
            // Settings flow: restart the app so every string, style and
            // computed list picks up the new language without any stale state.
            CanChangeLanguage = false;
            RestartApplication();
        }
    }

    private static void RestartApplication()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exe))
            System.Diagnostics.Process.Start(exe);
        Environment.Exit(0);
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

        ShowToastMessage(Loc["Settings.LogFormat.Saved"]);
    }

    // ── Log Mode / Log Server settings ───────────────────────────────────
    [RelayCommand]
    void SaveLogMode()
    {
        var cs = new ConfigService();
        var cfg = cs.Load();

        cfg.LogMode = LogModeIndex switch
        {
            1 => LogMode.Remote,
            2 => LogMode.Both,
            _ => LogMode.Local
        };

        cs.Save(cfg);

        LogService.Initialize(new LocalizationService(), cfg.LogFormat, cfg);

        ShowToastMessage(Loc["Settings.LogMode.Saved"]);
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

        ShowToastMessage(Loc["Settings.LogMode.ServerSaved"]);
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
            ShowToastMessage(Loc["Settings.BusinessSoftware.AlreadyExists"]);
            return;
        }

        BusinessSoftware.Add(name);

        var configService = new ConfigService();
        var config = configService.Load();

        config.BusinessSoftware = BusinessSoftware.ToList();

        configService.Save(config);

        NewBusinessSoftware = "";

        ShowToastMessage(Loc["Settings.BusinessSoftware.Added"]);
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

        ShowToastMessage(Loc["Settings.BusinessSoftware.Removed"]);
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
            ShowToastMessage(Loc["Settings.PriorityExtensions.AlreadyExists"]);
            return;
        }

        PriorityExtensions.Add(ext);
        SavePriorityExtensions();
        NewPriorityExtension = "";
        ShowToastMessage(Loc["Settings.PriorityExtensions.Added"]);
    }

    [RelayCommand]
    void RemovePriorityExtension(string ext)
    {
        PriorityExtensions.Remove(ext);
        SavePriorityExtensions();
        ShowToastMessage(Loc["Settings.PriorityExtensions.Removed"]);
    }

    [RelayCommand]
    void MovePriorityExtensionUp(string ext)
    {
        int i = PriorityExtensions.IndexOf(ext);
        if (i <= 0) return;
        PriorityExtensions.Move(i, i - 1);
        SavePriorityExtensions();
    }

    [RelayCommand]
    void MovePriorityExtensionDown(string ext)
    {
        int i = PriorityExtensions.IndexOf(ext);
        if (i < 0 || i >= PriorityExtensions.Count - 1) return;
        PriorityExtensions.Move(i, i + 1);
        SavePriorityExtensions();
    }

    private void SavePriorityExtensions()
    {
        var cs = new ConfigService(); var cfg = cs.Load();
        cfg.PriorityExtensions = PriorityExtensions.ToList();
        cs.Save(cfg);
        RefreshPriorityExtensionsView();
    }

    public void MovePriorityExtension(string fromExt, string toExt)
    {
        int fromIndex = PriorityExtensions.IndexOf(fromExt);
        int toIndex   = PriorityExtensions.IndexOf(toExt);
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex) return;
        PriorityExtensions.Move(fromIndex, toIndex);
        SavePriorityExtensions();
    }

    // ── Large File Threshold ──────────────────────────────────────────────
    [RelayCommand]
    void SaveLargeFileThreshold()
    {
        var cs = new ConfigService(); var cfg = cs.Load();
        cfg.LargeFileThresholdKb = LargeFileThresholdKb;
        cs.Save(cfg);
        ShowToastMessage(Loc["Settings.LargeFileThreshold.Updated"]);
    }

    // ── CryptoSoft settings ───────────────────────────────────────────────
    [RelayCommand]
    void SaveCryptoSettings()
    {
        var cs = new ConfigService(); var cfg = cs.Load();
        cfg.CryptoKey = CryptoKey;
        cfg.CryptoExtensions = CryptoExtensions.ToList();
        cs.Save(cfg);
        _cryptoService.Update(cfg.CryptoExtensions, cfg.CryptoKey);
        ShowToastMessage(Loc["Settings.CryptoSoft.Saved"]);
    }

    [RelayCommand]
    void AddCryptoExtension()
    {
        if (string.IsNullOrWhiteSpace(NewCryptoExtension)) return;

        var ext = NewCryptoExtension.Trim();
        if (!ext.StartsWith('.')) ext = "." + ext;

        if (CryptoExtensions.Any(x => x.Equals(ext, StringComparison.OrdinalIgnoreCase)))
        {
            ShowToastMessage(Loc["Settings.CryptoSoft.AlreadyExists"]);
            return;
        }

        CryptoExtensions.Add(ext);
        var cs = new ConfigService(); var cfg = cs.Load();
        cfg.CryptoExtensions = CryptoExtensions.ToList();
        cs.Save(cfg);
        _cryptoService.Update(cfg.CryptoExtensions, CryptoKey);
        NewCryptoExtension = "";
        ShowToastMessage(Loc["Settings.CryptoSoft.ExtAdded"]);
    }

    [RelayCommand]
    void RemoveCryptoExtension(string ext)
    {
        CryptoExtensions.Remove(ext);
        var cs = new ConfigService(); var cfg = cs.Load();
        cfg.CryptoExtensions = CryptoExtensions.ToList();
        cs.Save(cfg);
        _cryptoService.Update(cfg.CryptoExtensions, CryptoKey);
        ShowToastMessage(Loc["Settings.CryptoSoft.ExtRemoved"]);
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

        ShowCryptoSoftSetting =
            string.IsNullOrEmpty(search) || "crypto cryptosoft chiffrement encryption clé key extension".Contains(search);
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

    private bool _showCryptoSoftSetting = true;
    public bool ShowCryptoSoftSetting
    {
        get => _showCryptoSoftSetting;
        set => SetProperty(ref _showCryptoSoftSetting, value);
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
