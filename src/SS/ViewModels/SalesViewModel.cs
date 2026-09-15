using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SS.Data;
using SS.Models;

namespace SS.ViewModels;

public partial class SalesViewModel : ViewModelBase
{
    private readonly SqliteProductRepository _productRepo;
    private readonly SqliteSaleRepository _saleRepo;
    private readonly User _currentUser;

    [ObservableProperty]
    private ObservableCollection<CartItem> _cartItems = new();

    [ObservableProperty]
    private decimal _cartTotal;

    [ObservableProperty]
    private int _cartCount;

    [ObservableProperty]
    private string _barcodeInput = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private ObservableCollection<Product> _availableProducts = new();

    public ICommand AddByBarcodeCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand RemoveFromCartCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ClearCartCommand { get; }

    public SalesViewModel(string dbPath, User currentUser)
    {
        _currentUser = currentUser;
        _productRepo = new SqliteProductRepository(dbPath);
        _saleRepo = new SqliteSaleRepository(dbPath);

        AddByBarcodeCommand = new RelayCommand(OnAddByBarcode);
        AddToCartCommand = new RelayCommand(OnAddToCart, () => SelectedProduct != null);
        RemoveFromCartCommand = new RelayCommand<CartItem>(OnRemoveFromCart);
        IncreaseQtyCommand = new RelayCommand<CartItem>(OnIncreaseQty);
        DecreaseQtyCommand = new RelayCommand<CartItem>(OnDecreaseQty);
        CheckoutCommand = new RelayCommand(OnCheckout, () => CartItems.Count > 0);
        ClearCartCommand = new RelayCommand(OnClearCart);

        LoadProducts();
    }

    private void LoadProducts()
    {
        AvailableProducts = _productRepo.GetAll();
    }

    private void OnAddByBarcode()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var product = AvailableProducts.FirstOrDefault(p => p.Barcode == BarcodeInput);
        if (product == null)
        {
            StatusMessage = "Producto no encontrado";
            return;
        }

        AddProductToCart(product);
        BarcodeInput = "";
    }

    private void OnAddToCart()
    {
        if (SelectedProduct == null) return;
        AddProductToCart(SelectedProduct);
    }

    private void AddProductToCart(Product product)
    {
        var existing = CartItems.FirstOrDefault(ci => ci.Product.Id == product.Id);
        if (existing != null)
        {
            existing.Quantity += 1;
        }
        else
        {
            CartItems.Add(new CartItem { Product = product, Quantity = 1 });
        }
        UpdateCartTotals();
        StatusMessage = $"{product.Name} agregado al carrito";
    }

    private void OnRemoveFromCart(CartItem? item)
    {
        if (item == null) return;
        CartItems.Remove(item);
        UpdateCartTotals();
    }

    private void OnIncreaseQty(CartItem? item)
    {
        if (item == null) return;
        item.Quantity++;
        UpdateCartTotals();
    }

    private void OnDecreaseQty(CartItem? item)
    {
        if (item == null) return;
        if (item.Quantity > 1)
        {
            item.Quantity--;
            UpdateCartTotals();
        }
    }

    private void OnCheckout()
    {
        if (CartItems.Count == 0) return;

        var sale = new Sale
        {
            UserId = _currentUser.Id,
            Total = CartTotal,
            CreatedAt = DateTime.Now
        };

        int saleId = _saleRepo.Add(sale);

        foreach (var ci in CartItems)
        {
            _saleRepo.AddItem(new SaleItem
            {
                SaleId = saleId,
                ProductId = ci.Product.Id,
                Quantity = ci.Quantity,
                PriceSold = ci.Price
            });

            ci.Product.Stock -= ci.Quantity;
            _productRepo.Update(ci.Product);
        }

        StatusMessage = $"Venta #{saleId} completada - Total: ${CartTotal:F2}";
        OnClearCart();
        LoadProducts();
    }

    private void OnClearCart()
    {
        CartItems.Clear();
        UpdateCartTotals();
        StatusMessage = "";
    }

    private void UpdateCartTotals()
    {
        CartTotal = CartItems.Sum(ci => ci.Subtotal);
        CartCount = CartItems.Sum(ci => ci.Quantity);
    }
}
