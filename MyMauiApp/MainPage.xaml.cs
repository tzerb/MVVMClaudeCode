using MyMauiApp.ViewModels;

namespace MyMauiApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<MainViewModel>();
    }
}
