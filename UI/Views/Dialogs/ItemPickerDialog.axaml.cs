using Avalonia.Controls;
using Avalonia.Interactivity;
using NMSE.Data;
using NMSE.UI.ViewModels.Dialogs;

namespace NMSE.UI.Views.Dialogs;

public partial class ItemPickerDialog : Window
{
    private readonly ItemPickerViewModel _viewModel;

    public string? SelectedItemId => _viewModel.HasResult ? _viewModel.ResultItemId : null;

    public ItemPickerDialog()
    {
        InitializeComponent();
        _viewModel = new ItemPickerViewModel();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ItemPickerViewModel.HasResult) && _viewModel.HasResult)
                Close(_viewModel.ResultItemId);
        };
    }

    public void Initialize(GameItemDatabase database, IconManager? iconManager,
        string? filterCategory = null, string? filterType = null)
    {
        _viewModel.Initialize(database, iconManager, filterCategory, filterType);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
