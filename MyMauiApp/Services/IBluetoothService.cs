namespace MyMauiApp.Services;

public interface IBluetoothService
{
    event EventHandler<string>? StatusChanged;
    event EventHandler<bool>? ConnectionChanged;

    bool IsConnected { get; }
    bool IsScanning { get; }
    string Status { get; }

    Task<bool> StartScanningAsync();
    Task StopScanningAsync();
    Task<bool> ConnectToArduinoAsync();
    Task DisconnectAsync();
    Task<bool> SetServoAsync(int angle);
}
