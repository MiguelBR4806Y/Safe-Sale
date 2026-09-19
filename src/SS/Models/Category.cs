using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class Category : ObservableObject
{
    private int _id;
    private string _name = string.Empty;
    private string _icon = "";
    private string _color = "#7B1FA2";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
    public string Color { get => _color; set => SetProperty(ref _color, value); }
}
