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
    private readonly SaleRepository _saleRepo;

    [ObservableProperty]
    private ObservableCollection<Sale> _sales = new();

    [ObservableProperty]
    private decimal _totalSales;

    [ObservableProperty]
    private DateTimeOffset? _filterFrom = DateTimeOffset.Now.AddDays(-30);

    [ObservableProperty]
    private DateTimeOffset? _filterTo = DateTimeOffset.Now;

    [ObservableProperty]
    private int _totalTransactions;

    [ObservableProperty]
    private Sale? _selectedSale;

    [ObservableProperty]
    private ObservableCollection<SaleItem> _selectedSaleItems = new();

    public bool HasSelectedSale => SelectedSale != null;
    public string SelectedSaleTitle => SelectedSale == null ? "" : $"Venta #{SelectedSale.Id}";
    public string SelectedCashierLabel => SelectedSale == null ? "" : $"Cajero: {SelectedSale.UserName}";
    public decimal SelectedSubtotal => SelectedSale?.Subtotal ?? 0m;
    public decimal SelectedIvaAmount => SelectedSale?.IvaAmount ?? 0m;
    public decimal SelectedIvaRate => SelectedSale?.IvaRate ?? 0m;
    public decimal SelectedTotal => SelectedSale?.Total ?? 0m;
    public string SelectedIvaLabel => $"IVA {SelectedIvaRate:0.##}%";

    public ICommand FilterCommand { get; }

    private bool _hasCustomFilter;
    private bool _syncingFilter;

    public RecordsViewModel(string connectionString)
    {
        _saleRepo = new SaleRepository(connectionString);
        FilterCommand = new RelayCommand(LoadData);
        LoadData();
    }

    partial void OnSelectedSaleChanged(Sale? value)
    {
        SelectedSaleItems = value == null
            ? new ObservableCollection<SaleItem>()
            : _saleRepo.GetItems(value.Id);

        OnPropertyChanged(nameof(HasSelectedSale));
        OnPropertyChanged(nameof(SelectedSaleTitle));
        OnPropertyChanged(nameof(SelectedCashierLabel));
        OnPropertyChanged(nameof(SelectedSubtotal));
        OnPropertyChanged(nameof(SelectedIvaAmount));
        OnPropertyChanged(nameof(SelectedIvaRate));
        OnPropertyChanged(nameof(SelectedTotal));
        OnPropertyChanged(nameof(SelectedIvaLabel));
    }

    partial void OnFilterFromChanged(DateTimeOffset? value)
    {
        if (!_syncingFilter) _hasCustomFilter = true;
    }

    partial void OnFilterToChanged(DateTimeOffset? value)
    {
        if (!_syncingFilter) _hasCustomFilter = true;
    }

    public void LoadData()
    {
        if (!_hasCustomFilter)
        {
            _syncingFilter = true;
            FilterFrom = DateTimeOffset.Now.AddDays(-30);
            FilterTo = DateTimeOffset.Now;
            _syncingFilter = false;
        }

        var from = (FilterFrom?.DateTime ?? DateTime.Today.AddDays(-30)).Date;
        var to = (FilterTo?.DateTime ?? DateTime.Today).Date.AddDays(1).AddSeconds(-1);

        SelectedSale = null;
        Sales = _saleRepo.GetByDateRange(from, to);
        TotalSales = _saleRepo.GetTotalByDateRange(from, to);
        TotalTransactions = Sales.Count;
    }
}
