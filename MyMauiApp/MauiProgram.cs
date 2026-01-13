using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MyMauiApp.Services;
using MyMauiApp.ViewModels;
using MyMauiApp.Views;

namespace MyMauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services
        builder.Services.AddSingleton<PersonService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
        builder.Services.AddSingleton<IBatteryService, BatteryService>();

        // Register ViewModels
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<PersonEditViewModel>();
        builder.Services.AddTransient<ArduinoViewModel>();
        builder.Services.AddTransient<BatteryViewModel>();

        // Register Views
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<PersonEditPage>();
        builder.Services.AddTransient<ArduinoPage>();
        builder.Services.AddTransient<BatteryPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
