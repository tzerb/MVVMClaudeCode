using Moq;
using MyMauiApp.Services;
using MyMauiApp.ViewModels;

namespace MyMauiApp.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<IThemeService> _themeServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;

    public SettingsViewModelTests()
    {
        _themeServiceMock = new Mock<IThemeService>();
        _navigationServiceMock = new Mock<INavigationService>();
    }

    private SettingsViewModel CreateViewModel(bool isDarkMode = false)
    {
        _themeServiceMock.Setup(t => t.IsDarkMode).Returns(isDarkMode);
        return new SettingsViewModel(_themeServiceMock.Object, _navigationServiceMock.Object);
    }

    [Fact]
    public void Constructor_SetsDefaultThemeText()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(isDarkMode: false);

        // Assert
        Assert.Equal("Light", viewModel.CurrentThemeText);
    }

    [Fact]
    public void Constructor_SetsIsDarkModeFromService()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(isDarkMode: false);

        // Assert
        Assert.False(viewModel.IsDarkMode);
    }

    [Fact]
    public void Constructor_InitializesDarkModeFromService_WhenDark()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(isDarkMode: true);

        // Assert
        Assert.True(viewModel.IsDarkMode);
        Assert.Equal("Dark", viewModel.CurrentThemeText);
    }

    [Fact]
    public void IsDarkMode_WhenSetToTrue_UpdatesCurrentThemeText()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.IsDarkMode = true;

        // Assert
        Assert.Equal("Dark", viewModel.CurrentThemeText);
    }

    [Fact]
    public void IsDarkMode_WhenSetToTrue_CallsThemeService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.IsDarkMode = true;

        // Assert
        _themeServiceMock.Verify(t => t.SetTheme(true), Times.Once);
    }

    [Fact]
    public void IsDarkMode_WhenSetToFalse_UpdatesCurrentThemeText()
    {
        // Arrange
        var viewModel = CreateViewModel();
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
        var viewModel = CreateViewModel();

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
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.GoBackCommand);
    }

    [Fact]
    public async Task GoBackCommand_CallsNavigationService()
    {
        // Arrange
        _navigationServiceMock.Setup(n => n.GoBackAsync()).Returns(Task.CompletedTask);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert
        _navigationServiceMock.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Fact]
    public void CurrentThemeText_InitialValue_IsLight()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal("Light", viewModel.CurrentThemeText);
    }
}
