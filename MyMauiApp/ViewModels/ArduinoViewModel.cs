using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMauiApp.Services;

namespace MyMauiApp.ViewModels;

public partial class ArduinoViewModel : ObservableObject
{
    private readonly IBluetoothService _bluetoothService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _status = "Not connected";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private int _servoPosition = 10;

    public ArduinoViewModel(IBluetoothService bluetoothService, INavigationService navigationService)
    {
        _bluetoothService = bluetoothService;
        _navigationService = navigationService;

        _bluetoothService.StatusChanged += OnStatusChanged;
        _bluetoothService.ConnectionChanged += OnConnectionChanged;

        Status = _bluetoothService.Status;
        IsConnected = _bluetoothService.IsConnected;
    }

    private void OnStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() => Status = status);
    }

    private void OnConnectionChanged(object? sender, bool connected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsConnected = connected;
            IsConnecting = false;
        });
    }

    [RelayCommand]
    private async Task Connect()
    {
        if (IsConnected)
        {
            await _bluetoothService.DisconnectAsync();
        }
        else
        {
            IsConnecting = true;
            await _bluetoothService.ConnectToArduinoAsync();
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task SetServo(string positionStr)
    {
        if (!IsConnected) return;

        if (int.TryParse(positionStr, out int position))
        {
            ServoPosition = position;
            await _bluetoothService.SetServoAsync(position);
        }
    }

    partial void OnServoPositionChanged(int value)
    {
        if (IsConnected)
        {
            _ = _bluetoothService.SetServoAsync(value);
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await _navigationService.GoBackAsync();
    }
}
