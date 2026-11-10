using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    private readonly ProductRepository _productRepo;
    private readonly SaleRepository _saleRepo;
    private readonly CashSessionRepository _cashRepo;
    private readonly User _currentUser;
    private readonly ICameraService _cameraService;
    private readonly IBarcodeScannerService _barcodeScanner;
    private readonly MobileScannerService _mobileScanner;
    private string _lastScannedCode = "";
    private DateTime _lastScanTime = DateTime.MinValue;
    private CashSession? _openCashSession;

    [ObservableProperty]
    private ObservableCollection<CartItem> _cartItems = new();

    [ObservableProperty]
    private decimal _cartTotal;

    [ObservableProperty]
    private decimal _cartSubtotal;

    [ObservableProperty]
    private string _ivaRateInput = "21";

    [ObservableProperty]
    private int _cartCount;

    [ObservableProperty]
    private string _barcodeInput = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string _statusColor = "#4A148C";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private List<string> _productSuggestions = new();

    [ObservableProperty]
    private List<string> _barcodeSuggestions = new();

    [ObservableProperty]
    private string _lastSaleSummary = "Sin ventas aún";

    [ObservableProperty]
    private bool _isCameraActive;

    [ObservableProperty]
    private string _scanFeedback = "";

    [ObservableProperty]
    private string _scanFeedbackColor = "#4A148C";

    [ObservableProperty]
    private string _selectedPaymentMethod = "Efectivo";

    public string[] PaymentMethods { get; } = new[] { "Efectivo", "Tarjeta", "Transferencia" };

    public bool HasCartItems => CartItems.Count > 0;
    public bool IsCartEmpty => CartItems.Count == 0;
    public bool HasOpenCashSession => _openCashSession != null;
    public bool IsCashClosed => _openCashSession == null;
    public bool CanCheckout => HasCartItems && HasOpenCashSession;
    public string CashSessionStatus => _openCashSession == null
        ? "No hay caja abierta. Abrí una caja para poder cobrar."
        : $"Caja abierta desde {_openCashSession.OpenedAtLabel} · Efectivo esperado: {_openCashSession.ExpectedCashLabel}";
    public string CartCountLabel => $"{CartCount} items";
    public string CheckoutLabel => $"COBRAR  ${CartTotal:F2}";
    public decimal IvaRate => TryParseIvaRate(out var rate, out _) ? rate : 0m;
    public decimal IvaAmount => Math.Round(CartSubtotal * IvaRate / 100m, 2);
    public string IvaRateLabel => $"IVA {IvaRate:0.##}%";

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
    private List<string> _availableCameras = new();

    [ObservableProperty]
    private int _selectedCameraIndex;

    public event Action<bool>? ScannerToggled;
    public event Action? ClearCartRequested;
    public event Action? CheckoutRequested;

    public ICommand AddByBarcodeCommand { get; }
    public ICommand AddByNameCommand { get; }
    public ICommand RemoveFromCartCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand ToggleCameraCommand { get; }
    public ICommand ToggleMobileScannerCommand { get; }

    public SalesViewModel(string connectionString, User currentUser, MobileScannerService mobileScanner)
    {
        _currentUser = currentUser;
        _productRepo = new ProductRepository(connectionString);
        _saleRepo = new SaleRepository(connectionString);
        _cashRepo = new CashSessionRepository(connectionString);
        _cameraService = new CameraService();
        _barcodeScanner = new BarcodeScannerService();
        _mobileScanner = mobileScanner;
        SelectedCameraIndex = 0;

        _cameraService.FrameAvailable += OnFrameAvailable;

        LoadData();

        AddByBarcodeCommand = new RelayCommand(OnAddByBarcode);
        AddByNameCommand = new RelayCommand(OnAddByName);
        RemoveFromCartCommand = new RelayCommand<CartItem>(OnRemoveFromCart);
        IncreaseQtyCommand = new RelayCommand<CartItem>(OnIncreaseQty);
        DecreaseQtyCommand = new RelayCommand<CartItem>(OnDecreaseQty);
        CheckoutCommand = new RelayCommand(OnCheckout, () => CanCheckout);
        ClearCartCommand = new RelayCommand(OnClearCart);
        ToggleCameraCommand = new RelayCommand(OnToggleCamera);
        ToggleMobileScannerCommand = new RelayCommand(OnToggleMobileScanner);
    }

    public void LoadData()
    {
        ProductSuggestions = _productRepo.GetAll()
            .Select(p => p.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        BarcodeSuggestions = _productRepo.GetAll()
            .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
            .OrderBy(p => p.Barcode, StringComparer.Ordinal)
            .Select(p => $"{p.Barcode} — {p.Name}")
            .ToList();

        var lastSale = _saleRepo.GetAll().FirstOrDefault();
        LastSaleSummary = lastSale == null ? "Sin ventas aún" : $"Venta #{lastSale.Id} · ${lastSale.Total:F2}";

        RefreshCashSession();
    }

    public void RefreshCashSession()
    {
        _openCashSession = _cashRepo.GetOpenSession(_currentUser.Id);
        OnPropertyChanged(nameof(HasOpenCashSession));
        OnPropertyChanged(nameof(IsCashClosed));
        OnPropertyChanged(nameof(CashSessionStatus));
        OnPropertyChanged(nameof(CanCheckout));
        if (CheckoutCommand is RelayCommand command)
            command.NotifyCanExecuteChanged();
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
            var cameraIndex = SelectedCameraIndex;

            Task.Run(async () =>
            {
                if (AvailableCameras.Count == 0)
                {
                    var cameras = _cameraService.GetAvailableCameras().ToList();
                    cameraIndex = 0;

                    if (cameras.Count > 0)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            AvailableCameras = cameras;
                            SelectedCameraIndex = 0;
                        });
                    }
                }

                await _cameraService.StartCaptureAsync(cameraIndex);
                var isCapturing = _cameraService.IsCapturing;

                Dispatcher.UIThread.Post(() =>
                {
                    IsCameraActive = isCapturing;

                    if (!IsCameraActive)
                    {
                        ScanFeedback = "No se pudo acceder a la cámara";
                        ScanFeedbackColor = "#EF5350";
                    }
                });
            });
        }
    }

    private void OnAddByBarcode()
    {
        var raw = BarcodeInput?.Trim() ?? "";
        if (raw.Length == 0)
        {
            SetFeedback("Ingrese un código de barras", "#FF9800");
            return;
        }

        var code = raw;
        var sepIndex = code.IndexOf('—');
        if (sepIndex > 0)
        {
            code = code.Substring(0, sepIndex).Trim();
        }

        var products = _productRepo.GetAll();
        var product = products.FirstOrDefault(p => p.Barcode == code)
                   ?? products.FirstOrDefault(p => p.Barcode.StartsWith(code, StringComparison.Ordinal));

        if (product == null)
        {
            SetFeedback($"Producto no encontrado: {code}", "#EF5350");
            return;
        }

        AddProductToCart(product);
        BarcodeInput = "";
    }

    private void OnAddByName()
    {
        var text = SearchText?.Trim() ?? "";
        if (text.Length == 0)
        {
            SetFeedback("Ingrese el nombre de un producto", "#FF9800");
            return;
        }

        var products = _productRepo.GetAll();
        var product = products.FirstOrDefault(p => string.Equals(p.Name, text, StringComparison.OrdinalIgnoreCase))
                   ?? products.FirstOrDefault(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

        if (product == null)
        {
            SetFeedback($"Producto no encontrado: {text}", "#EF5350");
            return;
        }

        AddProductToCart(product);
        SearchText = "";
    }

    public void SetFeedback(string message, string color = "#4A148C")
    {
        StatusMessage = message;
        StatusColor = color;
    }

    private void AddProductToCart(Product product)
    {
        if (product.Stock <= 0)
        {
            SetFeedback($"Sin stock disponible: {product.Name}", "#EF5350");
            return;
        }

        var existing = CartItems.FirstOrDefault(ci => ci.Product.Id == product.Id);
        if (existing != null)
        {
            if (existing.Quantity >= existing.Product.Stock)
            {
                SetFeedback($"Stock máximo de {product.Name} (disponible: {product.Stock})", "#EF5350");
                return;
            }
            existing.Quantity += 1;
        }
        else
        {
            CartItems.Add(new CartItem { Product = product, Quantity = 1 });
        }
        UpdateCartTotals();
        SetFeedback($"{product.Name} agregado al carrito", "#4CAF50");
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
        if (item.Quantity >= item.Product.Stock)
        {
            SetFeedback($"Stock máximo de {item.ProductName} (disponible: {item.Product.Stock})", "#EF5350");
            return;
        }
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

        if (!HasOpenCashSession)
        {
            SetFeedback("Abrí una caja antes de cobrar.", "#FF9800");
            return;
        }

        var sinStock = CartItems.FirstOrDefault(ci => ci.Product.Stock < ci.Quantity);
        if (sinStock != null)
        {
            SetFeedback($"Sin stock suficiente: {sinStock.Product.Name} (disponible: {sinStock.Product.Stock})", "#EF5350");
            return;
        }

        if (!TryParseIvaRate(out _, out var ivaError))
        {
            SetFeedback(ivaError!, "#EF5350");
            return;
        }

        CheckoutRequested?.Invoke();
    }

    public void ConfirmCheckout()
    {
        if (CartItems.Count == 0) return;

        var lines = CartItems
            .Select(ci => new SaleRepository.SaleLine(ci.Product.Id, ci.Quantity, ci.Price))
            .ToList();

        try
        {
            var session = _cashRepo.GetOpenSession(_currentUser.Id);
            if (session == null)
            {
                SetFeedback("Abrí una caja antes de cobrar.", "#FF9800");
                return;
            }

            var total = CartTotal;
            if (!TryParseIvaRate(out var ivaRate, out var ivaError))
            {
                SetFeedback(ivaError!, "#EF5350");
                return;
            }

            int saleId = _saleRepo.RegisterSale(_currentUser.Id, session.Id, SelectedPaymentMethod, ivaRate, lines);

            foreach (var ci in CartItems)
                ci.Product.Stock -= ci.Quantity;

            LastSaleSummary = $"Venta #{saleId} · ${total:F2}";
            ClearCart();
            RefreshCashSession();
            SetFeedback($"Venta #{saleId} completada - Total: ${total:F2}", "#4CAF50");
        }
        catch (Exception ex)
        {
            SetFeedback($"No se pudo cobrar: {ex.Message}", "#EF5350");
        }
    }

    private void OnClearCart()
    {
        ClearCartRequested?.Invoke();
    }

    public void ConfirmClearCart()
    {
        ClearCart();
    }

    private void ClearCart()
    {
        CartItems.Clear();
        UpdateCartTotals();
        StatusMessage = "";
        StatusColor = "#4A148C";
    }

    private void UpdateCartTotals()
    {
        CartSubtotal = CartItems.Sum(ci => ci.Subtotal);
        CartCount = CartItems.Sum(ci => ci.Quantity);
        CartTotal = CartSubtotal + IvaAmount;
        OnPropertyChanged(nameof(HasCartItems));
        OnPropertyChanged(nameof(IsCartEmpty));
        OnPropertyChanged(nameof(CartCountLabel));
        OnPropertyChanged(nameof(CheckoutLabel));
        OnPropertyChanged(nameof(IvaAmount));
        OnPropertyChanged(nameof(IvaRate));
        OnPropertyChanged(nameof(IvaRateLabel));
        OnPropertyChanged(nameof(CanCheckout));
        if (CheckoutCommand is RelayCommand command)
            command.NotifyCanExecuteChanged();
    }

    partial void OnIvaRateInputChanged(string value)
    {
        UpdateCartTotals();
    }

    private bool TryParseIvaRate(out decimal rate, out string? error)
    {
        error = null;
        var raw = (IvaRateInput ?? "").Trim().Replace(',', '.');

        if (raw.Length == 0)
        {
            rate = 0m;
            return true;
        }

        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out rate))
        {
            error = "La tasa de IVA debe ser un número (ej: 21 o 0).";
            return false;
        }

        if (rate < 0 || rate > 100)
        {
            error = "La tasa de IVA debe estar entre 0 y 100.";
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        _cameraService.FrameAvailable -= OnFrameAvailable;
        _cameraService.StopCapture();
        _cameraService.Dispose();
        CameraPreview?.Dispose();
        CameraPreview = null;
        MobileQrBitmap?.Dispose();
        MobileQrBitmap = null;
    }
}
