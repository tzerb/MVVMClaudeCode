namespace MyMauiApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Create MainPage AFTER InitializeComponent() so resources are available
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        return new Window(mainPage);
    }
}
