using CommunityToolkit.Mvvm.ComponentModel;

namespace EasySaveProject.UI.Avalonia.ViewModels;

public partial class PriorityExtensionItem : ObservableObject
{
    [ObservableProperty] private string _extension = string.Empty;
    [ObservableProperty] private bool _canMoveUp;
    [ObservableProperty] private bool _canMoveDown;
}
