using Avalonia.Controls;
using SS.Dialogs;
using SS.ViewModels;

namespace SS.Views;

public partial class SalesView : UserControl
{
    private SalesViewModel? _viewModel;

    public SalesView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.QuickAddRequested -= OnQuickAddRequested;
        }

        if (DataContext is SalesViewModel vm)
        {
            _viewModel = vm;
            _viewModel.QuickAddRequested += OnQuickAddRequested;
        }
    }

    private async void OnQuickAddRequested(string barcode)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new QuickAddProductDialog(barcode, SS.Data.AppDatabase.DbPath);

            var result = await dialog.ShowDialog<bool?>(topLevel as Window);

            if (result == true && _viewModel != null)
            {
                _viewModel.HandleQuickAddResult(true);
            }
        }
        catch
        {
            // Silently handle dialog errors
        }
    }
}
