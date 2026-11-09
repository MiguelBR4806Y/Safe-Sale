using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ClearCartRequested -= OnClearCartRequested;
            _viewModel.CheckoutRequested -= OnCheckoutRequested;
        }

        if (DataContext is SalesViewModel vm)
        {
            _viewModel = vm;
            _viewModel.ClearCartRequested += OnClearCartRequested;
            _viewModel.CheckoutRequested += OnCheckoutRequested;
        }
    }

    private Window? GetParentWindow()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel as Window;
    }

    private async void OnClearCartRequested()
    {
        try
        {
            var window = GetParentWindow();
            if (window == null || _viewModel == null) return;

            var message = "¿Está seguro que desea vaciar el carrito?\nEsta acción no se puede deshacer.";
            var confirmDialog = new ConfirmDialog(message);
            var confirmed = await confirmDialog.ShowDialog<bool?>(window);

            if (confirmed == true)
            {
                _viewModel.ConfirmClearCart();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAG] OnClearCartRequested EXCEPTION: {ex}");
        }
    }

    private async void OnCheckoutRequested()
    {
        try
        {
            var window = GetParentWindow();
            if (window == null || _viewModel == null) return;

            var message = $"¿Confirmar cobro de ${_viewModel.CartTotal:F2}?\n" +
                          $"{_viewModel.CartCount} items · {_viewModel.SelectedPaymentMethod}";
            var confirmDialog = new ConfirmDialog(message);
            var confirmed = await confirmDialog.ShowDialog<bool?>(window);

            if (confirmed == true)
            {
                _viewModel.ConfirmCheckout();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAG] OnCheckoutRequested EXCEPTION: {ex}");
        }
    }
}
