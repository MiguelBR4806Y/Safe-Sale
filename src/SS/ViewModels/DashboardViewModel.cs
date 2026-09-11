using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using SS.Data;
using SS.Models;

namespace SS.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly SqliteProductRepository _productRepo;
    private readonly SqliteSaleRepository _saleRepo;

    [ObservableProperty]
    private decimal _todaySales;

    [ObservableProperty]
    private decimal _monthIncome;

    [ObservableProperty]
    private int _activeProducts;

    [ObservableProperty]
    private int _todayCustomers;

    [ObservableProperty]
    private ObservableCollection<Sale> _recentSales = new();

    public DashboardViewModel(string dbPath)
    {
        _productRepo = new SqliteProductRepository(dbPath);
        _saleRepo = new SqliteSaleRepository(dbPath);
        LoadData();
    }

    public void LoadData()
    {
        TodaySales = _saleRepo.GetTodayTotal();
        MonthIncome = _saleRepo.GetMonthTotal();
        ActiveProducts = _productRepo.GetAll().Count;
        TodayCustomers = _saleRepo.GetSaleCount();
        RecentSales = _saleRepo.GetAll();
    }
}
