using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Data.Sqlite;
using SS.Data;
using SS.Models;

namespace SS.Dialogs;

public partial class EditProductDialog : Window
{
    private readonly Product _product;
    private readonly SqliteProductRepository _productRepo;
    private readonly SqliteCategoryRepository _categoryRepo;
    private List<Category> _categories = new();

    public bool WasUpdated { get; private set; }

    public EditProductDialog(Product product, string dbPath)
    {
        InitializeComponent();
        _product = product;
        _productRepo = new SqliteProductRepository(dbPath);
        _categoryRepo = new SqliteCategoryRepository(dbPath);

        BarcodeBox.Text = product.Barcode;
        NameBox.Text = product.Name;
        PriceBox.Text = product.Price.ToString("0.##");
        StockBox.Text = product.Stock.ToString();
        MinStockBox.Text = product.MinStock.ToString();

        BarcodeBox.TextChanged += (_, _) => HideError("Barcode");
        NameBox.TextChanged += (_, _) => HideError("Name");
        PriceBox.TextChanged += (_, _) => HideError("Price");
        StockBox.TextChanged += (_, _) => HideError("Stock");

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
        var (isValid, errorMsg, field) = ValidateInputs();
        if (!isValid)
        {
            ShowFieldError(field, errorMsg);
            return;
        }

        _product.Barcode = BarcodeBox.Text!.Trim();
        _product.Name = NameBox.Text!.Trim();
        _product.Price = decimal.Parse(PriceBox.Text!.Trim());
        _product.Stock = int.TryParse(StockBox.Text?.Trim(), out var s) ? s : 0;
        _product.MinStock = int.TryParse(MinStockBox.Text?.Trim(), out var ms) ? ms : 5;
        _product.CategoryId = (CategoryCombo.SelectedItem as Category)?.Id ?? 0;

        try
        {
            _productRepo.Update(_product);
            WasUpdated = true;
            Close(true);
        }
        catch (SqliteException ex) when (ex.ErrorCode == 19)
        {
            ShowGlobalError($"Ya existe otro producto con el c\u00f3digo {_product.Barcode}");
        }
        catch (Exception)
        {
            ShowGlobalError("Error al guardar los cambios");
        }
    }

    private (bool isValid, string errorMsg, string field) ValidateInputs()
    {
        var barcode = BarcodeBox.Text?.Trim() ?? "";
        var name = NameBox.Text?.Trim() ?? "";
        var priceText = PriceBox.Text?.Trim() ?? "";
        var stockText = StockBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(barcode))
            return (false, "El c\u00f3digo de barras es obligatorio", "Barcode");

        if (string.IsNullOrEmpty(name))
            return (false, "El nombre del producto es obligatorio", "Name");

        if (name.Length < 3)
            return (false, "El nombre debe tener m\u00ednimo 3 caracteres", "Name");

        if (string.IsNullOrEmpty(priceText))
            return (false, "El precio es obligatorio", "Price");

        if (!decimal.TryParse(priceText, out decimal price) || price < 0)
            return (false, "El precio debe ser un n\u00famero v\u00e1lido", "Price");

        if (!string.IsNullOrEmpty(stockText) && (!int.TryParse(stockText, out int stock) || stock < 0))
            return (false, "El stock debe ser un n\u00famero entero positivo", "Stock");

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
        }
        ErrorBorder.IsVisible = false;
    }

    private void HideAllErrors()
    {
        BarcodeError.IsVisible = false;
        NameError.IsVisible = false;
        PriceError.IsVisible = false;
        StockError.IsVisible = false;
        ErrorBorder.IsVisible = false;
        BarcodeBox.Classes.Remove("error");
        NameBox.Classes.Remove("error");
        PriceBox.Classes.Remove("error");
        StockBox.Classes.Remove("error");
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
