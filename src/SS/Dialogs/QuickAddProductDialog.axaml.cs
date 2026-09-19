using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Microsoft.Data.Sqlite;
using SS.Data;
using SS.Models;

namespace SS.Dialogs;

public partial class QuickAddProductDialog : Window
{
    private readonly string _barcode;
    private readonly SqliteProductRepository _productRepo;
    private readonly SqliteCategoryRepository _categoryRepo;
    private List<Category> _categories = new();

    public Product? CreatedProduct { get; private set; }

    public QuickAddProductDialog() : this("", AppDatabase.DbPath) { }

    public QuickAddProductDialog(string barcode, string dbPath)
    {
        InitializeComponent();
        _barcode = barcode;
        _productRepo = new SqliteProductRepository(dbPath);
        _categoryRepo = new SqliteCategoryRepository(dbPath);

        BarcodeBox.Text = barcode;
        BarcodeBox.TextChanged += OnBarcodeTextChanged;
        NameBox.TextChanged += OnNameTextChanged;
        PriceBox.TextChanged += OnPriceTextChanged;

        SaveButton.Click += OnSave;
        CancelButton.Click += OnCancel;

        LoadCategories();
    }

    private void LoadCategories()
    {
        _categories = _categoryRepo.GetAll().ToList();
        CategoryCombo.ItemsSource = _categories;
        CategoryCombo.DisplayMemberBinding = null;
        CategoryCombo.ItemTemplate = new FuncDataTemplate<Category>((item, _) =>
        {
            var sp = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
            var icon = new TextBlock { Text = item.Icon, FontSize = 14, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            var name = new TextBlock { Text = item.Name, FontSize = 13, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            sp.Children.Add(icon);
            sp.Children.Add(name);
            return sp;
        }, true);
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
        var stock = int.TryParse(StockBox.Text?.Trim(), out var s) ? s : 0;
        var minStock = int.TryParse(MinStockBox.Text?.Trim(), out var ms) ? ms : 5;
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
        catch (SqliteException ex) when (ex.ErrorCode == 19)
        {
            ShowGlobalError($"Ya existe un producto con el código {product.Barcode}");
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
            return (false, "El código de barras es obligatorio", "Barcode");

        if (string.IsNullOrEmpty(name))
            return (false, "El nombre del producto es obligatorio", "Name");

        if (name.Length < 3)
            return (false, "El nombre debe tener mínimo 3 caracteres", "Name");

        if (string.IsNullOrEmpty(priceText))
            return (false, "El precio es obligatorio", "Price");

        if (!decimal.TryParse(priceText, out decimal price) || price < 0)
            return (false, "El precio debe ser un número válido (ej: 10.50)", "Price");

        if (CategoryCombo.SelectedItem == null)
            return (false, "Debe seleccionar una categoría", "Category");

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
        }
        ErrorBorder.IsVisible = false;
    }

    private void HideAllErrors()
    {
        BarcodeError.IsVisible = false;
        NameError.IsVisible = false;
        PriceError.IsVisible = false;
        CategoryError.IsVisible = false;
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
