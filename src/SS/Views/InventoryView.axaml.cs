using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
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
            _viewModel.DeleteMultipleRequested -= OnDeleteMultipleRequested;
            _viewModel.AddMultipleToCartRequested -= OnAddMultipleToCartRequested;
            _viewModel.ClearInventoryRequested -= OnClearInventoryRequested;
        }

        if (DataContext is InventoryViewModel vm)
        {
            _viewModel = vm;
            _viewModel.AddProductRequested += OnAddProductRequested;
            _viewModel.EditProductRequested += OnEditProductRequested;
            _viewModel.DeleteMultipleRequested += OnDeleteMultipleRequested;
            _viewModel.AddMultipleToCartRequested += OnAddMultipleToCartRequested;
            _viewModel.ClearInventoryRequested += OnClearInventoryRequested;
        }
    }

    private Window? GetParentWindow()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel as Window;
    }

    private async void OnAddProductRequested()
    {
        try
        {
            var window = GetParentWindow();
            if (window == null)
            {
                Console.WriteLine("[DIAG] OnAddProductRequested: parent window is null");
                return;
            }

            var dialog = new QuickAddProductDialog("", SS.Data.AppDatabase.DbPath);
            var result = await dialog.ShowDialog<bool?>(window);

            if (result == true && _viewModel != null)
            {
                _viewModel.HandleAddProductResult(true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAG] OnAddProductRequested EXCEPTION: {ex}");
        }
    }

    private async void OnEditProductRequested(Product product)
    {
        try
        {
            var window = GetParentWindow();
            if (window == null)
            {
                Console.WriteLine("[DIAG] OnEditProductRequested: parent window is null");
                return;
            }

            var dialog = new EditProductDialog(product, SS.Data.AppDatabase.DbPath);
            var result = await dialog.ShowDialog<bool?>(window);

            if (result == true && _viewModel != null)
            {
                _viewModel.HandleAddProductResult(true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAG] OnEditProductRequested EXCEPTION: {ex}");
        }
    }

    private async void OnDeleteMultipleRequested(List<int> productIds)
    {
        Console.WriteLine($"[DIAG] OnDeleteMultipleRequested called with {productIds.Count} products");
        try
        {
            var window = GetParentWindow();
            if (window == null)
            {
                Console.WriteLine("[DIAG] OnDeleteMultipleRequested: parent window is null, aborting");
                return;
            }

            var count = productIds.Count;
            var message = count == 1
                ? "¿Eliminar 1 producto del inventario?\nEsta acción no se puede deshacer."
                : $"¿Eliminar {count} productos del inventario?\nEsta acción no se puede deshacer.";

            Console.WriteLine("[DIAG] OnDeleteMultipleRequested: showing ConfirmDialog");
            var confirmDialog = new ConfirmDialog(message);
            var confirmed = await confirmDialog.ShowDialog<bool?>(window);
            Console.WriteLine($"[DIAG] OnDeleteMultipleRequested: dialog result = {confirmed}");

            if (confirmed == true && _viewModel != null)
            {
                Console.WriteLine("[DIAG] OnDeleteMultipleRequested: calling ConfirmDeleteProducts");
                _viewModel.ConfirmDeleteProducts(productIds);
            }
            else
            {
                Console.WriteLine("[DIAG] OnDeleteMultipleRequested: cancelled or viewModel null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAG] OnDeleteMultipleRequested EXCEPTION: {ex}");
        }
    }

    private void OnAddMultipleToCartRequested(List<int> productIds)
    {
        Console.WriteLine($"[DIAG] OnAddMultipleToCartRequested called with {productIds.Count} products");
        try
        {
            if (_viewModel != null)
            {
                _viewModel.ConfirmAddMultipleToCart(productIds);
                Console.WriteLine("[DIAG] OnAddMultipleToCartRequested: ConfirmAddMultipleToCart completed");
            }
            else
            {
                Console.WriteLine("[DIAG] OnAddMultipleToCartRequested: viewModel is null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAG] OnAddMultipleToCartRequested EXCEPTION: {ex}");
        }
    }

    private async void OnClearInventoryRequested()
    {
        try
        {
            var window = GetParentWindow();
            if (window == null) return;

            var message = "¿Está seguro que desea vaciar todo el inventario?\nEsta acción no se puede deshacer.";
            var confirmDialog = new ConfirmDialog(message);
            var confirmed = await confirmDialog.ShowDialog<bool?>(window);

            if (confirmed == true && _viewModel != null)
            {
                _viewModel.ConfirmClearInventory();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAG] OnClearInventoryRequested EXCEPTION: {ex}");
        }
    }

    private void OnCategoryHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Visual visual && visual.DataContext is CategoryGroup group)
        {
            group.IsExpanded = !group.IsExpanded;
        }
    }
}
