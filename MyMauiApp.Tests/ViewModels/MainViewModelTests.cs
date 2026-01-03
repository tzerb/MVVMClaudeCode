using Moq;
using MyMauiApp.Models;
using MyMauiApp.Services;
using MyMauiApp.ViewModels;

namespace MyMauiApp.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly PersonService _personService;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _personService = new PersonService();
        _navigationServiceMock = new Mock<INavigationService>();
        _viewModel = new MainViewModel(_personService, _navigationServiceMock.Object);
    }

    [Fact]
    public void Constructor_SetsDefaultGreeting()
    {
        // Assert
        Assert.Equal("People Manager", _viewModel.Greeting);
    }

    [Fact]
    public void People_ReturnsPeopleFromService()
    {
        // Assert - PersonService adds 2 sample people by default
        Assert.Equal(_personService.People.Count, _viewModel.People.Count);
    }

    [Fact]
    public void People_ReflectsServiceChanges_WhenPersonAdded()
    {
        // Arrange
        var initialCount = _viewModel.People.Count;
        var newPerson = new Person { Name = "Test Person", Email = "test@example.com" };

        // Act
        _personService.AddPerson(newPerson);

        // Assert
        Assert.Equal(initialCount + 1, _viewModel.People.Count);
    }

    [Fact]
    public void People_ReflectsServiceChanges_WhenPersonDeleted()
    {
        // Arrange
        var initialCount = _viewModel.People.Count;
        var personToDelete = _viewModel.People.First();

        // Act
        _personService.DeletePerson(personToDelete.Id);

        // Assert
        Assert.Equal(initialCount - 1, _viewModel.People.Count);
    }

    [Fact]
    public void Greeting_CanBeChanged()
    {
        // Arrange
        var newGreeting = "New Greeting";

        // Act
        _viewModel.Greeting = newGreeting;

        // Assert
        Assert.Equal(newGreeting, _viewModel.Greeting);
    }

    [Fact]
    public void EditPersonCommand_IsNotNull()
    {
        // Assert
        Assert.NotNull(_viewModel.EditPersonCommand);
    }

    [Fact]
    public void AddPersonCommand_IsNotNull()
    {
        // Assert
        Assert.NotNull(_viewModel.AddPersonCommand);
    }

    [Fact]
    public void GoToSettingsCommand_IsNotNull()
    {
        // Assert
        Assert.NotNull(_viewModel.GoToSettingsCommand);
    }
}
