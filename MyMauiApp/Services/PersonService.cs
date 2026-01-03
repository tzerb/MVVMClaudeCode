using System.Collections.ObjectModel;
using MyMauiApp.Models;
using MyMauiApp.Helpers;

namespace MyMauiApp.Services;

public class PersonService
{
    public ObservableCollection<Person> People { get; } = [];

    public PersonService()
    {
        // Add some sample data
        AddPerson(new Person { Name = "John Doe", Email = "john@example.com" });
        AddPerson(new Person { Name = "Jane Smith", Email = "jane@example.com" });
    }

    public Person? GetPersonById(Guid id)
    {
        return People.FirstOrDefault(p => p.Id == id);
    }

    public (bool Success, string? Error) AddPerson(Person person)
    {
        var validationError = ValidatePerson(person, isNew: true);
        if (validationError != null)
        {
            return (false, validationError);
        }

        People.Add(person);
        return (true, null);
    }

    public (bool Success, string? Error) UpdatePerson(Person person)
    {
        var existing = People.FirstOrDefault(p => p.Id == person.Id);
        if (existing == null)
        {
            return (false, "Person not found.");
        }

        var validationError = ValidatePerson(person, isNew: false);
        if (validationError != null)
        {
            return (false, validationError);
        }

        existing.Name = person.Name;
        existing.Email = person.Email;

        return (true, null);
    }

    public bool DeletePerson(Guid id)
    {
        var person = People.FirstOrDefault(p => p.Id == id);
        if (person == null)
        {
            return false;
        }

        People.Remove(person);
        return true;
    }

    public bool IsNameDuplicate(string name, Guid? excludeId = null)
    {
        return People.Any(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
    }

    public bool IsEmailDuplicate(string email, Guid? excludeId = null)
    {
        return People.Any(p =>
            p.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
    }

    private string? ValidatePerson(Person person, bool isNew)
    {
        if (string.IsNullOrWhiteSpace(person.Name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(person.Email))
        {
            return "Email is required.";
        }

        if (!ValidationHelper.IsValidEmail(person.Email))
        {
            return "Email format is invalid.";
        }

        var excludeId = isNew ? null : (Guid?)person.Id;

        if (IsNameDuplicate(person.Name, excludeId))
        {
            return "A person with this name already exists.";
        }

        if (IsEmailDuplicate(person.Email, excludeId))
        {
            return "A person with this email already exists.";
        }

        return null;
    }
}
