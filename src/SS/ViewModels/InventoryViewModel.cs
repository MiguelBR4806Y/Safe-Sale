using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SS.Models;

namespace SS.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    // Products
    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    // Selected product
    [ObservableProperty]
    private Product? _selectedProduct;

    // Search
    [ObservableProperty]
    private string _searchText = "";

    // Commands
    public ICommand AddProductCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand LoadByBarcodeCommand { get; }

    // Stats
    [ObservableProperty]
    private int _totalProducts;
    [ObservableProperty]
    private int _lowStock;
    [ObservableProperty]
    private int _categoriesCount;
    [ObservableProperty]
    private decimal _valueTotal;

    public InventoryViewModel()
    {
        // Comandos
        AddProductCommand = new RelayCommand(OnAddProduct);
        EditProductCommand = new RelayCommand(OnEditProduct, () => SelectedProduct != null);
        DeleteProductCommand = new RelayCommand(OnDeleteProduct, () => SelectedProduct != null);
        SearchCommand = new RelayCommand(OnSearch);
        LoadByBarcodeCommand = new RelayCommand(OnLoadByBarcode);

        // Cargar productos (usando repositorio estático o en memoria)
        LoadProducts();

        // Actualizar stats
        UpdateStats();
    }

    private void LoadProducts()
    {
        // Productos de ejemplo para demostración
        Products = new ObservableCollection<Product>
        {
            new Product { Id = 1, Name = "Leche", Barcode = "7501234567890", Price = 15.99m, Stock = 50, MinStock = 10 },
            new Product { Id = 2, Name="Pan", Barcode="7501234567891", Price = 25.50m, Stock = 30, MinStock = 5 },
            new Product { Id = 3, Name="Huevos", Barcode="7501234567892", Price = 32.00m, Stock = 15, MinStock = 5 }
        };
    }

    private void UpdateStats()
    {
        TotalProducts = Products.Count;
        LowStock = Products.Count(p => p.Stock <= p.MinStock);
        CategoriesCount = 5; // Valor por defecto
        ValueTotal = Products.Sum(p => p.Price * p.Stock);
    }

    private void OnSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            LoadProducts();
            return;
        }

        var filtered = Products.Where(p =>
            p.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
            p.Barcode.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0
        ).ToList();

        Products = new ObservableCollection<Product>(filtered);
    }

    private void OnAddProduct()
    {
        // TODO: Implementar
    }

    private void OnEditProduct()
    {
        if (SelectedProduct == null) return;
        // TODO: Implementar
    }

    private void OnDeleteProduct()
    {
        if (SelectedProduct == null) return;
        // TODO: Implementar
        Products.Remove(SelectedProduct);
        UpdateStats();
    }

    private void OnLoadByBarcode()
    {
        // TODO: Implementar escaneo de código de barras
    }
}