using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class SaleItem : ObservableObject
{
    private int _id;
    private int _saleId;
    private int _productId;
    private string _productName = string.Empty;
    private int _quantity;
    private decimal _priceSold;

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public int SaleId { get => _saleId; set => SetProperty(ref _saleId, value); }
    public int ProductId { get => _productId; set => SetProperty(ref _productId, value); }
    public string ProductName { get => _productName; set => SetProperty(ref _productName, value); }
    public int Quantity { get => _quantity; set => SetProperty(ref _quantity, value); }
    public decimal PriceSold { get => _priceSold; set => SetProperty(ref _priceSold, value); }
    public decimal Subtotal => Quantity * PriceSold;
}
