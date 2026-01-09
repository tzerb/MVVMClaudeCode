using MyMauiApp.Views;

namespace MyMauiApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("personedit", typeof(PersonEditPage));
        Routing.RegisterRoute("arduino", typeof(ArduinoPage));
    }
}
