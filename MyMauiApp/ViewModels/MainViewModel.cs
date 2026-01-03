using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyMauiApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _greeting = "Welcome to .NET MAUI with MVVM!";

    [RelayCommand]
    private void Greet()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Greeting = "Please enter your name.";
        }
        else
        {
            Greeting = $"Hello, {Name}! Welcome to .NET MAUI!";
        }
    }
}
