using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMauiApp.Models;
using MyMauiApp.Services;

namespace MyMauiApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PersonService _personService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _greeting = "People Manager";

    public ObservableCollection<Person> People => _personService.People;

    public MainViewModel(PersonService personService, INavigationService navigationService)
    {
        _personService = personService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task GoToSettings()
    {
        await _navigationService.GoToAsync("settings");
    }

    [RelayCommand]
    private async Task GoToArduino()
    {
        await _navigationService.GoToAsync("arduino");
    }

    [RelayCommand]
    private async Task GoToBattery()
    {
        await _navigationService.GoToAsync("battery");
    }

    [RelayCommand]
    private async Task AddPerson()
    {
        await _navigationService.GoToAsync("personedit?personId=new");
    }

    [RelayCommand]
    private async Task EditPerson(Person person)
    {
        await _navigationService.GoToAsync($"personedit?personId={person.Id}");
    }
}
