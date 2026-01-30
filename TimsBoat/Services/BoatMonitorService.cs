using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace TimsBoat.Services;

public class BoatMonitorService : IBoatMonitorService
{
    private const string DeviceName = "BoatMonitor";
    private const string LogSource = "BoatMonitor";

    // BLE UUIDs (16-bit format expanded to 128-bit)
    private static readonly Guid ServiceUuid = Guid.Parse("000000FF-0000-1000-8000-00805F9B34FB");
    private static readonly Guid LedOnOffUuid = Guid.Parse("0000FF01-0000-1000-8000-00805F9B34FB");
    private static readonly Guid BlinkRateUuid = Guid.Parse("0000FF02-0000-1000-8000-00805F9B34FB");
    private static readonly Guid AnalogInputsUuid = Guid.Parse("0000FF03-0000-1000-8000-00805F9B34FB");
    private static readonly Guid LedStripUuid = Guid.Parse("0000FF04-0000-1000-8000-00805F9B34FB");

    private readonly BleConnectionManager _connectionManager;
    private readonly IAdapter _adapter;
    private IDevice? _connectedDevice;
    private IService? _service;
    private ICharacteristic? _ledOnOffChar;
    private ICharacteristic? _blinkRateChar;
    private ICharacteristic? _analogInputsChar;
    private ICharacteristic? _ledStripChar;
    private string _status = "Not connected";
    private Guid? _deviceId;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<bool>? ConnectionChanged;
    public event EventHandler<BoatMonitorData>? DataReceived;

    public bool IsConnected => _connectedDevice != null;
    public string Status => _status;

    public BoatMonitorService()
    {
        _connectionManager = BleConnectionManager.Instance;
        _adapter = _connectionManager.Adapter;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
        _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
    }

    private void SetStatus(string status)
    {
        _status = status;
        StatusChanged?.Invoke(this, status);
    }

    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        BleLogger.Log(LogSource, $"OnDeviceDisconnected: {e.Device.Name ?? "(null)"} ID={e.Device.Id}");
        if (_connectedDevice != null && e.Device.Id == _connectedDevice.Id)
        {
            BleLogger.Log(LogSource, "Device disconnected - cleaning up");
            CleanupConnection();
            SetStatus("Disconnected");
            ConnectionChanged?.Invoke(this, false);
        }
    }

    private void OnDeviceConnectionLost(object? sender, DeviceErrorEventArgs e)
    {
        BleLogger.Log(LogSource, $"OnDeviceConnectionLost: {e.Device.Name ?? "(null)"} ID={e.Device.Id}");
        if (e.ErrorMessage != null)
        {
            BleLogger.LogError(LogSource, $"Connection lost error: {e.ErrorMessage}");
        }
        if (_connectedDevice != null && e.Device.Id == _connectedDevice.Id)
        {
            BleLogger.Log(LogSource, "Device connection lost - cleaning up");
            CleanupConnection();
            SetStatus("Connection lost");
            ConnectionChanged?.Invoke(this, false);
        }
    }

    private void CleanupConnection()
    {
        _connectedDevice = null;
        _service = null;
        _ledOnOffChar = null;
        _blinkRateChar = null;
        _analogInputsChar = null;
        _ledStripChar = null;
    }

    public async Task<bool> ConnectAsync()
    {
        BleLogger.Log(LogSource, "ConnectAsync started");

        if (!_connectionManager.IsBluetoothOn)
        {
            BleLogger.Log(LogSource, "Bluetooth is not enabled");
            SetStatus("Bluetooth is not enabled");
            return false;
        }

        BleLogger.Log(LogSource, $"Bluetooth state: {_connectionManager.IsBluetoothOn}");
        SetStatus($"Scanning for {DeviceName}...");

        IDevice? device = null;
        var deviceFoundTcs = new TaskCompletionSource<IDevice?>();

        void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
        {
            BleLogger.Log(LogSource, $"Discovered: '{e.Device.Name ?? "(null)"}' ID={e.Device.Id}");
            if (e.Device.Name == DeviceName)
            {
                BleLogger.Log(LogSource, $"Found target device: {DeviceName}");
                deviceFoundTcs.TrySetResult(e.Device);
            }
        }

        _adapter.DeviceDiscovered += OnDeviceDiscovered;

        try
        {
            BleLogger.Log(LogSource, "Starting BLE scan (15s timeout)");
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var scanTask = _adapter.StartScanningForDevicesAsync(cancellationToken: cts.Token);

            var completedTask = await Task.WhenAny(deviceFoundTcs.Task, scanTask);

            if (deviceFoundTcs.Task.IsCompleted)
            {
                device = await deviceFoundTcs.Task;
                BleLogger.Log(LogSource, "Device found, stopping scan");
                await _adapter.StopScanningForDevicesAsync();
            }
            else
            {
                BleLogger.Log(LogSource, "Scan completed without finding device");
            }
        }
        catch (OperationCanceledException)
        {
            BleLogger.Log(LogSource, "Scan timed out");
        }
        catch (Exception ex)
        {
            BleLogger.LogError(LogSource, "Scan error", ex);
        }
        finally
        {
            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
        }

        if (device == null)
        {
            BleLogger.Log(LogSource, $"{DeviceName} not found after scan");
            SetStatus($"{DeviceName} not found");
            return false;
        }

        _deviceId = device.Id;
        BleLogger.Log(LogSource, $"Device ID: {_deviceId}");
        SetStatus($"Connecting to {DeviceName}...");

        try
        {
            BleLogger.Log(LogSource, "Calling ConnectToDeviceAsync...");
            await _adapter.ConnectToDeviceAsync(device);
            BleLogger.Log(LogSource, "ConnectToDeviceAsync completed successfully");
            _connectedDevice = device;

            BleLogger.Log(LogSource, $"Getting service {ServiceUuid}...");
            _service = await device.GetServiceAsync(ServiceUuid);
            if (_service == null)
            {
                BleLogger.LogError(LogSource, "BoatMonitor service not found");

                // Log all available services for debugging
                var services = await device.GetServicesAsync();
                BleLogger.Log(LogSource, $"Available services ({services.Count}):");
                foreach (var svc in services)
                {
                    BleLogger.Log(LogSource, $"  - {svc.Id}");
                }

                SetStatus("BoatMonitor service not found");
                await DisconnectAsync();
                return false;
            }
            BleLogger.Log(LogSource, "Service found");

            BleLogger.Log(LogSource, "Getting characteristics...");
            _ledOnOffChar = await _service.GetCharacteristicAsync(LedOnOffUuid);
            BleLogger.Log(LogSource, $"LED On/Off char: {(_ledOnOffChar != null ? "found" : "NOT FOUND")}");

            _blinkRateChar = await _service.GetCharacteristicAsync(BlinkRateUuid);
            BleLogger.Log(LogSource, $"Blink Rate char: {(_blinkRateChar != null ? "found" : "NOT FOUND")}");

            _analogInputsChar = await _service.GetCharacteristicAsync(AnalogInputsUuid);
            BleLogger.Log(LogSource, $"Analog Inputs char: {(_analogInputsChar != null ? "found" : "NOT FOUND")}");

            _ledStripChar = await _service.GetCharacteristicAsync(LedStripUuid);
            BleLogger.Log(LogSource, $"LED Strip char: {(_ledStripChar != null ? "found" : "NOT FOUND")}");

            if (_analogInputsChar == null)
            {
                BleLogger.LogError(LogSource, "Required characteristics not found");
                SetStatus("Required characteristics not found");
                await DisconnectAsync();
                return false;
            }

            BleLogger.Log(LogSource, "Connection successful!");
            SetStatus($"Connected to {DeviceName}");
            ConnectionChanged?.Invoke(this, true);
            return true;
        }
        catch (Exception ex)
        {
            BleLogger.LogError(LogSource, "Connection error", ex);
            SetStatus($"Connection error: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        BleLogger.Log(LogSource, "DisconnectAsync called");
        if (_connectedDevice != null)
        {
            var device = _connectedDevice;
            BleLogger.Log(LogSource, $"Disconnecting from {device.Name ?? "(null)"} ID={device.Id}");
            CleanupConnection();

            try
            {
                await _adapter.DisconnectDeviceAsync(device);
                BleLogger.Log(LogSource, "Disconnect successful");
            }
            catch (Exception ex)
            {
                BleLogger.LogError(LogSource, "Disconnect error", ex);
            }

            SetStatus("Disconnected");
            ConnectionChanged?.Invoke(this, false);
        }
        else
        {
            BleLogger.Log(LogSource, "DisconnectAsync: No device connected");
        }
    }

    public async Task<bool> ReadAnalogInputsAsync()
    {
        if (_analogInputsChar == null)
        {
            BleLogger.Log(LogSource, "ReadAnalogInputsAsync: Not connected");
            SetStatus("Not connected");
            return false;
        }

        try
        {
            BleLogger.Log(LogSource, "Reading analog inputs...");
            var data = await _analogInputsChar.ReadAsync();
            if (data.data != null && data.data.Length >= 4)
            {
                BleLogger.LogData(LogSource, "Analog data", data.data);
                var info = new BoatMonitorData
                {
                    Voltage1 = ((data.data[0] << 8) | data.data[1]) / 1000.0,
                    Voltage2 = ((data.data[2] << 8) | data.data[3]) / 1000.0
                };
                BleLogger.Log(LogSource, $"Voltages: V1={info.Voltage1:F2}V, V2={info.Voltage2:F2}V");

                DataReceived?.Invoke(this, info);
                return true;
            }
            else
            {
                BleLogger.Log(LogSource, $"Unexpected data length: {data.data?.Length ?? 0}");
            }
        }
        catch (Exception ex)
        {
            BleLogger.LogError(LogSource, "Read analog inputs error", ex);
            SetStatus($"Read error: {ex.Message}");
        }

        return false;
    }

    public async Task<bool> SetStatusLedAsync(bool on)
    {
        if (_ledOnOffChar == null)
        {
            SetStatus("Not connected");
            return false;
        }

        try
        {
            await _ledOnOffChar.WriteAsync([(byte)(on ? 1 : 0)]);
            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"Write error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetBlinkRateAsync(byte rate)
    {
        if (_blinkRateChar == null)
        {
            SetStatus("Not connected");
            return false;
        }

        try
        {
            await _blinkRateChar.WriteAsync([Math.Min(rate, (byte)100)]);
            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"Write error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetLedStripAsync(bool on, byte red, byte green, byte blue, byte white)
    {
        if (_ledStripChar == null)
        {
            BleLogger.Log(LogSource, "SetLedStripAsync: Not connected");
            SetStatus("Not connected");
            return false;
        }

        try
        {
            var data = new byte[] { (byte)(on ? 1 : 0), red, green, blue, white };
            BleLogger.LogData(LogSource, "Writing LED strip (RGBW)", data);
            await _ledStripChar.WriteAsync(data);
            BleLogger.Log(LogSource, "LED strip write successful");
            return true;
        }
        catch (Exception ex)
        {
            BleLogger.LogError(LogSource, "Write LED strip error", ex);
            SetStatus($"Write error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ReadLedStripStateAsync()
    {
        if (_ledStripChar == null)
        {
            BleLogger.Log(LogSource, "ReadLedStripStateAsync: Not connected");
            SetStatus("Not connected");
            return false;
        }

        try
        {
            BleLogger.Log(LogSource, "Reading LED strip state...");
            var data = await _ledStripChar.ReadAsync();
            if (data.data != null && data.data.Length >= 4)
            {
                BleLogger.LogData(LogSource, "LED strip data", data.data);
                var info = new BoatMonitorData
                {
                    StripOn = data.data[0] != 0,
                    StripRed = data.data[1],
                    StripGreen = data.data[2],
                    StripBlue = data.data[3],
                    StripWhite = data.data.Length >= 5 ? data.data[4] : (byte)0
                };
                BleLogger.Log(LogSource, $"Strip: On={info.StripOn}, R={info.StripRed}, G={info.StripGreen}, B={info.StripBlue}, W={info.StripWhite}");

                DataReceived?.Invoke(this, info);
                return true;
            }
            else
            {
                BleLogger.Log(LogSource, $"Unexpected data length: {data.data?.Length ?? 0}");
            }
        }
        catch (Exception ex)
        {
            BleLogger.LogError(LogSource, "Read LED strip error", ex);
            SetStatus($"Read error: {ex.Message}");
        }

        return false;
    }
}
