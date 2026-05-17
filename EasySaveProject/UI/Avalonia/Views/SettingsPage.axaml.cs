using Avalonia.Controls;
using Avalonia.Input;
using EasySaveProject.UI.Avalonia.ViewModels;

namespace EasySaveProject.UI.Avalonia.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage() => InitializeComponent();

    // ── Priority extensions drag reorder ─────────────────────────────────
    // Pointer-events approach: press on handle, release on target item.
    // Works with all Avalonia 12 versions without the DragDrop system.

    private string? _draggedExt;

    private void PriorityExt_DragHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed
            && sender is Control ctrl
            && ctrl.DataContext is string ext)
        {
            _draggedExt = ext;
            e.Handled = true;
        }
    }

    private void PriorityExt_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (_draggedExt == null) return;
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            _draggedExt = null;
            return;
        }
        if (sender is not Control ctrl) return;
        if (ctrl.DataContext is not string toExt) return;
        if (_draggedExt == toExt) return;
        if (DataContext is not MainWindowViewModel vm) return;

        vm.MovePriorityExtension(_draggedExt, toExt);
        // _draggedExt stays the same — it keeps tracking the extension we grabbed
    }

    private void PriorityExt_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _draggedExt = null;
    }
}
