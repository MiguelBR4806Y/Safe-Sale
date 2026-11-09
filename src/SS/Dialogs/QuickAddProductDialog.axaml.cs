using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS.Data;
using SS.Models;
using SS.Services;

namespace SS.Dialogs;

public partial class QuickAddProductDialog : Window
{
    private readonly string _barcode;
    private readonly ProductRepository _productRepo;
    private readonly CategoryRepository _categoryRepo;
    private List<Category> _categories = new();

    public Product? CreatedProduct { get; private set; }

    public QuickAddProductDialog() : this("", DatabaseConfig.ConnectionString) { }

    public QuickAddProductDialog(string barcode, string connectionString)
    {
        InitializeComponent();
        _barcode = barcode;
        _productRepo = new ProductRepository(connectionString);
        _categoryRepo = new CategoryRepository(connectionString);

        BarcodeBox.Text = barcode;
        BarcodeBox.TextChanged += OnBarcodeTextChanged;
        NameBox.TextChanged += OnNameTextChanged;
        PriceBox.TextChanged += OnPriceTextChanged;
        StockBox.TextChanged += (_, _) => HideError("Stock");
        MinStockBox.TextChanged += (_, _) => HideError("MinStock");
        CategoryCombo.SelectionChanged += (_, _) => HideError("Category");

        SaveButton.Click += OnSave;
        CancelButton.Click += OnCancel;

        LoadCategories();
    }

    private void LoadCategories()
    {
        _categories = _categoryRepo.GetAll().ToList();
        CategoryCombo.ItemsSource = _categories;
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
        var (isValid, errorMsg, field) = ValidateInputs(out var price, out var stock, out var minStock);

        if (!isValid)
        {
            ShowFieldError(field, errorMsg);
            return;
        }

        var selectedCategory = CategoryCombo.SelectedItem as Category;

        var product = new Product
        {
            Name = NameBox.Text!.Trim(),
            Barcode = BarcodeBox.Text!.Trim(),
            Price = price,
            Stock = stock,
            MinStock = minStock,
            CategoryId = selectedCategory?.Id ?? 0
        };

        try
        {
            _productRepo.Add(product);
            CreatedProduct = product;
            Close(true);
        }
        catch (Exception ex) when (DbErrors.IsDuplicateKey(ex))
        {
            ShowGlobalError($"Ya existe un producto con el código {product.Barcode}");
        }
        catch (Exception)
        {
            ShowGlobalError("Error al guardar el producto");
        }
    }

    private (bool isValid, string errorMsg, string field) ValidateInputs(
        out decimal price, out int stock, out int minStock)
    {
        price = 0m;
        stock = 0;
        minStock = 0;

        var barcode = BarcodeBox.Text?.Trim() ?? "";
        var name = NameBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(barcode))
            return (false, "El código de barras es obligatorio", "Barcode");

        if (string.IsNullOrEmpty(name))
            return (false, "El nombre del producto es obligatorio", "Name");

        if (name.Length < 3)
            return (false, "El nombre debe tener mínimo 3 caracteres", "Name");

        if (!InputValidator.TryParsePrice(PriceBox.Text, out price, out var priceError))
            return (false, priceError, "Price");

        if (CategoryCombo.SelectedItem == null)
            return (false, "Debe seleccionar una categoría", "Category");

        if (!InputValidator.TryParseStock(StockBox.Text, out stock, out var stockError))
            return (false, stockError, "Stock");

        if (!InputValidator.TryParseMinStock(MinStockBox.Text, out minStock, out var minStockError))
            return (false, minStockError, "MinStock");

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
            case "Category":
                CategoryError.Text = message;
                CategoryError.IsVisible = true;
                break;
            case "Stock":
                StockError.Text = message;
                StockError.IsVisible = true;
                StockBox.Classes.Add("error");
                break;
            case "MinStock":
                MinStockError.Text = message;
                MinStockError.IsVisible = true;
                MinStockBox.Classes.Add("error");
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
            case "Category":
                CategoryError.IsVisible = false;
                break;
            case "Stock":
                StockError.IsVisible = false;
                StockBox.Classes.Remove("error");
                break;
            case "MinStock":
                MinStockError.IsVisible = false;
                MinStockBox.Classes.Remove("error");
                break;
        }
        ErrorBorder.IsVisible = false;
    }

    private void HideAllErrors()
    {
        BarcodeError.IsVisible = false;
        NameError.IsVisible = false;
        PriceError.IsVisible = false;
        CategoryError.IsVisible = false;
        StockError.IsVisible = false;
        MinStockError.IsVisible = false;
        ErrorBorder.IsVisible = false;
        BarcodeBox.Classes.Remove("error");
        NameBox.Classes.Remove("error");
        PriceBox.Classes.Remove("error");
        StockBox.Classes.Remove("error");
        MinStockBox.Classes.Remove("error");
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
