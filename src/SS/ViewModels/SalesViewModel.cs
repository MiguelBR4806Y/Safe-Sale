using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
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
    private readonly MobileScannerService _mobileScanner;
    private string _lastScannedCode = "";
    private DateTime _lastScanTime = DateTime.MinValue;

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
    private bool _isCameraActive;

    [ObservableProperty]
    private string _scanFeedback = "";

    [ObservableProperty]
    private string _scanFeedbackColor = "#4A148C";

    [ObservableProperty]
    private string _selectedPaymentMethod = "Efectivo";

    public string[] PaymentMethods { get; } = new[] { "Efectivo", "Tarjeta", "Transferencia" };

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _cameraPreview;

    [ObservableProperty]
    private bool _isMobileScannerActive;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _mobileQrBitmap;

    [ObservableProperty]
    private string _mobileUrl = "";

    [ObservableProperty]
    private string _mobileScanFeedback = "";

    [ObservableProperty]
    private string _mobileScanFeedbackColor = "#4A148C";

    public event Action<bool>? ScannerToggled;

    public ICommand AddByBarcodeCommand { get; }
    public ICommand RemoveFromCartCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand ToggleCameraCommand { get; }
    public ICommand ToggleMobileScannerCommand { get; }

    public SalesViewModel(string dbPath, User currentUser, MobileScannerService mobileScanner)
    {
        _currentUser = currentUser;
        _productRepo = new SqliteProductRepository(dbPath);
        _saleRepo = new SqliteSaleRepository(dbPath);
        _cameraService = new CameraService();
        _barcodeScanner = new BarcodeScannerService();
        _mobileScanner = mobileScanner;

        _cameraService.FrameAvailable += OnFrameAvailable;
        _mobileScanner.BarcodeReceived += OnMobileBarcodeReceived;

        AddByBarcodeCommand = new RelayCommand(OnAddByBarcode);
        RemoveFromCartCommand = new RelayCommand<CartItem>(OnRemoveFromCart);
        IncreaseQtyCommand = new RelayCommand<CartItem>(OnIncreaseQty);
        DecreaseQtyCommand = new RelayCommand<CartItem>(OnDecreaseQty);
        CheckoutCommand = new RelayCommand(OnCheckout, () => CartItems.Count > 0);
        ClearCartCommand = new RelayCommand(OnClearCart);
        ToggleCameraCommand = new RelayCommand(OnToggleCamera);
        ToggleMobileScannerCommand = new RelayCommand(OnToggleMobileScanner);
    }

    public void AddProductToCartById(int productId)
    {
        var product = _productRepo.GetById(productId);
        if (product != null)
        {
            AddProductToCart(product);
        }
    }

    private void OnToggleMobileScanner()
    {
        if (_mobileScanner.IsRunning)
        {
            _mobileScanner.Stop();
            IsMobileScannerActive = false;
            MobileQrBitmap?.Dispose();
            MobileQrBitmap = null;
            MobileScanFeedback = "";
            ScannerToggled?.Invoke(false);
        }
        else
        {
            _mobileScanner.Mode = "sales";
            _mobileScanner.Start();
            IsMobileScannerActive = true;
            MobileUrl = _mobileScanner.Url ?? "";

            if (_mobileScanner.QrImageBytes != null)
            {
                using var stream = new MemoryStream(_mobileScanner.QrImageBytes);
                MobileQrBitmap = new Avalonia.Media.Imaging.Bitmap(stream);
            }

            ScannerToggled?.Invoke(true);
        }
    }

    private void OnMobileBarcodeReceived(string barcode)
    {
        Dispatcher.UIThread.Post(() =>
        {
            MobileScanFeedback = $"Código recibido: {barcode}";
            MobileScanFeedbackColor = "#4CAF50";
        });
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

                Dispatcher.UIThread.Post(() =>
                {
                    BarcodeInput = code;

                    var product = _productRepo.GetByBarcode(code);
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
                    }
                });
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

            Dispatcher.UIThread.Post(() =>
            {
                var old = CameraPreview;
                CameraPreview = bitmap;
                old?.Dispose();
            });
        }
        catch
        {
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
                var isCapturing = _cameraService.IsCapturing;

                Dispatcher.UIThread.Post(() =>
                {
                    IsCameraActive = isCapturing;

                    if (!IsCameraActive)
                    {
                        ScanFeedback = "No se pudo acceder a la c\u00e1mara";
                        ScanFeedbackColor = "#EF5350";
                    }
                });
            });
        }
    }

    private void OnAddByBarcode()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput))
        {
            ScanFeedback = "Ingrese un c\u00f3digo de barras";
            ScanFeedbackColor = "#FF9800";
            return;
        }

        var product = _productRepo.GetByBarcode(BarcodeInput.Trim());
        if (product == null)
        {
            ScanFeedback = $"Producto no encontrado: {BarcodeInput}";
            ScanFeedbackColor = "#EF5350";
            return;
        }

        AddProductToCart(product);
        BarcodeInput = "";
        ScanFeedback = $"\u2713 {product.Name} agregado";
        ScanFeedbackColor = "#4CAF50";
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

        var sinStock = CartItems.FirstOrDefault(ci => ci.Product.Stock < ci.Quantity);
        if (sinStock != null)
        {
            ScanFeedback = $"Sin stock suficiente: {sinStock.Product.Name} (disponible: {sinStock.Product.Stock})";
            ScanFeedbackColor = "#EF5350";
            return;
        }

        try
        {
            var sale = new Sale
            {
                UserId = _currentUser.Id,
                Total = CartTotal,
                PaymentMethod = SelectedPaymentMethod,
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            ScanFeedback = "Error al procesar la venta";
            ScanFeedbackColor = "#EF5350";
        }
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
        _mobileScanner.BarcodeReceived -= OnMobileBarcodeReceived;
        MobileQrBitmap?.Dispose();
        MobileQrBitmap = null;
    }
}
