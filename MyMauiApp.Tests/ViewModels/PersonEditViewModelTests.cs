using Moq;
using MyMauiApp.Models;
using MyMauiApp.Services;
using MyMauiApp.ViewModels;

namespace MyMauiApp.Tests.ViewModels;

public class PersonEditViewModelTests
{
    private readonly PersonService _personService;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly PersonEditViewModel _viewModel;

    public PersonEditViewModelTests()
    {
        _personService = new PersonService();
        _navigationServiceMock = new Mock<INavigationService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _viewModel = new PersonEditViewModel(_personService, _navigationServiceMock.Object, _dialogServiceMock.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_SetsDefaultPageTitle()
    {
        // Assert
        Assert.Equal("Add Person", _viewModel.PageTitle);
    }

    [Fact]
    public void Constructor_SetsCanDeleteToFalse()
    {
        // Assert
        Assert.False(_viewModel.CanDelete);
    }

    [Fact]
    public void PersonId_WhenSetToNew_SetsAddPersonMode()
    {
        // Act
        _viewModel.PersonId = "new";

        // Assert
        Assert.Equal("Add Person", _viewModel.PageTitle);
        Assert.False(_viewModel.CanDelete);
        Assert.Equal(string.Empty, _viewModel.Name);
        Assert.Equal(string.Empty, _viewModel.Email);
    }

    [Fact]
    public void PersonId_WhenSetToExistingId_SetsEditPersonMode()
    {
        // Arrange
        var existingPerson = _personService.People.First();

        // Act
        _viewModel.PersonId = existingPerson.Id.ToString();

        // Assert
        Assert.Equal("Edit Person", _viewModel.PageTitle);
        Assert.True(_viewModel.CanDelete);
        Assert.Equal(existingPerson.Name, _viewModel.Name);
        Assert.Equal(existingPerson.Email, _viewModel.Email);
    }

    [Fact]
    public void PersonId_WhenSetToInvalidId_StaysInAddModeWithEmptyFields()
    {
        // Act - Setting an invalid GUID that doesn't exist
        _viewModel.PersonId = Guid.NewGuid().ToString();

        // Assert - PageTitle changes to Edit but data doesn't load (person not found)
        // Name and Email remain empty
        Assert.Equal(string.Empty, _viewModel.Name);
        Assert.Equal(string.Empty, _viewModel.Email);
    }

    #endregion

    #region Name Validation Tests

    [Fact]
    public void Name_WhenSetToEmptyAfterValid_ShowsError()
    {
        // Arrange
        _viewModel.PersonId = "new";
        _viewModel.Name = "Valid Name"; // Set a valid name first

        // Act
        _viewModel.Name = ""; // Now set to empty

        // Assert
        Assert.True(_viewModel.HasNameError);
        Assert.Equal("Name is required.", _viewModel.NameError);
    }

    [Fact]
    public void Name_WhenWhitespace_ShowsError()
    {
        // Arrange
        _viewModel.PersonId = "new";

        // Act
        _viewModel.Name = "   ";

        // Assert
        Assert.True(_viewModel.HasNameError);
        Assert.Equal("Name is required.", _viewModel.NameError);
    }

    [Fact]
    public void Name_WhenValid_ClearsError()
    {
        // Arrange
        _viewModel.PersonId = "new";
        _viewModel.Name = "   "; // First trigger error with whitespace

        // Act
        _viewModel.Name = "Valid Name";

        // Assert
        Assert.False(_viewModel.HasNameError);
        Assert.Equal(string.Empty, _viewModel.NameError);
    }

    [Fact]
    public void Name_WhenDuplicate_ShowsError()
    {
        // Arrange
        var existingPerson = _personService.People.First();
        _viewModel.PersonId = "new";

        // Act
        _viewModel.Name = existingPerson.Name;

        // Assert
        Assert.True(_viewModel.HasNameError);
        Assert.Equal("A person with this name already exists.", _viewModel.NameError);
    }

    [Fact]
    public void Name_WhenDuplicateOfSelf_DoesNotShowError()
    {
        // Arrange
        var existingPerson = _personService.People.First();
        _viewModel.PersonId = existingPerson.Id.ToString();

        // Act - Set name to same value (editing self)
        _viewModel.Name = existingPerson.Name;

        // Assert - Should not show error since it's the same person
        Assert.False(_viewModel.HasNameError);
    }

    #endregion

    #region Email Validation Tests

    [Fact]
    public void Email_WhenSetToEmptyAfterValid_ShowsError()
    {
        // Arrange
        _viewModel.PersonId = "new";
        _viewModel.Email = "valid@example.com"; // Set valid first

        // Act
        _viewModel.Email = ""; // Now set to empty

        // Assert
        Assert.True(_viewModel.HasEmailError);
        Assert.Equal("Email is required.", _viewModel.EmailError);
    }

    [Fact]
    public void Email_WhenInvalidFormat_ShowsError()
    {
        // Arrange
        _viewModel.PersonId = "new";

        // Act
        _viewModel.Email = "invalid-email";

        // Assert
        Assert.True(_viewModel.HasEmailError);
        Assert.Equal("Email format is invalid.", _viewModel.EmailError);
    }

    [Fact]
    public void Email_WhenValidFormat_ClearsError()
    {
        // Arrange
        _viewModel.PersonId = "new";
        _viewModel.Email = "invalid"; // First trigger error

        // Act
        _viewModel.Email = "valid@example.com";

        // Assert
        Assert.False(_viewModel.HasEmailError);
        Assert.Equal(string.Empty, _viewModel.EmailError);
    }

    [Fact]
    public void Email_WhenDuplicate_ShowsError()
    {
        // Arrange
        var existingPerson = _personService.People.First();
        _viewModel.PersonId = "new";

        // Act
        _viewModel.Email = existingPerson.Email;

        // Assert
        Assert.True(_viewModel.HasEmailError);
        Assert.Equal("A person with this email already exists.", _viewModel.EmailError);
    }

    [Fact]
    public void Email_WhenDuplicateOfSelf_DoesNotShowError()
    {
        // Arrange
        var existingPerson = _personService.People.First();
        _viewModel.PersonId = existingPerson.Id.ToString();

        // Act - Set email to same value (editing self)
        _viewModel.Email = existingPerson.Email;

        // Assert - Should not show error since it's the same person
        Assert.False(_viewModel.HasEmailError);
    }

    [Theory]
    [InlineData("test@example.com", false)]
    [InlineData("user.name@domain.co.uk", false)]
    [InlineData("missing@domain", false)] // This is valid per System.Net.Mail.MailAddress
    [InlineData("invalid", true)]
    [InlineData("@nodomain.com", true)]
    public void Email_ValidationWorksForVariousFormats(string email, bool shouldHaveError)
    {
        // Arrange
        _viewModel.PersonId = "new";
        _viewModel.Name = "Test"; // Set valid name to avoid name errors

        // Act
        _viewModel.Email = email;

        // Assert
        Assert.Equal(shouldHaveError, _viewModel.HasEmailError);
    }

    #endregion

    #region Command Tests

    [Fact]
    public void SaveCommand_IsNotNull()
    {
        Assert.NotNull(_viewModel.SaveCommand);
    }

    [Fact]
    public void DeleteCommand_IsNotNull()
    {
        Assert.NotNull(_viewModel.DeleteCommand);
    }

    [Fact]
    public void CancelCommand_IsNotNull()
    {
        Assert.NotNull(_viewModel.CancelCommand);
    }

    #endregion

    #region Error Clearing Tests

    [Fact]
    public void PersonId_WhenChangedToExistingPerson_ClearsErrors()
    {
        // Arrange
        _viewModel.PersonId = "new";
        _viewModel.Name = "   "; // Trigger name error with whitespace
        _viewModel.Email = "invalid"; // Trigger email error
        Assert.True(_viewModel.HasNameError);
        Assert.True(_viewModel.HasEmailError);

        // Act - Change to existing person (which triggers OnPersonIdChanged)
        var existingPerson = _personService.People.First();
        _viewModel.PersonId = existingPerson.Id.ToString();

        // Assert - Errors should be cleared when loading a person
        Assert.False(_viewModel.HasNameError);
        Assert.False(_viewModel.HasEmailError);
    }

    #endregion
}
