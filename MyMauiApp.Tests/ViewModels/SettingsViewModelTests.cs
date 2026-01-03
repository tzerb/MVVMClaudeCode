using MyMauiApp.ViewModels;

namespace MyMauiApp.Tests.ViewModels;

public class SettingsViewModelTests
{
    [Fact]
    public void Constructor_SetsDefaultThemeText()
    {
        // Arrange & Act
        var viewModel = new SettingsViewModel();

        // Assert - Default should be Light when Application.Current is null
        Assert.Equal("Light", viewModel.CurrentThemeText);
    }

    [Fact]
    public void Constructor_SetsIsDarkModeToFalse_WhenNoApplication()
    {
        // Arrange & Act
        var viewModel = new SettingsViewModel();

        // Assert - Default should be false when Application.Current is null
        Assert.False(viewModel.IsDarkMode);
    }

    [Fact]
    public void IsDarkMode_WhenSetToTrue_UpdatesCurrentThemeText()
    {
        // Arrange
        var viewModel = new SettingsViewModel();

        // Act
        viewModel.IsDarkMode = true;

        // Assert
        Assert.Equal("Dark", viewModel.CurrentThemeText);
    }

    [Fact]
    public void IsDarkMode_WhenSetToFalse_UpdatesCurrentThemeText()
    {
        // Arrange
        var viewModel = new SettingsViewModel();
        viewModel.IsDarkMode = true; // First set to true

        // Act
        viewModel.IsDarkMode = false;

        // Assert
        Assert.Equal("Light", viewModel.CurrentThemeText);
    }

    [Fact]
    public void IsDarkMode_TogglingMultipleTimes_UpdatesThemeTextCorrectly()
    {
        // Arrange
        var viewModel = new SettingsViewModel();

        // Act & Assert
        viewModel.IsDarkMode = true;
        Assert.Equal("Dark", viewModel.CurrentThemeText);

        viewModel.IsDarkMode = false;
        Assert.Equal("Light", viewModel.CurrentThemeText);

        viewModel.IsDarkMode = true;
        Assert.Equal("Dark", viewModel.CurrentThemeText);
    }

    [Fact]
    public void GoBackCommand_IsNotNull()
    {
        // Arrange
        var viewModel = new SettingsViewModel();

        // Assert
        Assert.NotNull(viewModel.GoBackCommand);
    }

    [Fact]
    public void CurrentThemeText_InitialValue_IsLight()
    {
        // Arrange & Act
        var viewModel = new SettingsViewModel();

        // Assert
        Assert.Equal("Light", viewModel.CurrentThemeText);
    }
}
