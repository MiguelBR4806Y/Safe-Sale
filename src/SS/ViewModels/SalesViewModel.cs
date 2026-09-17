using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SS.Data;
using SS.Models;
using SS.Services;

namespace SS.ViewModels;

public partial class SalesViewModel : ViewModelBase, IDisposable
{
    private readonly SqliteProductRepository _productRepo;
    private readonly SqliteSaleRepository _saleRepo;
    private readonly User _currentUser;
    private readonly ICameraService _cameraService;
    private readonly IBarcodeScannerService _barcodeScanner;
    private string _lastScannedCode = "";
    private DateTime _lastScanTime = DateTime.MinValue;
    private readonly string _dbPath;

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

    [ObservableProperty]
    private bool _isCameraActive;

    [ObservableProperty]
    private string _scanFeedback = "";

    [ObservableProperty]
    private string _scanFeedbackColor = "#4A148C";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _cameraPreview;

    public ICommand AddByBarcodeCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand RemoveFromCartCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand ToggleCameraCommand { get; }
    public ICommand OpenQuickAddDialogCommand { get; }

    public event Action<string>? QuickAddRequested;

    public SalesViewModel(string dbPath, User currentUser)
    {
        _dbPath = dbPath;
        _currentUser = currentUser;
        _productRepo = new SqliteProductRepository(dbPath);
        _saleRepo = new SqliteSaleRepository(dbPath);
        _cameraService = new CameraService();
        _barcodeScanner = new BarcodeScannerService();

        _cameraService.FrameAvailable += OnFrameAvailable;

        AddByBarcodeCommand = new RelayCommand(OnAddByBarcode);
        AddToCartCommand = new RelayCommand(OnAddToCart, () => SelectedProduct != null);
        RemoveFromCartCommand = new RelayCommand<CartItem>(OnRemoveFromCart);
        IncreaseQtyCommand = new RelayCommand<CartItem>(OnIncreaseQty);
        DecreaseQtyCommand = new RelayCommand<CartItem>(OnDecreaseQty);
        CheckoutCommand = new RelayCommand(OnCheckout, () => CartItems.Count > 0);
        ClearCartCommand = new RelayCommand(OnClearCart);
        ToggleCameraCommand = new RelayCommand(OnToggleCamera);
        OpenQuickAddDialogCommand = new RelayCommand(() => QuickAddRequested?.Invoke(_lastScannedCode));

        LoadProducts();
    }

    private void OnFrameAvailable(byte[] frameData)
    {
        if (!IsCameraActive || IsScanning) return;

        IsScanning = true;

        try
        {
            var code = _barcodeScanner.ScanFrame(frameData, 1280, 720);

            if (!string.IsNullOrEmpty(code) && code != _lastScannedCode)
            {
                _lastScannedCode = code;
                _lastScanTime = DateTime.Now;
                BarcodeInput = code;

                var product = AvailableProducts.FirstOrDefault(p => p.Barcode == code);
                if (product != null)
                {
                    AddProductToCart(product);
                    ScanFeedback = $"\u2713 {product.Name} agregado";
                    ScanFeedbackColor = "#4CAF50";
                }
                else
                {
                    ScanFeedback = $"C\u00f3digo no encontrado: {code}";
                    ScanFeedbackColor = "#EF5350";
                    QuickAddRequested?.Invoke(code);
                }
            }
            else if (!string.IsNullOrEmpty(code) && code == _lastScannedCode)
            {
                if ((DateTime.Now - _lastScanTime).TotalSeconds > 3)
                {
                    _lastScannedCode = "";
                }
            }

            UpdateCameraPreview(frameData);
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void UpdateCameraPreview(byte[] jpegBytes)
    {
        try
        {
            using var stream = new MemoryStream(jpegBytes);
            var bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
            var old = CameraPreview;
            CameraPreview = bitmap;
            old?.Dispose();
        }
        catch
        {
            // Silently ignore frame decode errors
        }
    }

    private void OnToggleCamera()
    {
        if (_cameraService.IsCapturing)
        {
            _cameraService.StopCapture();
            IsCameraActive = false;
            ScanFeedback = "";
            CameraPreview?.Dispose();
            CameraPreview = null;
        }
        else
        {
            Task.Run(async () =>
            {
                await _cameraService.StartCaptureAsync();
                IsCameraActive = _cameraService.IsCapturing;

                if (!IsCameraActive)
                {
                    ScanFeedback = "No se pudo acceder a la c\u00e1mara";
                    ScanFeedbackColor = "#EF5350";
                }
            });
        }
    }

    public void HandleQuickAddResult(bool success)
    {
        if (success)
        {
            LoadProducts();

            var lastProduct = AvailableProducts.FirstOrDefault(p => p.Barcode == _lastScannedCode);
            if (lastProduct != null)
            {
                AddProductToCart(lastProduct);
                ScanFeedback = $"\u2713 {lastProduct.Name} creado y agregado";
                ScanFeedbackColor = "#4CAF50";
            }
        }
    }

    private void LoadProducts()
    {
        AvailableProducts = _productRepo.GetAll();
    }

    private void OnAddByBarcode()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput))
        {
            ScanFeedback = "Ingrese un código de barras";
            ScanFeedbackColor = "#FF9800";
            return;
        }

        if (!IsValidBarcode(BarcodeInput))
        {
            ScanFeedback = "Código inválido. Use EAN-13 (13 dígitos) o UPC-A (12 dígitos)";
            ScanFeedbackColor = "#EF5350";
            return;
        }

        var product = AvailableProducts.FirstOrDefault(p => p.Barcode == BarcodeInput);
        if (product == null)
        {
            ScanFeedback = $"Producto no encontrado: {BarcodeInput}";
            ScanFeedbackColor = "#EF5350";
            QuickAddRequested?.Invoke(BarcodeInput);
            return;
        }

        AddProductToCart(product);
        BarcodeInput = "";
        ScanFeedback = $"\u2713 {product.Name} agregado";
        ScanFeedbackColor = "#4CAF50";
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
        ScanFeedback = $"\u2713 Venta #{saleId} completada";
        ScanFeedbackColor = "#4CAF50";
        OnClearCart();
        LoadProducts();
    }

    private bool IsValidEAN13(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length != 13)
            return false;

        if (!barcode.All(char.IsDigit))
            return false;

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = int.Parse(barcode[i].ToString());
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        return int.Parse(barcode[12].ToString()) == checkDigit;
    }

    private bool IsValidUPCA(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length != 12)
            return false;

        if (!barcode.All(char.IsDigit))
            return false;

        int sum = 0;
        for (int i = 0; i < 11; i++)
        {
            int digit = int.Parse(barcode[i].ToString());
            sum += (i % 2 == 0) ? digit * 3 : digit;
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        return int.Parse(barcode[11].ToString()) == checkDigit;
    }

    private bool IsValidBarcode(string barcode)
    {
        return IsValidEAN13(barcode) || IsValidUPCA(barcode);
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

    public void Dispose()
    {
        _cameraService.FrameAvailable -= OnFrameAvailable;
        _cameraService.StopCapture();
        _cameraService.Dispose();
        CameraPreview?.Dispose();
        CameraPreview = null;
    }
}
