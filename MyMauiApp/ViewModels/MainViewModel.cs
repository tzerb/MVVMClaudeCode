using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMauiApp.Models;
using MyMauiApp.Services;

namespace MyMauiApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PersonService _personService;

    [ObservableProperty]
    private string _greeting = "People Manager";

    public ObservableCollection<Person> People => _personService.People;

    public MainViewModel(PersonService personService)
    {
        _personService = personService;
    }

    [RelayCommand]
    private async Task GoToSettings()
    {
        await Shell.Current.GoToAsync("settings");
    }

    [RelayCommand]
    private async Task AddPerson()
    {
        await Shell.Current.GoToAsync("personedit?personId=new");
    }

    [RelayCommand]
    private async Task EditPerson(Person person)
    {
        await Shell.Current.GoToAsync($"personedit?personId={person.Id}");
    }
}
