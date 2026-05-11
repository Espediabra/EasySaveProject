using CommunityToolkit.Mvvm.ComponentModel;

namespace EasySaveProject.UI.Avalonia.ViewModels;

public partial class BackupJobViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _sourcePath = "";
    [ObservableProperty] private string _targetPath = "";
    [ObservableProperty] private string _type = "Full";

    [ObservableProperty] private string _status = "Idle";
    [ObservableProperty] private int _progress = 0;
    [ObservableProperty] private int _totalFiles = 0;
    [ObservableProperty] private int _remainingFiles = 0;

    public bool IsRunning => Status == "Active";
    public bool IsCompleted => Status == "Completed";
    public bool IsError => Status == "Error";

    public string StatusLabel => Status switch
    {
        "Active" => "En cours…",
        "Completed" => "Terminé",
        "Error" => "Erreur",
        _ => "Prêt"
    };

    public string TypeLabel => Type == "Differential" ? "Différentielle" : "Complète";

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
}
