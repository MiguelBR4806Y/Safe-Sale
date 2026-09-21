using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using SS.Data;
using SS.Models;
using SS.Services;

namespace SS.ViewModels;

public enum SelectionMode { None, Delete, Cart }

public partial class CategoryGroup : ObservableObject
{
    public Category Category { get; set; } = null!;
    public ObservableCollection<Product> Products { get; set; } = new();
    public InventoryMetrics Metrics { get; set; } = new();
    public bool IsEmpty => Products.Count == 0;
    public bool HasProducts => Products.Count > 0;

    [ObservableProperty]
    private bool _isExpanded = true;

    public string ChevronIcon => IsExpanded ? "\u25BC" : "\u25B6";

    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ChevronIcon));
    }
}

public partial class InventoryViewModel : ViewModelBase, IDisposable
{
    private readonly SqliteProductRepository _productRepo;
    private readonly SqliteCategoryRepository _categoryRepo;
    private readonly MobileScannerService _mobileScanner;
    private readonly ICameraService _cameraService;
    private readonly IBarcodeScannerService _barcodeScanner;
    private List<Product> _allProducts = new();
    private string _lastScannedCode = "";
    private DateTime _lastScanTime = DateTime.MinValue;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<CategoryGroup> _categoryGroups = new();

    [ObservableProperty]
    private bool _isCategoryGroupsEmpty = true;

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private InventoryMetrics _globalMetrics = new();

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

    [ObservableProperty]
    private bool _isSelectionModeActive;

    [ObservableProperty]
    private SelectionMode _activeSelectionMode = SelectionMode.None;

    [ObservableProperty]
    private ObservableCollection<Product> _selectedProducts = new();

    public int SelectedCount => SelectedProducts.Count;

    public string DeleteButtonText => !IsSelectionModeActive || ActiveSelectionMode != SelectionMode.Delete
        ? "Eliminar"
        : SelectedProducts.Count == 0
            ? "Cancelar"
            : $"Eliminar ({SelectedProducts.Count})";

    public string AddToCartButtonText => !IsSelectionModeActive || ActiveSelectionMode != SelectionMode.Cart
        ? "Agregar al Carrito"
        : SelectedProducts.Count == 0
            ? "Cancelar"
            : $"Agregar al Carrito ({SelectedProducts.Count})";

    public bool IsDeleteEnabled => HasProducts && (!IsSelectionModeActive || ActiveSelectionMode == SelectionMode.Delete);
    public bool IsCartEnabled => HasProducts && (!IsSelectionModeActive || ActiveSelectionMode == SelectionMode.Cart);

    public bool IsDeleteHighlighted => IsSelectionModeActive && ActiveSelectionMode == SelectionMode.Delete;
    public bool IsCartHighlighted => IsSelectionModeActive && ActiveSelectionMode == SelectionMode.Cart;

    public ICommand AddProductCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ToggleMobileScannerCommand { get; }
    public ICommand ToggleCameraCommand { get; }
    public ICommand ClearInventoryCommand { get; }
    public ICommand ToggleProductSelectionCommand { get; }
    public ICommand ConfirmSelectionCommand { get; }
    public ICommand CancelSelectionCommand { get; }

    public event Action? AddProductRequested;
    public event Action<Product>? EditProductRequested;
    public event Action<int>? AddToCartRequested;
    public event Action<List<int>>? AddMultipleToCartRequested;
    public event Action<List<int>>? DeleteMultipleRequested;
    public event Action<bool>? ScannerToggled;
    public event Action? ClearInventoryRequested;

    public bool HasProducts => _allProducts.Count > 0;

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
        DeleteProductCommand = new RelayCommand(OnDeleteProductClick);
        AddToCartCommand = new RelayCommand(OnAddToCartClick);
        SearchCommand = new RelayCommand(OnSearch);
        RefreshCommand = new RelayCommand(LoadData);
        ToggleMobileScannerCommand = new RelayCommand(OnToggleMobileScanner);
        ToggleCameraCommand = new RelayCommand(OnToggleCamera);
        ClearInventoryCommand = new RelayCommand(OnClearInventory);
        ToggleProductSelectionCommand = new RelayCommand<Product>(OnToggleProductSelection);
        ConfirmSelectionCommand = new RelayCommand(OnConfirmSelection);
        CancelSelectionCommand = new RelayCommand(OnCancelSelection);

        LoadData();
    }

    public void LoadData()
    {
        SearchText = "";
        _allProducts = _productRepo.GetAll().ToList();
        GlobalMetrics = InventoryMetrics.Calculate(_allProducts);
        OnPropertyChanged(nameof(HasProducts));
        OnPropertyChanged(nameof(IsDeleteEnabled));
        OnPropertyChanged(nameof(IsCartEnabled));
        ApplyFilter();
    }

    private void BuildCategoryGroups()
    {
        var categories = _categoryRepo.GetAll();
        CategoryGroups.Clear();

        foreach (var cat in categories)
        {
            var catProducts = _allProducts.Where(p => p.CategoryId == cat.Id).ToList();

            CategoryGroups.Add(new CategoryGroup
            {
                Category = cat,
                Products = new ObservableCollection<Product>(catProducts),
                Metrics = InventoryMetrics.Calculate(_allProducts, cat.Id)
            });
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Products = new ObservableCollection<Product>(_allProducts);
        }
        else
        {
            var filtered = _allProducts.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            Products = new ObservableCollection<Product>(filtered);
        }

        GlobalMetrics = InventoryMetrics.Calculate(_allProducts);
        RebuildCategoryGroupsWithFilter();
    }

    private void RebuildCategoryGroupsWithFilter()
    {
        var categories = _categoryRepo.GetAll();
        CategoryGroups.Clear();
        var hasSearch = !string.IsNullOrWhiteSpace(SearchText);

        foreach (var cat in categories)
        {
            var catProducts = Products.Where(p => p.CategoryId == cat.Id).ToList();

            if (hasSearch && catProducts.Count == 0)
                continue;

            CategoryGroups.Add(new CategoryGroup
            {
                Category = cat,
                Products = new ObservableCollection<Product>(catProducts),
                Metrics = InventoryMetrics.Calculate(_allProducts, cat.Id)
            });
        }

        IsCategoryGroupsEmpty = CategoryGroups.Count == 0;
    }

    private void OnSearch()
    {
        ApplyFilter();
    }

    private void OnAddProduct()
    {
        AddProductRequested?.Invoke();
    }

    public void HandleAddProductResult(bool success)
    {
        if (success) LoadData();
    }

    private void OnClearInventory()
    {
        ClearInventoryRequested?.Invoke();
    }

    public void ConfirmClearInventory()
    {
        _productRepo.DeleteAll();
        LoadData();
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
    }

    partial void OnIsSelectionModeActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(DeleteButtonText));
        OnPropertyChanged(nameof(AddToCartButtonText));
        OnPropertyChanged(nameof(IsDeleteEnabled));
        OnPropertyChanged(nameof(IsCartEnabled));
        OnPropertyChanged(nameof(IsDeleteHighlighted));
        OnPropertyChanged(nameof(IsCartHighlighted));
    }

    partial void OnActiveSelectionModeChanged(SelectionMode value)
    {
        OnPropertyChanged(nameof(DeleteButtonText));
        OnPropertyChanged(nameof(AddToCartButtonText));
        OnPropertyChanged(nameof(IsDeleteEnabled));
        OnPropertyChanged(nameof(IsCartEnabled));
        OnPropertyChanged(nameof(IsDeleteHighlighted));
        OnPropertyChanged(nameof(IsCartHighlighted));
    }

    private void OnToggleProductSelection(Product? product)
    {
        if (product == null) return;

        var existing = SelectedProducts.FirstOrDefault(p => p.Id == product.Id);
        if (existing != null)
        {
            SelectedProducts.Remove(existing);
        }
        else
        {
            SelectedProducts.Add(product);
        }

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(DeleteButtonText));
        OnPropertyChanged(nameof(AddToCartButtonText));
    }

    private void OnDeleteProductClick()
    {
        Console.WriteLine($"[DEBUG] OnDeleteProductClick called. IsSelectionModeActive={IsSelectionModeActive}, ActiveSelectionMode={ActiveSelectionMode}, SelectedCount={SelectedProducts.Count}");
        if (!IsSelectionModeActive)
        {
            IsSelectionModeActive = true;
            ActiveSelectionMode = SelectionMode.Delete;
            SelectedProducts.Clear();
            OnPropertyChanged(nameof(SelectedCount));
            Console.WriteLine("[DEBUG] Delete: activated selection mode");
            return;
        }

        if (ActiveSelectionMode != SelectionMode.Delete)
        {
            ActiveSelectionMode = SelectionMode.Delete;
            Console.WriteLine("[DEBUG] Delete: switched to Delete mode");
            return;
        }

        if (SelectedProducts.Count == 0)
        {
            Console.WriteLine("[DEBUG] Delete: no products selected, cancelling");
            OnCancelSelection();
            return;
        }

        Console.WriteLine($"[DEBUG] Delete: invoking DeleteMultipleRequested with {SelectedProducts.Count} products");
        DeleteMultipleRequested?.Invoke(SelectedProducts.Select(p => p.Id).ToList());
    }

    public void ConfirmDeleteProducts(List<int> productIds)
    {
        foreach (var id in productIds)
        {
            _productRepo.Delete(id);
        }
        OnCancelSelection();
        LoadData();
    }

    private void OnAddToCartClick()
    {
        Console.WriteLine($"[DEBUG] OnAddToCartClick called. IsSelectionModeActive={IsSelectionModeActive}, ActiveSelectionMode={ActiveSelectionMode}, SelectedCount={SelectedProducts.Count}");
        if (!IsSelectionModeActive)
        {
            IsSelectionModeActive = true;
            ActiveSelectionMode = SelectionMode.Cart;
            SelectedProducts.Clear();
            OnPropertyChanged(nameof(SelectedCount));
            Console.WriteLine("[DEBUG] Cart: activated selection mode");
            return;
        }

        if (ActiveSelectionMode != SelectionMode.Cart)
        {
            ActiveSelectionMode = SelectionMode.Cart;
            Console.WriteLine("[DEBUG] Cart: switched to Cart mode");
            return;
        }

        if (SelectedProducts.Count == 0)
        {
            Console.WriteLine("[DEBUG] Cart: no products selected, cancelling");
            OnCancelSelection();
            return;
        }

        Console.WriteLine($"[DEBUG] Cart: invoking AddMultipleToCartRequested with {SelectedProducts.Count} products");
        AddMultipleToCartRequested?.Invoke(SelectedProducts.Select(p => p.Id).ToList());
    }

    public void ConfirmAddMultipleToCart(List<int> productIds)
    {
        OnCancelSelection();
    }

    private void OnConfirmSelection()
    {
        if (SelectedProducts.Count == 0) return;

        if (ActiveSelectionMode == SelectionMode.Delete)
        {
            DeleteMultipleRequested?.Invoke(SelectedProducts.Select(p => p.Id).ToList());
        }
        else if (ActiveSelectionMode == SelectionMode.Cart)
        {
            AddMultipleToCartRequested?.Invoke(SelectedProducts.Select(p => p.Id).ToList());
        }
    }

    private void OnCancelSelection()
    {
        IsSelectionModeActive = false;
        ActiveSelectionMode = SelectionMode.None;
        SelectedProducts.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(DeleteButtonText));
        OnPropertyChanged(nameof(AddToCartButtonText));
    }

    private void OnDeleteProduct()
    {
        if (SelectedProduct == null) return;
        _productRepo.Delete(SelectedProduct.Id);
        SelectedProduct = null;
        LoadData();
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
            MobileScanFeedback = $"Codigo recibido: {barcode}";
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
                MobileScanFeedback = "Precio invalido";
                MobileScanFeedbackColor = "#EF5350";
                return;
            }

            int stock = 0;
            if (!string.IsNullOrEmpty(stockStr)) int.TryParse(stockStr, out stock);

            int minStock = 5;
            if (!string.IsNullOrEmpty(minStockStr)) int.TryParse(minStockStr, out minStock);

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

            MobileScanFeedback = $"\u2713 \"{name}\" agregado";
            MobileScanFeedbackColor = "#4CAF50";
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            MobileScanFeedback = "Ya existe un producto con ese codigo";
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
                        ScanFeedback = "No se pudo acceder a la camara";
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
                        ScanFeedback = $"Nuevo codigo: {code}";
                        ScanFeedbackColor = "#FF9800";
                        AddProductRequested?.Invoke();
                    }
                });
            }
            else if (!string.IsNullOrEmpty(code) && code == _lastScannedCode)
            {
                if ((DateTime.Now - _lastScanTime).TotalSeconds > 3)
                    _lastScannedCode = "";
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
        catch { }
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
