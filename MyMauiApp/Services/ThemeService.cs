namespace MyMauiApp.Services;

public class ThemeService : IThemeService
{
    public bool IsDarkMode
    {
        get
        {
            var app = Application.Current;
            if (app == null) return false;

            return app.UserAppTheme == AppTheme.Dark ||
                   (app.UserAppTheme == AppTheme.Unspecified && app.RequestedTheme == AppTheme.Dark);
        }
    }

    public void SetTheme(bool isDarkMode)
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
        }
    }
}
