using MyMauiApp.ViewModels;

namespace MyMauiApp.Views;

public partial class PersonEditPage : ContentPage
{
    public PersonEditPage(PersonEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
