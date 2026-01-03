namespace MyMauiApp.Services;

public class DialogService : IDialogService
{
    public async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page != null)
        {
            await page.DisplayAlert(title, message, cancel);
        }
    }

    public async Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page != null)
        {
            return await page.DisplayAlert(title, message, accept, cancel);
        }
        return false;
    }
}
