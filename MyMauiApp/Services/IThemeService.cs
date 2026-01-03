namespace MyMauiApp.Services;

public interface IThemeService
{
    bool IsDarkMode { get; }
    void SetTheme(bool isDarkMode);
}
