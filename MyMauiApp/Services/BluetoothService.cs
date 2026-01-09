using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace MyMauiApp.Services;

public class BluetoothService : IBluetoothService
{
    // Arduino BLE UUIDs from BluetoothSimpleService.ino
    private static readonly Guid ServiceUuid = Guid.Parse("19B10000-E8F2-537E-4F6C-D104768A1214");
    private static readonly Guid ServoCharacteristicUuid = Guid.Parse("19B10002-E8F2-537E-4F6C-D104768A1214");
    private const string ArduinoDeviceName = "R4 LED";

    private readonly IBluetoothLE _bluetoothLE;
    private readonly IAdapter _adapter;
    private IDevice? _connectedDevice;
    private ICharacteristic? _servoCharacteristic;
    private string _status = "Not connected";
    private bool _isScanning;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<bool>? ConnectionChanged;

    public bool IsConnected => _connectedDevice != null;
    public bool IsScanning => _isScanning;
    public string Status => _status;

    public BluetoothService()
    {
        _bluetoothLE = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        _adapter.DeviceConnected += OnDeviceConnected;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
        _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
    }

    private void SetStatus(string status)
    {
        _status = status;
        StatusChanged?.Invoke(this, status);
    }

    private void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        if (e.Device.Name == ArduinoDeviceName)
        {
            SetStatus($"Found {ArduinoDeviceName}");
        }
    }

    private void OnDeviceConnected(object? sender, DeviceEventArgs e)
    {
        SetStatus($"Connected to {e.Device.Name}");
        ConnectionChanged?.Invoke(this, true);
    }

    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        if (_connectedDevice != null && e.Device.Id == _connectedDevice.Id)
        {
            _connectedDevice = null;
            _servoCharacteristic = null;
            SetStatus("Disconnected");
            ConnectionChanged?.Invoke(this, false);
        }
    }

    private void OnDeviceConnectionLost(object? sender, DeviceErrorEventArgs e)
    {
        _connectedDevice = null;
        _servoCharacteristic = null;
        SetStatus("Connection lost");
        ConnectionChanged?.Invoke(this, false);
    }

    public async Task<bool> StartScanningAsync()
    {
        if (_bluetoothLE.State != BluetoothState.On)
        {
            SetStatus("Bluetooth is not enabled");
            return false;
        }

        _isScanning = true;
        SetStatus("Scanning for devices...");

        try
        {
            await _adapter.StartScanningForDevicesAsync();
            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"Scan error: {ex.Message}");
            return false;
        }
        finally
        {
            _isScanning = false;
        }
    }

    public async Task StopScanningAsync()
    {
        if (_isScanning)
        {
            await _adapter.StopScanningForDevicesAsync();
            _isScanning = false;
            SetStatus("Scan stopped");
        }
    }

    public async Task<bool> ConnectToArduinoAsync()
    {
        if (_bluetoothLE.State != BluetoothState.On)
        {
            SetStatus("Bluetooth is not enabled");
            return false;
        }

        SetStatus($"Scanning for {ArduinoDeviceName}...");

        IDevice? arduinoDevice = null;
        var deviceFoundTcs = new TaskCompletionSource<IDevice?>();

        void OnDeviceFound(object? s, DeviceEventArgs e)
        {
            if (e.Device.Name == ArduinoDeviceName)
            {
                arduinoDevice = e.Device;
                deviceFoundTcs.TrySetResult(e.Device);
            }
        }

        _adapter.DeviceDiscovered += OnDeviceFound;

        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Start scanning with service UUID filter for faster discovery
            var scanTask = _adapter.StartScanningForDevicesAsync(
                serviceUuids: [ServiceUuid],
                cancellationToken: cts.Token);

            // Wait for either device found or scan timeout
            var completedTask = await Task.WhenAny(
                deviceFoundTcs.Task,
                scanTask);

            // Stop scanning immediately once device is found
            if (arduinoDevice != null)
            {
                await _adapter.StopScanningForDevicesAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Scan timeout is expected
        }
        finally
        {
            _adapter.DeviceDiscovered -= OnDeviceFound;
        }

        if (arduinoDevice == null)
        {
            SetStatus($"{ArduinoDeviceName} not found");
            return false;
        }

        SetStatus($"Connecting to {ArduinoDeviceName}...");

        try
        {
            await _adapter.ConnectToDeviceAsync(arduinoDevice);
            _connectedDevice = arduinoDevice;

            // Get the service and characteristics
            var service = await arduinoDevice.GetServiceAsync(ServiceUuid);
            if (service == null)
            {
                SetStatus("BLE service not found");
                await DisconnectAsync();
                return false;
            }

            _servoCharacteristic = await service.GetCharacteristicAsync(ServoCharacteristicUuid);

            if (_servoCharacteristic == null)
            {
                SetStatus("BLE characteristics not found");
                await DisconnectAsync();
                return false;
            }

            SetStatus($"Connected to {ArduinoDeviceName}");
            ConnectionChanged?.Invoke(this, true);
            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"Connection error: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connectedDevice != null)
        {
            var device = _connectedDevice;

            try
            {
                await _adapter.DisconnectDeviceAsync(device);
                // OnDeviceDisconnected event handler will update state and fire ConnectionChanged
            }
            catch
            {
                // If disconnect fails, manually update state
                _connectedDevice = null;
                _servoCharacteristic = null;
                SetStatus("Disconnected");
                ConnectionChanged?.Invoke(this, false);
            }
        }
    }

    public async Task<bool> SetServoAsync(int angle)
    {
        if (_servoCharacteristic == null)
        {
            SetStatus("Not connected");
            return false;
        }

        // Clamp angle to valid range (10-180 degrees)
        angle = Math.Clamp(angle, 10, 180);

        // Convert angle to 0-9 value for Arduino (angle / 20)
        int servoValue = (int) Math.Ceiling((double)angle / 20.0);

        try
        {
            var value = new byte[] { (byte)servoValue };
            await _servoCharacteristic.WriteAsync(value);
            SetStatus($"Servo angle: {angle}°");
            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"Servo error: {ex.Message}");
            return false;
        }
    }
}
