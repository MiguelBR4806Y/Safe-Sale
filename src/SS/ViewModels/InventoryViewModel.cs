using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SS.Data;
using SS.Models;

namespace SS.ViewModels;

public partial class InventoryViewModel : ViewModelBase
{
    private readonly SqliteProductRepository _productRepo;
    private readonly SqliteCategoryRepository _categoryRepo;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private int _totalProducts;

    [ObservableProperty]
    private int _lowStock;

    [ObservableProperty]
    private int _categoriesCount;

    [ObservableProperty]
    private decimal _valueTotal;

    public ICommand AddProductCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand LoadByBarcodeCommand { get; }
    public ICommand RefreshCommand { get; }

    public InventoryViewModel(string dbPath)
    {
        _productRepo = new SqliteProductRepository(dbPath);
        _categoryRepo = new SqliteCategoryRepository(dbPath);

        AddProductCommand = new RelayCommand(OnAddProduct);
        EditProductCommand = new RelayCommand(OnEditProduct, () => SelectedProduct != null);
        DeleteProductCommand = new RelayCommand(OnDeleteProduct, () => SelectedProduct != null);
        SearchCommand = new RelayCommand(OnSearch);
        LoadByBarcodeCommand = new RelayCommand(OnLoadByBarcode);
        RefreshCommand = new RelayCommand(LoadData);

        LoadData();
    }

    public void LoadData()
    {
        Products = _productRepo.GetAll();
        CategoriesCount = _categoryRepo.GetAll().Count;
        UpdateStats();
    }

    private void UpdateStats()
    {
        TotalProducts = Products.Count;
        LowStock = Products.Count(p => p.Stock <= p.MinStock);
        ValueTotal = Products.Sum(p => p.Price * p.Stock);
    }

    private void OnSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            LoadData();
            return;
        }

        var filtered = Products.Where(p =>
            p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            p.Barcode.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        Products = new ObservableCollection<Product>(filtered);
        UpdateStats();
    }

    private void OnAddProduct()
    {
        var product = new Product
        {
            Name = "Nuevo Producto",
            Barcode = "",
            Price = 0,
            Stock = 0,
            MinStock = 5
        };
        _productRepo.Add(product);
        LoadData();
    }

    private void OnEditProduct()
    {
        if (SelectedProduct == null) return;
        _productRepo.Update(SelectedProduct);
        LoadData();
    }

    private void OnDeleteProduct()
    {
        if (SelectedProduct == null) return;
        _productRepo.Delete(SelectedProduct.Id);
        SelectedProduct = null;
        LoadData();
    }

    private void OnLoadByBarcode()
    {
        OnSearch();
    }
}
