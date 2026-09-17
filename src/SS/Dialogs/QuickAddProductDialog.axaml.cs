using System;
using System.Linq;
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
        BarcodeBox.TextChanged += OnBarcodeTextChanged;
        NameBox.TextChanged += OnNameTextChanged;
        PriceBox.TextChanged += OnPriceTextChanged;

        SaveButton.Click += OnSave;
        CancelButton.Click += OnCancel;
    }

    private void OnBarcodeTextChanged(object? sender, TextChangedEventArgs e)
    {
        var tb = (TextBox)sender!;
        var digits = new string(tb.Text?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());

        if (digits.Length > 13)
            digits = digits[..13];

        if (tb.Text != digits)
        {
            var caret = tb.CaretIndex;
            tb.Text = digits;
            tb.CaretIndex = Math.Min(caret, digits.Length);
        }

        HideError("Barcode");
    }

    private void OnNameTextChanged(object? sender, TextChangedEventArgs e)
    {
        HideError("Name");
    }

    private void OnPriceTextChanged(object? sender, TextChangedEventArgs e)
    {
        HideError("Price");
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var (isValid, errorMsg, field) = ValidateInputs();

        if (!isValid)
        {
            ShowFieldError(field, errorMsg);
            return;
        }

        var price = decimal.Parse(PriceBox.Text!.Trim());
        var product = new Product
        {
            Name = NameBox.Text!.Trim(),
            Barcode = BarcodeBox.Text!.Trim(),
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
            ShowGlobalError($"Ya existe un producto con el c\u00f3digo {product.Barcode}");
        }
        catch (Exception)
        {
            ShowGlobalError("Error al guardar el producto");
        }
    }

    private (bool isValid, string errorMsg, string field) ValidateInputs()
    {
        var barcode = BarcodeBox.Text?.Trim() ?? "";
        var name = NameBox.Text?.Trim() ?? "";
        var priceText = PriceBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(barcode))
            return (false, "El c\u00f3digo de barras es obligatorio", "Barcode");

        if (!barcode.All(char.IsDigit))
            return (false, "El c\u00f3digo debe contener solo n\u00fameros", "Barcode");

        if (barcode.Length != 12 && barcode.Length != 13)
            return (false, "El c\u00f3digo debe tener 12 o 13 d\u00edgitos", "Barcode");

        if (string.IsNullOrEmpty(name))
            return (false, "El nombre del producto es obligatorio", "Name");

        if (name.Length < 3)
            return (false, "El nombre debe tener m\u00ednimo 3 caracteres", "Name");

        if (string.IsNullOrEmpty(priceText))
            return (false, "El precio es obligatorio", "Price");

        if (!decimal.TryParse(priceText, out decimal price) || price < 0)
            return (false, "El precio debe ser un n\u00famero v\u00e1lido (ej: 10.50)", "Price");

        return (true, "", "");
    }

    private void ShowFieldError(string field, string message)
    {
        HideAllErrors();

        switch (field)
        {
            case "Barcode":
                BarcodeError.Text = message;
                BarcodeError.IsVisible = true;
                BarcodeBox.Classes.Add("error");
                break;
            case "Name":
                NameError.Text = message;
                NameError.IsVisible = true;
                NameBox.Classes.Add("error");
                break;
            case "Price":
                PriceError.Text = message;
                PriceError.IsVisible = true;
                PriceBox.Classes.Add("error");
                break;
        }
    }

    private void ShowGlobalError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.IsVisible = true;
    }

    private void HideError(string field)
    {
        switch (field)
        {
            case "Barcode":
                BarcodeError.IsVisible = false;
                BarcodeBox.Classes.Remove("error");
                break;
            case "Name":
                NameError.IsVisible = false;
                NameBox.Classes.Remove("error");
                break;
            case "Price":
                PriceError.IsVisible = false;
                PriceBox.Classes.Remove("error");
                break;
        }
        ErrorBorder.IsVisible = false;
    }

    private void HideAllErrors()
    {
        BarcodeError.IsVisible = false;
        NameError.IsVisible = false;
        PriceError.IsVisible = false;
        ErrorBorder.IsVisible = false;
        BarcodeBox.Classes.Remove("error");
        NameBox.Classes.Remove("error");
        PriceBox.Classes.Remove("error");
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
