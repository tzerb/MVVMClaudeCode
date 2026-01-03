# .NET MAUI MVVM Project Setup Guide

## Project Overview

Building a Windows desktop application with .NET MAUI and MVVM pattern, designed to extend to mobile platforms (iOS/Android) in the future.

## Technology Stack

- .NET MAUI (latest stable version)
- MVVM Community Toolkit
- C# 12+ / .NET 8+

## Project Structure

```
MyMauiApp/
├── Models/              # Data models
├── ViewModels/          # MVVM ViewModels
├── Views/               # XAML Views/Pages
├── Services/            # Business logic, data access
├── Resources/           # Images, fonts, styles
├── Platforms/           # Platform-specific code
└── MauiProgram.cs       # App configuration
```

## Initial Setup Commands

```bash
# Create new MAUI project
dotnet new maui -n MyMauiApp

# Add MVVM Community Toolkit
dotnet add package CommunityToolkit.Mvvm

# Add additional useful packages
dotnet add package CommunityToolkit.Maui
```

## MVVM Architecture Guidelines

### ViewModels

- Inherit from `ObservableObject` or use `[ObservableObject]` attribute
- Use `[ObservableProperty]` for bindable properties
- Use `[RelayCommand]` for command methods
- Keep UI-agnostic - no View references

### Views

- XAML files with code-behind
- Set BindingContext to ViewModel (via DI)
- Use data binding for all UI updates
- No business logic in code-behind

### Services

- Register in MauiProgram.cs
- Inject into ViewModels via constructor
- Keep platform-agnostic when possible

## Dependency Injection Setup

In `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<MainViewModel>();
builder.Services.AddSingleton<MainPage>();
// Add services and ViewModels here
```

## Next Steps

1. Create initial project structure
2. Set up MauiProgram.cs with DI
3. Create a sample MainViewModel with MVVM Toolkit
4. Create MainPage with data binding example
5. Implement basic navigation using Shell

## Windows-Specific Considerations

- Target Windows 10/11 (version 10.0.17763.0 or higher)
- Test with WinUI 3 controls
- Consider window sizing and desktop-specific UX

## Future Mobile Extension Notes

- Keep platform-specific code in Platforms/ folders
- Use dependency injection for platform services
- Design UI to be responsive (desktop + mobile)
- Test layouts at different screen sizes
