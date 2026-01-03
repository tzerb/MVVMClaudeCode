using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyMauiApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _currentThemeText = "Light";

    public SettingsViewModel()
    {
        // Initialize from current app theme
        var currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
        IsDarkMode = currentTheme == AppTheme.Dark;
        UpdateThemeText();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }
        UpdateThemeText();
    }

    private void UpdateThemeText()
    {
        CurrentThemeText = IsDarkMode ? "Dark" : "Light";
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}
