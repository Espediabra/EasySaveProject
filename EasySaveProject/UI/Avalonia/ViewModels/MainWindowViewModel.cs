using System.Collections.ObjectModel;
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

<<<<<<< HEAD
    // ── Page Jobs
=======
    // Page Jobs
>>>>>>> f67e00b0db0d5bd98353b478cb7c9bebfa56a41f
    [ObservableProperty] private ObservableCollection<BackupJobViewModel> _jobs = new();
    [ObservableProperty] private BackupJobViewModel? _selectedJob;
    [ObservableProperty] private bool _showJobPanel = false;

<<<<<<< HEAD
    // Formulaire de création
=======
    // Form création sauvegarde
>>>>>>> f67e00b0db0d5bd98353b478cb7c9bebfa56a41f
    [ObservableProperty] private bool _showCreateForm = false;
    [ObservableProperty] private string _formName = "";
    [ObservableProperty] private string _formSource = "";
    [ObservableProperty] private string _formTarget = "";
    [ObservableProperty] private string _formType = "Full";
    [ObservableProperty] private string _formError = "";

<<<<<<< HEAD
    // ── Page Logs ─────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<LogEntryViewModel> _logEntries = new();
    [ObservableProperty] private string _selectedLogLevel = "All";

    // ── Page Settings ─────────────────────────────────────────────────────
=======
    // Page Logs
    [ObservableProperty] private ObservableCollection<LogEntryViewModel> _logEntries = new();
    [ObservableProperty] private string _selectedLogLevel = "All";

    // Page Settings
>>>>>>> f67e00b0db0d5bd98353b478cb7c9bebfa56a41f
    [ObservableProperty] private string _selectedLanguage = "English";

    // ── Toast ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _toastMessage = "";
    [ObservableProperty] private bool _showToast = false;

    public MainWindowViewModel()
    {
        // ── Initialisation avec les vrais services du projet console ──────
        var fileService = new FileService();
        var stateService = new StateService();
        var loc = new LocalizationService();

        // Charger la langue depuis la config
        var configService = new ConfigService();
        var config = configService.Load();
        try { loc.Load(string.IsNullOrWhiteSpace(config.Langage) ? "en" : config.Langage); }
        catch { /* fichier langue introuvable — on continue */ }

        SelectedLanguage = config.Langage == "fr" ? "Français" : "English";

        LogService.Initialize(loc);
        _logService = LogService.Instance;

        _backupService = new BackupService(fileService, _logService, stateService);
        _coreViewModel = new MainViewModel(_backupService);

        // Charger les jobs existants depuis jobs.json
        LoadJobsFromService();

        // Charger les logs du jour
        LoadTodayLogs();
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
        catch { /* pas de logs encore */ }
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

        // Rafraîchir les logs
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
<<<<<<< HEAD
        if (Jobs.Count >= 5)
        {
            ShowToastMessage("Maximum 5 sauvegardes. Supprimez-en une d'abord.");
            return;
        }
=======
>>>>>>> f67e00b0db0d5bd98353b478cb7c9bebfa56a41f
        FormName = FormSource = FormTarget = FormError = "";
        FormType = "Full";
        ShowJobPanel = false;
        SelectedJob = null;
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

        var type = FormType == "Differential" ? BackupType.Differential : BackupType.Full;
        _coreViewModel.CreateJob(FormName, FormSource, FormTarget, type);

        Jobs.Add(BackupJobViewModel.FromJob(FormName, FormSource, FormTarget, FormType));
        ShowCreateForm = false;
        FormError = "";
        ShowToastMessage($"Sauvegarde « {FormName} » créée.");
    }

    // ── Filtrage des logs ──────────────────────────────────────────────────
    public IEnumerable<LogEntryViewModel> FilteredLogs =>
        SelectedLogLevel == "All"
            ? LogEntries
            : LogEntries.Where(l => l.Level == SelectedLogLevel);

    partial void OnSelectedLogLevelChanged(string value)
        => OnPropertyChanged(nameof(FilteredLogs));

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
