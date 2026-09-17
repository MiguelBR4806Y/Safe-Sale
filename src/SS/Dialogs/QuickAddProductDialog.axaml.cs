using Avalonia.Controls;
using Avalonia.Interactivity;
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
        if (string.IsNullOrWhiteSpace(NameBox.Text)) return;

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

        _productRepo.Add(product);
        CreatedProduct = product;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
