# Project Conventions for Claude

## Testing Guidelines

### Avoid Global Objects in ViewModels
ViewModels should NOT directly reference global/static objects like:
- `Application.Current`
- `Shell.Current`
- `MainThread`

Instead, inject abstractions via constructor dependency injection:
- `INavigationService` - for navigation (wraps Shell.Current.GoToAsync)
- `IThemeService` - for theme management (wraps Application.Current.UserAppTheme)
- `IDialogService` - for alerts/dialogs (wraps DisplayAlert)

This enables:
1. Unit testing without MAUI runtime
2. Mocking navigation and UI interactions
3. Better separation of concerns

### Service Interfaces Location
Service interfaces are in: `MyMauiApp/Services/`
- `INavigationService.cs`
- `IThemeService.cs`
- `IDialogService.cs`

### Test Project
Test project: `MyMauiApp.Tests`
- Uses xUnit
- Uses Moq for mocking
- Tests should not require Application.Current or Shell.Current
