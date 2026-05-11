using CommunityToolkit.Mvvm.ComponentModel;

namespace EasySaveProject.UI.Avalonia.ViewModels;

public partial class LogEntryViewModel : ObservableObject
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "Info";
    public string JobName { get; set; } = "";
    public string Message { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public long TransferTimeMs { get; set; }

    public string TimeFormatted => Timestamp.ToString("HH:mm:ss");
    public string DateFormatted => Timestamp.ToString("dd/MM/yyyy");
    public string SizeFormatted => FileSizeBytes > 0 ? $"{FileSizeBytes / 1024.0:F1} Ko" : "—";
    public string TransferFormatted => TransferTimeMs > 0 ? $"{TransferTimeMs} ms" : (TransferTimeMs == -1 ? "Erreur" : "—");

    // Couleurs alignées sur la charte
    public string LevelColor => Level switch
    {
        "Error" => "#6F1A07",
        "Warning" => "#AF9164",
        _ => "#3D7A40"
    };

    public string LevelBg => Level switch
    {
        "Error" => "#F5E0DC",
        "Warning" => "#FBF5E6",
        _ => "#E4F0E5"
    };
}
