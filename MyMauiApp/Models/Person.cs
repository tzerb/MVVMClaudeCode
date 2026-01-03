using CommunityToolkit.Mvvm.ComponentModel;

namespace MyMauiApp.Models;

public partial class Person : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;
}
