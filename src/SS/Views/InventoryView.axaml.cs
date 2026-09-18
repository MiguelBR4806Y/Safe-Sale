using Avalonia.Controls;
using SS.Dialogs;
using SS.Models;
using SS.ViewModels;

namespace SS.Views;

public partial class InventoryView : UserControl
{
    private InventoryViewModel? _viewModel;

    public InventoryView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.AddProductRequested -= OnAddProductRequested;
            _viewModel.EditProductRequested -= OnEditProductRequested;
        }

        if (DataContext is InventoryViewModel vm)
        {
            _viewModel = vm;
            _viewModel.AddProductRequested += OnAddProductRequested;
            _viewModel.EditProductRequested += OnEditProductRequested;
        }
    }

    private async void OnAddProductRequested()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new QuickAddProductDialog("", SS.Data.AppDatabase.DbPath);
            var result = await dialog.ShowDialog<bool?>(topLevel as Window ?? throw new System.InvalidOperationException("No parent window"));

            if (result == true && _viewModel != null)
            {
                _viewModel.HandleAddProductResult(true);
            }
        }
        catch
        {
        }
    }

    private async void OnEditProductRequested(Product product)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new EditProductDialog(product, SS.Data.AppDatabase.DbPath);
            var result = await dialog.ShowDialog<bool?>(topLevel as Window ?? throw new System.InvalidOperationException("No parent window"));

            if (result == true && _viewModel != null)
            {
                _viewModel.HandleAddProductResult(true);
            }
        }
        catch
        {
        }
    }
}
