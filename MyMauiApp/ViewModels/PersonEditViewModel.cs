using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMauiApp.Models;
using MyMauiApp.Services;
using MyMauiApp.Helpers;

namespace MyMauiApp.ViewModels;

[QueryProperty(nameof(PersonId), "personId")]
public partial class PersonEditViewModel : ObservableObject
{
    private readonly PersonService _personService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private Guid? _originalPersonId;
    private bool _isNewPerson;

    [ObservableProperty]
    private string _personId = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _pageTitle = "Add Person";

    [ObservableProperty]
    private string _nameError = string.Empty;

    [ObservableProperty]
    private string _emailError = string.Empty;

    [ObservableProperty]
    private bool _hasNameError;

    [ObservableProperty]
    private bool _hasEmailError;

    [ObservableProperty]
    private bool _canDelete;

    public PersonEditViewModel(PersonService personService, INavigationService navigationService, IDialogService dialogService)
    {
        _personService = personService;
        _navigationService = navigationService;
        _dialogService = dialogService;
    }

    partial void OnPersonIdChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "new")
        {
            _isNewPerson = true;
            _originalPersonId = null;
            PageTitle = "Add Person";
            Name = string.Empty;
            Email = string.Empty;
            CanDelete = false;
        }
        else if (Guid.TryParse(value, out var id))
        {
            _isNewPerson = false;
            _originalPersonId = id;
            var person = _personService.GetPersonById(id);
            if (person != null)
            {
                PageTitle = "Edit Person";
                Name = person.Name;
                Email = person.Email;
                CanDelete = true;
            }
        }

        ClearErrors();
    }

    partial void OnNameChanged(string value)
    {
        ValidateName();
    }

    partial void OnEmailChanged(string value)
    {
        ValidateEmail();
    }

    private void ValidateName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            NameError = "Name is required.";
            HasNameError = true;
        }
        else if (_personService.IsNameDuplicate(Name, _originalPersonId))
        {
            NameError = "A person with this name already exists.";
            HasNameError = true;
        }
        else
        {
            NameError = string.Empty;
            HasNameError = false;
        }
    }

    private void ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailError = "Email is required.";
            HasEmailError = true;
        }
        else if (!ValidationHelper.IsValidEmail(Email))
        {
            EmailError = "Email format is invalid.";
            HasEmailError = true;
        }
        else if (_personService.IsEmailDuplicate(Email, _originalPersonId))
        {
            EmailError = "A person with this email already exists.";
            HasEmailError = true;
        }
        else
        {
            EmailError = string.Empty;
            HasEmailError = false;
        }
    }

    private void ClearErrors()
    {
        NameError = string.Empty;
        EmailError = string.Empty;
        HasNameError = false;
        HasEmailError = false;
    }

    private bool ValidateAll()
    {
        ValidateName();
        ValidateEmail();
        return !HasNameError && !HasEmailError;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!ValidateAll())
        {
            return;
        }

        if (_isNewPerson)
        {
            var person = new Person
            {
                Name = Name.Trim(),
                Email = Email.Trim()
            };

            var (success, error) = _personService.AddPerson(person);
            if (!success)
            {
                await _dialogService.DisplayAlertAsync("Error", error ?? "An error occurred", "OK");
                return;
            }
        }
        else if (_originalPersonId.HasValue)
        {
            var person = new Person
            {
                Id = _originalPersonId.Value,
                Name = Name.Trim(),
                Email = Email.Trim()
            };

            var (success, error) = _personService.UpdatePerson(person);
            if (!success)
            {
                await _dialogService.DisplayAlertAsync("Error", error ?? "An error occurred", "OK");
                return;
            }
        }

        await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!_originalPersonId.HasValue)
        {
            return;
        }

        var confirm = await _dialogService.DisplayConfirmAsync(
            "Confirm Delete",
            $"Are you sure you want to delete {Name}?",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        _personService.DeletePerson(_originalPersonId.Value);
        await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await _navigationService.GoBackAsync();
    }
}
