using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class Sale : ObservableObject
{
    private int _id;
    private int _userId;
    private decimal _total;
    private DateTime _createdAt = DateTime.Now;

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public int UserId { get => _userId; set => SetProperty(ref _userId, value); }
    public decimal Total { get => _total; set => SetProperty(ref _total, value); }
    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
}
