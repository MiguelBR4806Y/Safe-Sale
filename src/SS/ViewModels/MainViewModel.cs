using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading;
using System.Windows.Input;
using SS.Data;
using SS.Models;
using SS.Services;

namespace SS.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly InventoryViewModel _inventoryViewModel;
    private readonly SalesViewModel _salesViewModel;
    private readonly RecordsViewModel _recordsViewModel;
    private readonly MobileScannerService _sharedScanner;
    private readonly Timer _pollTimer;

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    [ObservableProperty]
    private string _currentViewTitle = "Dashboard";

    [ObservableProperty]
    private User _currentUser;

    public string CurrentUsername => CurrentUser?.Username ?? "";
    public string CurrentRole => CurrentUser?.Role == "admin" ? "Administrador" : "Cajero";

    public ICommand NavigateDashboardCommand { get; }
    public ICommand NavigateInventoryCommand { get; }
    public ICommand NavigateSalesCommand { get; }
    public ICommand NavigateRecordsCommand { get; }
    public ICommand LogoutCommand { get; }

    public event Action? LogoutRequested;

    public MainViewModel(User user)
    {
        CurrentUser = user;

        var dbPath = AppDatabase.DbPath;

        _sharedScanner = new MobileScannerService(dbPath);

        _dashboardViewModel = new DashboardViewModel(dbPath);
        _inventoryViewModel = new InventoryViewModel(dbPath, _sharedScanner);
        _salesViewModel = new SalesViewModel(dbPath, user, _sharedScanner);
        _recordsViewModel = new RecordsViewModel(dbPath);

        _inventoryViewModel.AddToCartRequested += OnAddToCartFromInventory;
        _inventoryViewModel.ScannerToggled += OnScannerToggled;
        _salesViewModel.ScannerToggled += OnScannerToggled;

        _pollTimer = new Timer(_ => PollBarcodes(), null, Timeout.Infinite, Timeout.Infinite);

        NavigateDashboardCommand = new RelayCommand(() =>
        {
            CurrentViewModel = _dashboardViewModel;
            _dashboardViewModel.LoadData();
            CurrentViewTitle = "Dashboard";
        });

        NavigateInventoryCommand = new RelayCommand(() =>
        {
            CurrentViewModel = _inventoryViewModel;
            _inventoryViewModel.LoadData();
            _sharedScanner.Mode = "inventory";
            CurrentViewTitle = "Inventario";
        });

        NavigateSalesCommand = new RelayCommand(() =>
        {
            CurrentViewModel = _salesViewModel;
            _sharedScanner.Mode = "sales";
            CurrentViewTitle = "Ventas";
        });

        NavigateRecordsCommand = new RelayCommand(() =>
        {
            CurrentViewModel = _recordsViewModel;
            _recordsViewModel.LoadData();
            CurrentViewTitle = "Registros";
        });

        LogoutCommand = new RelayCommand(() =>
        {
            LogoutRequested?.Invoke();
        });

        CurrentViewModel = _dashboardViewModel;
    }

    private void PollBarcodes()
    {
        if (!_sharedScanner.IsRunning) return;

        while (_sharedScanner.HasBarcodes())
        {
            var code = _sharedScanner.DequeueBarcode();
            if (string.IsNullOrEmpty(code)) continue;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (code.StartsWith("ADD:"))
                {
                    _inventoryViewModel.HandleMobileAddProduct(code.Substring(4));
                    return;
                }

                if (code.StartsWith("SALES:"))
                {
                    var barcode = code.Substring(6);
                    var product = _inventoryViewModel.FindProductByBarcode(barcode);
                    if (product != null)
                    {
                        _salesViewModel.AddProductToCartById(product.Id);
                        _inventoryViewModel.MobileScanFeedback = $"\u2713 {product.Name} agregado al carrito";
                        _inventoryViewModel.MobileScanFeedbackColor = "#4CAF50";
                    }
                    return;
                }

                var product2 = _inventoryViewModel.FindProductByBarcode(code);
                if (product2 != null)
                {
                    if (_sharedScanner.Mode == "sales")
                    {
                        _salesViewModel.AddProductToCartById(product2.Id);
                        _inventoryViewModel.MobileScanFeedback = $"\u2713 {product2.Name} agregado al carrito";
                        _inventoryViewModel.MobileScanFeedbackColor = "#4CAF50";
                    }
                    else
                    {
                        _inventoryViewModel.MobileScanFeedback = $"\u2713 {product2.Name} encontrado (Stock: {product2.Stock})";
                        _inventoryViewModel.MobileScanFeedbackColor = "#4CAF50";
                    }
                }
                else
                {
                    if (_sharedScanner.Mode == "sales")
                    {
                        _inventoryViewModel.MobileScanFeedback = $"Producto no encontrado: {code}. Agrégalo desde Inventario.";
                        _inventoryViewModel.MobileScanFeedbackColor = "#EF5350";
                    }
                    else
                    {
                        _inventoryViewModel.MobileScanFeedback = $"Código nuevo: {code}. Completa los datos en tu teléfono.";
                        _inventoryViewModel.MobileScanFeedbackColor = "#FF9800";
                    }
                }
            });
        }
    }

    private void OnAddToCartFromInventory(int productId)
    {
        _salesViewModel.AddProductToCartById(productId);
        CurrentViewModel = _salesViewModel;
        CurrentViewTitle = "Ventas";
    }

    private void OnScannerToggled(bool active)
    {
        if (active)
            _pollTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(300));
        else
            _pollTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        _pollTimer.Dispose();
        _sharedScanner.Stop();
        _sharedScanner.Dispose();
    }
}
