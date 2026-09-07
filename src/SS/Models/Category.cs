using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class Category : ObservableObject
{
    private int _id;
    private string _name = string.Empty;
    private string? _description;

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string? Description { get => _description; set => SetProperty(ref _description, value); }
}