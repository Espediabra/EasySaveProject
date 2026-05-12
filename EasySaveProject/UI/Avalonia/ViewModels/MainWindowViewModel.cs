using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySaveProject.Models;
using EasySaveProject.Core.Services;
using EasySaveProject.Core.Localization;

namespace EasySaveProject.UI.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly BackupService _backupService;
    private readonly MainViewModel _coreViewModel;
    private readonly LogService _logService;

    // ── Navigation ───────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLanguageSelectPageVisible))]
    [NotifyPropertyChangedFor(nameof(IsJobsPageVisible))]
    [NotifyPropertyChangedFor(nameof(IsLogsPageVisible))]
    [NotifyPropertyChangedFor(nameof(IsSettingsPageVisible))]
    private string _currentPage = "LanguageSelect";

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
    [ObservableProperty] private string _formType = "Full";
    [ObservableProperty] private string _formError = "";

    // Prompt "Exécuter maintenant?" après création
    [ObservableProperty] private bool _showRunNowPrompt = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RunNowJobTitle))]
    private string _runNowJobName = "";

    private int _pendingRunJobIndex = -1;

    public string RunNowJobTitle => $"Sauvegarde « {RunNowJobName} » créée avec succès.";

    // Multi-sélection
    public bool HasSelectedJobs => Jobs.Any(j => j.IsSelected);

    // Page Logs
    [ObservableProperty] private ObservableCollection<LogEntryViewModel> _logEntries = new();
    [ObservableProperty] private string _selectedLogLevel = "All";

    public bool HasFilteredLogs => FilteredLogs.Any();
    public bool HasNoFilteredLogs => !FilteredLogs.Any();

    // Page Settings
    [ObservableProperty] private string _selectedLanguage = "English";

    // ── Toast ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _toastMessage = "";
    [ObservableProperty] private bool _showToast = false;

    public MainWindowViewModel()
    {
        var fileService = new FileService();
        var stateService = new StateService();
        var loc = new LocalizationService();

        var configService = new ConfigService();
        var config = configService.Load();
        try { loc.Load(string.IsNullOrWhiteSpace(config.Langage) ? "en" : config.Langage); }
        catch { }

        SelectedLanguage = config.Langage == "fr" ? "Français" : "English";

        LogService.Initialize(loc);
        _logService = LogService.Instance;

        _backupService = new BackupService(fileService, _logService, stateService);
        _coreViewModel = new MainViewModel(_backupService);

        Jobs.CollectionChanged += OnJobsCollectionChanged;

        LoadJobsFromService();
        LoadTodayLogs();
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
    }

    private void OnJobPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BackupJobViewModel.IsSelected))
            OnPropertyChanged(nameof(HasSelectedJobs));
    }

    private void LoadJobsFromService()
    {
        Jobs.Clear();
        foreach (var job in _coreViewModel.GetJobsRaw())
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
                    Message = e.Message,
                    FileSizeBytes = e.FileSizeBytes,
                    TransferTimeMs = e.TransferTimeMs
                });
            }
        }
        catch { }

        OnPropertyChanged(nameof(HasFilteredLogs));
        OnPropertyChanged(nameof(HasNoFilteredLogs));
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

        try
        {
            await Task.Run(() => _coreViewModel.ExecuteBackup(index));
            job.Status = "Completed";
            job.Progress = 100;
            ShowToastMessage($"Sauvegarde « {job.Name} » terminée ✓");
        }
        catch (Exception ex)
        {
            job.Status = "Error";
            ShowToastMessage($"Erreur : {ex.Message}");
        }

        LoadTodayLogs();
    }

    // ── Lancer les sauvegardes sélectionnées ─────────────────────────────
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

        try
        {
            await Task.Run(() => _coreViewModel.ExecuteMultipleBackups(indices));
            foreach (var job in selected)
            {
                job.Status = "Completed";
                job.Progress = 100;
            }
            ShowToastMessage($"{selected.Count} sauvegarde(s) terminée(s) ✓");
        }
        catch (Exception ex)
        {
            foreach (var job in selected)
                job.Status = "Error";
            ShowToastMessage($"Erreur : {ex.Message}");
        }

        LoadTodayLogs();
    }

    // ── Supprimer un job ──────────────────────────────────────────────────
    [RelayCommand]
    void DeleteJob(BackupJobViewModel job)
    {
        int index = Jobs.IndexOf(job);
        if (index < 0) return;

        _coreViewModel.DeleteJob(index);
        Jobs.Remove(job);

        if (SelectedJob == job) CloseJobPanel();
        ShowToastMessage("Sauvegarde supprimée.");
    }

    // ── Formulaire de création ────────────────────────────────────────────
    [RelayCommand]
    void OpenCreateForm()
    {
        if (Jobs.Count >= 5)
        {
            ShowToastMessage("Maximum 5 sauvegardes atteint. Supprimez-en une avant d'en créer une nouvelle.");
            return;
        }

        FormName = FormSource = FormTarget = FormError = "";
        FormType = "Full";
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
        if (Jobs.Count >= 5) { FormError = "Maximum 5 sauvegardes atteint."; return; }

        var type = FormType == "Differential" ? BackupType.Differential : BackupType.Full;
        _coreViewModel.CreateJob(FormName, FormSource, FormTarget, type);

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
        ShowToastMessage($"Sauvegarde « {RunNowJobName} » créée.");
    }

    // ── Modifier le type d'un job ─────────────────────────────────────────
    [RelayCommand]
    void SaveJobType(BackupJobViewModel job)
    {
        int index = Jobs.IndexOf(job);
        if (index < 0) return;

        var type = job.Type == "Differential" ? BackupType.Differential : BackupType.Full;
        _coreViewModel.ChangeJobType(index, type);
        ShowToastMessage("Type de sauvegarde mis à jour.");
    }

    // ── Filtrage des logs ──────────────────────────────────────────────────
    public IEnumerable<LogEntryViewModel> FilteredLogs =>
        SelectedLogLevel == "All"
            ? LogEntries
            : LogEntries.Where(l => l.Level == SelectedLogLevel);

    partial void OnSelectedLogLevelChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredLogs));
        OnPropertyChanged(nameof(HasFilteredLogs));
        OnPropertyChanged(nameof(HasNoFilteredLogs));
    }

    partial void OnSelectedLanguageChanged(string value)
        => ShowSaveLanguageButton = true;

    // ── Langue ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _showSaveLanguageButton = true;

    [RelayCommand]
    void SaveLanguage()
    {
        var langCode = SelectedLanguage == "Français" ? "fr" : "en";
        var configService = new ConfigService();
        var config = configService.Load();
        config.Langage = langCode;
        config.FirstRun = false;
        configService.Save(config);

        ShowSaveLanguageButton = false;

        if (CurrentPage == "LanguageSelect")
            CurrentPage = "Jobs";
        else
            ShowToastMessage("Langue enregistrée. Redémarrez l'application.");
    }

    // ── Toast ─────────────────────────────────────────────────────────────
    private async void ShowToastMessage(string msg)
    {
        ToastMessage = msg;
        ShowToast = true;
        await Task.Delay(2800);
        ShowToast = false;
    }
}
