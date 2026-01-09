using MyMauiApp.ViewModels;

namespace MyMauiApp.Views;

public partial class ArduinoPage : ContentPage
{
    public ArduinoPage(ArduinoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
