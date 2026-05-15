using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasySaveProject.Core.Localization;

public class LocalizationManager : INotifyPropertyChanged
{
    private readonly LocalizationService _service = new();

    private static LocalizationManager? _instance;
    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _currentLanguage = "en";
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                _service.Load(value);

                OnPropertyChanged(nameof(CurrentLanguage));
                OnPropertyChanged("Item"); // IMPORTANT pour indexer
            }
        }
    }

    public string this[string key] => _service.T(key);

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}