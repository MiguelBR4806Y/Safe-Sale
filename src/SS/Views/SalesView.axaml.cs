using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using SS.Dialogs;
using SS.Models;
using SS.ViewModels;

namespace SS.Views;

public partial class SalesView : UserControl
{
    private SalesViewModel? _viewModel;

    public SalesView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        var barcodeBox = this.FindControl<TextBox>("BarcodeInputBox");
        if (barcodeBox != null)
            barcodeBox.TextChanged += OnBarcodeInputChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_viewModel != null)
            _viewModel.QuickAddRequested -= OnQuickAddRequested;

        if (DataContext is SalesViewModel vm)
        {
            _viewModel = vm;
            _viewModel.QuickAddRequested += OnQuickAddRequested;
        }
    }

    private void OnBarcodeInputChanged(object? sender, TextChangedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb == null) return;

        var digits = new string(tb.Text?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
        if (digits.Length > 13) digits = digits[..13];

        if (tb.Text != digits)
        {
            var caret = tb.CaretIndex;
            tb.Text = digits;
            tb.CaretIndex = Math.Min(caret, digits.Length);
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
        catch { }
    }

    private void OnProductCheckboxPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is Product product && _viewModel != null)
        {
            _viewModel.ToggleProductSelectionCommand.Execute(product);
        }
    }
}
