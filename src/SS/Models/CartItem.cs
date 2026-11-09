using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class CartItem : ObservableObject
{
    private Product _product = null!;
    private int _quantity = 1;

    public Product Product { get => _product; set { SetProperty(ref _product, value); OnPropertyChanged(nameof(Subtotal)); OnPropertyChanged(nameof(ProductName)); OnPropertyChanged(nameof(Price)); OnPropertyChanged(nameof(PriceLabel)); OnPropertyChanged(nameof(CanIncrease)); } }
    public int Quantity { get => _quantity; set { SetProperty(ref _quantity, value); OnPropertyChanged(nameof(Subtotal)); OnPropertyChanged(nameof(CanIncrease)); OnPropertyChanged(nameof(CanDecrease)); } }
    public string ProductName => Product?.Name ?? string.Empty;
    public decimal Price => Product?.Price ?? 0;
    public decimal Subtotal => Price * Quantity;
    public string PriceLabel => $"${Price:F2} c/u";
    public bool CanIncrease => Product != null && Quantity < Product.Stock;
    public bool CanDecrease => Quantity > 1;
}
