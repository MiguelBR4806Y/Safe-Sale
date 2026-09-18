using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class Product : ObservableObject
{
    private int _id;
    private string _name = string.Empty;
    private string _barcode = string.Empty;
    private decimal _price;
    private int _stock;
    private int _minStock = 5;
    private int _categoryId;
    private bool _isSelected;

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Barcode { get => _barcode; set { SetProperty(ref _barcode, value); OnPropertyChanged(nameof(BarcodeDisplay)); } }
    public string BarcodeDisplay => string.IsNullOrEmpty(Barcode) ? "N/A" : Barcode;
    public decimal Price { get => _price; set => SetProperty(ref _price, value); }
    public int Stock { get => _stock; set => SetProperty(ref _stock, value); }
    public int MinStock { get => _minStock; set => SetProperty(ref _minStock, value); }
    public int CategoryId { get => _categoryId; set => SetProperty(ref _categoryId, value); }
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}