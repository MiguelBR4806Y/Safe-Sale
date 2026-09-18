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
    private DateTimeOffset? _filterFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day).AddDays(-30);

    [ObservableProperty]
    private DateTimeOffset? _filterTo = DateTime.Today;

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
        var from = FilterFrom?.DateTime ?? DateTime.Today.AddDays(-30);
        var to = FilterTo?.DateTime ?? DateTime.Today;
        Sales = _saleRepo.GetByDateRange(from, to);
        TotalSales = _saleRepo.GetTotalByDateRange(from, to);
        TotalTransactions = Sales.Count;
    }
}
