using MyMauiApp.ViewModels;

namespace MyMauiApp.Views;

public partial class BatteryPage : ContentPage
{
    public BatteryPage(BatteryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
