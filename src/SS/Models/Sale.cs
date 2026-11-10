using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class Sale : ObservableObject
{
    private int _id;
    private int _userId;
    private string _userName = "";
    private decimal _subtotal;
    private decimal _ivaRate;
    private decimal _ivaAmount;
    private decimal _total;
    private string _paymentMethod = "";
    private DateTime _createdAt = DateTime.Now;
    private int _lineCount;
    private int _unitsCount;

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public int UserId { get => _userId; set => SetProperty(ref _userId, value); }
    public string UserName { get => _userName; set => SetProperty(ref _userName, value); }
    public decimal Subtotal { get => _subtotal; set => SetProperty(ref _subtotal, value); }
    public decimal IvaRate { get => _ivaRate; set => SetProperty(ref _ivaRate, value); }
    public decimal IvaAmount { get => _ivaAmount; set => SetProperty(ref _ivaAmount, value); }
    public decimal Total { get => _total; set => SetProperty(ref _total, value); }
    public string PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }
    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
    public int LineCount { get => _lineCount; set => SetProperty(ref _lineCount, value); }
    public int UnitsCount { get => _unitsCount; set => SetProperty(ref _unitsCount, value); }

    public bool HasIva => IvaAmount > 0;
    public string SummaryLabel => LineCount == 1
        ? $"1 producto · {UnitsCount} unidades"
        : $"{LineCount} productos · {UnitsCount} unidades";
}
