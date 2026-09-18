using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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

public partial class InventoryViewModel : ViewModelBase, IDisposable
{
    private readonly SqliteProductRepository _productRepo;
    private readonly SqliteCategoryRepository _categoryRepo;
    private readonly MobileScannerService _mobileScanner;
    private readonly ICameraService _cameraService;
    private readonly IBarcodeScannerService _barcodeScanner;
    private string _lastScannedCode = "";
    private DateTime _lastScanTime = DateTime.MinValue;

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

    [ObservableProperty]
    private bool _isMobileScannerActive;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _mobileQrBitmap;

    [ObservableProperty]
    private string _mobileUrl = "";

    [ObservableProperty]
    private string _mobileStatusMessage = "";

    [ObservableProperty]
    private string _mobileScanFeedback = "";

    [ObservableProperty]
    private string _mobileScanFeedbackColor = "#4A148C";

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

    [ObservableProperty]
    private List<string> _availableCameras = new();

    [ObservableProperty]
    private int _selectedCameraIndex;

    public ICommand AddProductCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand LoadByBarcodeCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ToggleMobileScannerCommand { get; }
    public ICommand ToggleCameraCommand { get; }

    public event Action? AddProductRequested;
    public event Action<Product>? EditProductRequested;
    public event Action<int>? AddToCartRequested;
    public event Action<bool>? ScannerToggled;

    public InventoryViewModel(string dbPath, MobileScannerService mobileScanner)
    {
        _productRepo = new SqliteProductRepository(dbPath);
        _categoryRepo = new SqliteCategoryRepository(dbPath);
        _mobileScanner = mobileScanner;
        _cameraService = new CameraService();
        _barcodeScanner = new BarcodeScannerService();
        AvailableCameras = _cameraService.GetAvailableCameras().ToList();
        SelectedCameraIndex = 0;

        _mobileScanner.BarcodeReceived += OnMobileBarcodeReceived;
        _cameraService.FrameAvailable += OnFrameAvailable;

        AddProductCommand = new RelayCommand(OnAddProduct);
        EditProductCommand = new RelayCommand(OnEditProduct, () => SelectedProduct != null);
        DeleteProductCommand = new RelayCommand(OnDeleteProduct, () => SelectedProduct != null);
        AddToCartCommand = new RelayCommand(OnAddToCart, () => SelectedProduct != null);
        SearchCommand = new RelayCommand(OnSearch);
        LoadByBarcodeCommand = new RelayCommand(OnLoadByBarcode);
        RefreshCommand = new RelayCommand(LoadData);
        ToggleMobileScannerCommand = new RelayCommand(OnToggleMobileScanner);
        ToggleCameraCommand = new RelayCommand(OnToggleCamera);

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
        AddProductRequested?.Invoke();
    }

    public void HandleAddProductResult(bool success)
    {
        if (success)
        {
            LoadData();
        }
    }

    public void SetScannerMode(string mode)
    {
        _mobileScanner.Mode = mode;
    }

    public Product? FindProductByBarcode(string barcode)
    {
        return _productRepo.GetByBarcode(barcode);
    }

    private void OnAddToCart()
    {
        if (SelectedProduct == null) return;
        AddToCartRequested?.Invoke(SelectedProduct.Id);
    }

    private void OnEditProduct()
    {
        if (SelectedProduct == null) return;
        EditProductRequested?.Invoke(SelectedProduct);
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        ((RelayCommand)EditProductCommand).NotifyCanExecuteChanged();
        ((RelayCommand)DeleteProductCommand).NotifyCanExecuteChanged();
        ((RelayCommand)AddToCartCommand).NotifyCanExecuteChanged();
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

    private void OnToggleMobileScanner()
    {
        if (_mobileScanner.IsRunning)
        {
            _mobileScanner.Stop();
            IsMobileScannerActive = false;
            MobileQrBitmap?.Dispose();
            MobileQrBitmap = null;
            MobileStatusMessage = "";
            MobileScanFeedback = "";
            ScannerToggled?.Invoke(false);
        }
        else
        {
            _mobileScanner.Start();
            IsMobileScannerActive = true;
            MobileUrl = _mobileScanner.Url ?? "";
            MobileStatusMessage = "Escanea el QR con tu teléfono para agregar productos";

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
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            MobileScanFeedback = $"Código recibido: {barcode}";
            MobileScanFeedbackColor = "#4CAF50";
        });
    }

    public void HandleMobileAddProduct(string json)
    {
        try
        {
            var barcode = ExtractJsonField(json, "barcode");
            var name = ExtractJsonField(json, "name");
            var priceStr = ExtractJsonField(json, "price");
            var stockStr = ExtractJsonField(json, "stock");
            var minStockStr = ExtractJsonField(json, "min_stock");

            if (string.IsNullOrEmpty(barcode) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(priceStr))
            {
                MobileScanFeedback = "Datos incompletos del producto";
                MobileScanFeedbackColor = "#EF5350";
                return;
            }

            if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                MobileScanFeedback = "Precio inválido";
                MobileScanFeedbackColor = "#EF5350";
                return;
            }

            int stock = 0;
            if (!string.IsNullOrEmpty(stockStr))
                int.TryParse(stockStr, out stock);

            int minStock = 5;
            if (!string.IsNullOrEmpty(minStockStr))
                int.TryParse(minStockStr, out minStock);

            var product = new Product
            {
                Name = name,
                Barcode = barcode,
                Price = price,
                Stock = stock,
                MinStock = minStock
            };

            _productRepo.Add(product);
            LoadData();

            MobileScanFeedback = $"✓ \"{name}\" agregado";
            MobileScanFeedbackColor = "#4CAF50";
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            MobileScanFeedback = "Ya existe un producto con ese código";
            MobileScanFeedbackColor = "#EF5350";
        }
        catch
        {
            MobileScanFeedback = "Error al guardar producto";
            MobileScanFeedbackColor = "#EF5350";
        }
    }

    private static string ExtractJsonField(string json, string field)
    {
        var search = $"\"{field}\":";
        var idx = json.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return "";
        idx += search.Length;
        while (idx < json.Length && json[idx] == ' ') idx++;
        if (idx >= json.Length) return "";

        if (json[idx] == '"')
        {
            idx++;
            var end = json.IndexOf('"', idx);
            if (end < 0) return json.Substring(idx);
            return json.Substring(idx, end - idx);
        }
        else
        {
            var end = idx;
            while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
            return json.Substring(idx, end - idx).Trim();
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
                await _cameraService.StartCaptureAsync(SelectedCameraIndex);
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
                    var product = _productRepo.GetByBarcode(code);
                    if (product != null)
                    {
                        ScanFeedback = $"\u2713 {product.Name} encontrado (Stock: {product.Stock})";
                        ScanFeedbackColor = "#4CAF50";
                        SelectedProduct = Products.FirstOrDefault(p => p.Id == product.Id);
                    }
                    else
                    {
                        ScanFeedback = $"Nuevo código: {code}";
                        ScanFeedbackColor = "#FF9800";
                        AddProductRequested?.Invoke();
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

    public void Dispose()
    {
        _mobileScanner.BarcodeReceived -= OnMobileBarcodeReceived;
        MobileQrBitmap?.Dispose();
        MobileQrBitmap = null;
        _cameraService.FrameAvailable -= OnFrameAvailable;
        _cameraService.StopCapture();
        _cameraService.Dispose();
        CameraPreview?.Dispose();
        CameraPreview = null;
    }
}
