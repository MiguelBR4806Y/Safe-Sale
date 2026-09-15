using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using SS.Data;
using SS.Models;

namespace SS.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly InventoryViewModel _inventoryViewModel;
    private readonly SalesViewModel _salesViewModel;
    private readonly RecordsViewModel _recordsViewModel;

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

        _dashboardViewModel = new DashboardViewModel(dbPath);
        _inventoryViewModel = new InventoryViewModel(dbPath);
        _salesViewModel = new SalesViewModel(dbPath, user);
        _recordsViewModel = new RecordsViewModel(dbPath);

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
            CurrentViewTitle = "Inventario";
        });

        NavigateSalesCommand = new RelayCommand(() =>
        {
            CurrentViewModel = _salesViewModel;
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
}
