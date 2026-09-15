using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class User : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _passwordHash = "";

    [ObservableProperty]
    private string _role = "";

    public DateTime CreatedAt { get; set; }
}
