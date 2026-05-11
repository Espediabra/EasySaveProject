using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace EasySaveProject.UI.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
}

/// <summary>
/// MultiValueConverter pour le fond des boutons de navigation sidebar.
/// Binding[0] = CurrentPage (string), Binding[1] = target page (string)
/// Retourne un fond semi-transparent si actif, transparent sinon.
/// </summary>
public class NavBgConverter : IMultiValueConverter
{
    public static readonly NavBgConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0]?.ToString() == values[1]?.ToString())
            return new SolidColorBrush(Color.Parse("#3D3228"));

        return Brushes.Transparent;
    }
}
