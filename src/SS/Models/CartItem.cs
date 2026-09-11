using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class CartItem : ObservableObject
{
    private Product _product = null!;
    private int _quantity = 1;

    public Product Product { get => _product; set { SetProperty(ref _product, value); OnPropertyChanged(nameof(Subtotal)); OnPropertyChanged(nameof(ProductName)); OnPropertyChanged(nameof(Price)); } }
    public int Quantity { get => _quantity; set { SetProperty(ref _quantity, value); OnPropertyChanged(nameof(Subtotal)); } }
    public string ProductName => Product?.Name ?? string.Empty;
    public decimal Price => Product?.Price ?? 0;
    public decimal Subtotal => Price * Quantity;
}
