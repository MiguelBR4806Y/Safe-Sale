using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS.Data;
using SS.Models;
using SS.Services;

namespace SS.Dialogs;

public partial class EditProductDialog : Window
{
    private readonly Product _product;
    private readonly ProductRepository _productRepo;
    private readonly CategoryRepository _categoryRepo;
    private List<Category> _categories = new();

    public bool WasUpdated { get; private set; }

    public EditProductDialog(Product product, string connectionString)
    {
        InitializeComponent();
        _product = product;
        _productRepo = new ProductRepository(connectionString);
        _categoryRepo = new CategoryRepository(connectionString);

        BarcodeBox.Text = product.Barcode;
        NameBox.Text = product.Name;
        PriceBox.Text = product.Price.ToString("0.##", CultureInfo.InvariantCulture);
        StockBox.Text = product.Stock.ToString(CultureInfo.InvariantCulture);
        MinStockBox.Text = product.MinStock.ToString(CultureInfo.InvariantCulture);

        BarcodeBox.TextChanged += (_, _) => HideError("Barcode");
        NameBox.TextChanged += (_, _) => HideError("Name");
        PriceBox.TextChanged += (_, _) => HideError("Price");
        StockBox.TextChanged += (_, _) => HideError("Stock");
        MinStockBox.TextChanged += (_, _) => HideError("MinStock");

        SaveButton.Click += OnSave;
        CancelButton.Click += OnCancel;

        LoadCategories();
    }

    private void LoadCategories()
    {
        _categories = _categoryRepo.GetAll().ToList();
        CategoryCombo.ItemsSource = _categories;

        var matchedCategory = _categories.FirstOrDefault(c => c.Id == _product.CategoryId);
        if (matchedCategory != null)
        {
            CategoryCombo.SelectedItem = matchedCategory;
        }
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var (isValid, errorMsg, field) = ValidateInputs(out var price, out var stock, out var minStock);
        if (!isValid)
        {
            ShowFieldError(field, errorMsg);
            return;
        }

        _product.Barcode = BarcodeBox.Text!.Trim();
        _product.Name = NameBox.Text!.Trim();
        _product.Price = price;
        _product.Stock = stock;
        _product.MinStock = minStock;
        _product.CategoryId = (CategoryCombo.SelectedItem as Category)?.Id ?? 0;

        try
        {
            _productRepo.Update(_product);
            WasUpdated = true;
            Close(true);
        }
        catch (Exception ex) when (DbErrors.IsDuplicateKey(ex))
        {
            ShowGlobalError($"Ya existe otro producto con el c\u00f3digo {_product.Barcode}");
        }
        catch (Exception)
        {
            ShowGlobalError("Error al guardar los cambios");
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
            return (false, "El c\u00f3digo de barras es obligatorio", "Barcode");

        if (string.IsNullOrEmpty(name))
            return (false, "El nombre del producto es obligatorio", "Name");

        if (name.Length < 3)
            return (false, "El nombre debe tener m\u00ednimo 3 caracteres", "Name");

        if (!InputValidator.TryParsePrice(PriceBox.Text, out price, out var priceError))
            return (false, priceError, "Price");

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
