using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SS.Data;
using SS.Models;

namespace SS.ViewModels;

public partial class RecordsViewModel : ViewModelBase
{
    private readonly SqliteSaleRepository _saleRepo;

    [ObservableProperty]
    private ObservableCollection<Sale> _sales = new();

    [ObservableProperty]
    private decimal _totalSales;

    [ObservableProperty]
    private DateTime _filterFrom = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _filterTo = DateTime.Today;

    [ObservableProperty]
    private int _totalTransactions;

    public ICommand FilterCommand { get; }

    public RecordsViewModel(string dbPath)
    {
        _saleRepo = new SqliteSaleRepository(dbPath);
        FilterCommand = new RelayCommand(LoadData);
        LoadData();
    }

    public void LoadData()
    {
        Sales = _saleRepo.GetByDateRange(FilterFrom, FilterTo);
        TotalSales = _saleRepo.GetTotalByDateRange(FilterFrom, FilterTo);
        TotalTransactions = Sales.Count;
    }
}
