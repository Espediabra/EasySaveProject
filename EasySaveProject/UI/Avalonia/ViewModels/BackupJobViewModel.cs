using CommunityToolkit.Mvvm.ComponentModel;
using EasySaveProject.Core.Localization;

namespace EasySaveProject.UI.Avalonia.ViewModels;

public partial class BackupJobViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _sourcePath = "";
    [ObservableProperty] private string _targetPath = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeLabel))]
    private string _type = "Full";
    [ObservableProperty] private int _pendingTypeIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    private string _status = "Idle";

    [ObservableProperty] private int _progress = 0;
    [ObservableProperty] private int _totalFiles = 0;
    [ObservableProperty] private int _remainingFiles = 0;
    [ObservableProperty] private bool _isSelected = false;

    public bool IsRunning => Status == "Active";
    public bool IsCompleted => Status == "Completed";
    public bool IsError => Status == "Error";

    public string StatusLabel => Status switch
    {
        "Active" => LocalizationManager.Instance["Jobs.Status.Running"],
        "Completed" => LocalizationManager.Instance["Jobs.Status.Done"],
        "Error" => LocalizationManager.Instance["Jobs.Status.Error"],
        _ => LocalizationManager.Instance["Jobs.Status.Idle"]
    };

    public string TypeLabel => Type == "Differential"
    ? LocalizationManager.Instance["Backup.TypeDifferential"]
    : LocalizationManager.Instance["Backup.TypeFull"];

    public string ProgressText => TotalFiles > 0
        ? $"{TotalFiles - RemainingFiles}/{TotalFiles} fichiers"
        : "";

    public static BackupJobViewModel FromJob(string name, string source, string target, string type)
        => new()
        {
            Name = name,
            SourcePath = source,
            TargetPath = target,
            Type = type
        };

    partial void OnTypeChanged(string value)
    {
        PendingTypeIndex = value == "Differential" ? 1 : 0;
    }
}
