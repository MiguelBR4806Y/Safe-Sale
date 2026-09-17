using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Data.Sqlite;
using SS.Data;
using SS.Models;

namespace SS.Dialogs;

public partial class QuickAddProductDialog : Window
{
    private readonly string _barcode;
    private readonly SqliteProductRepository _productRepo;

    public Product? CreatedProduct { get; private set; }

    public QuickAddProductDialog() : this("", AppDatabase.DbPath) { }

    public QuickAddProductDialog(string barcode, string dbPath)
    {
        InitializeComponent();
        _barcode = barcode;
        _productRepo = new SqliteProductRepository(dbPath);

        BarcodeBox.Text = barcode;

        SaveButton.Click += OnSave;
        CancelButton.Click += OnCancel;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ShowError("El nombre del producto es obligatorio");
            return;
        }

        if (!decimal.TryParse(PriceBox.Text, out decimal price) || price < 0)
            price = 0;

        var product = new Product
        {
            Name = NameBox.Text.Trim(),
            Barcode = _barcode,
            Price = price,
            Stock = 0,
            MinStock = 5
        };

        try
        {
            _productRepo.Add(product);
            CreatedProduct = product;
            Close(true);
        }
        catch (SqliteException ex) when (ex.ErrorCode == 19)
        {
            ShowError($"Ya existe un producto con el código {_barcode}");
        }
        catch (Exception)
        {
            ShowError("Error al guardar el producto");
        }
    }

    private void ShowError(string message)
    {
        var errorBorder = this.FindControl<Border>("ErrorBorder");
        var errorText = this.FindControl<TextBlock>("ErrorText");

        if (errorBorder != null && errorText != null)
        {
            errorText.Text = message;
            errorBorder.IsVisible = true;
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
