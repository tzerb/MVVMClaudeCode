using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMauiApp.Services;

namespace MyMauiApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _currentThemeText = "Light";

    public SettingsViewModel(IThemeService themeService, INavigationService navigationService)
    {
        _themeService = themeService;
        _navigationService = navigationService;

        // Initialize from current app theme
        IsDarkMode = _themeService.IsDarkMode;
        UpdateThemeText();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        _themeService.SetTheme(value);
        UpdateThemeText();
    }

    private void UpdateThemeText()
    {
        CurrentThemeText = IsDarkMode ? "Dark" : "Light";
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await _navigationService.GoBackAsync();
    }
}
