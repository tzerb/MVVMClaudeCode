using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace TimsBoat.Services;

public class BatteryService : IBatteryService
{
    // JBD BMS BLE UUIDs
    private static readonly Guid ServiceUuid = Guid.Parse("0000FF00-0000-1000-8000-00805F9B34FB");
    private static readonly Guid WriteCharacteristicUuid = Guid.Parse("0000FF02-0000-1000-8000-00805F9B34FB");
    private static readonly Guid NotifyCharacteristicUuid = Guid.Parse("0000FF01-0000-1000-8000-00805F9B34FB");

    // JBD Commands
    private static readonly byte[] BasicInfoCommand = [0xDD, 0xA5, 0x03, 0x00, 0xFF, 0xFD, 0x77];
    private static readonly byte[] CellVoltageCommand = [0xDD, 0xA5, 0x04, 0x00, 0xFF, 0xFC, 0x77];

    private readonly BleConnectionManager _connectionManager;
    private readonly IAdapter _adapter;
    private IDevice? _connectedDevice;
    private ICharacteristic? _writeCharacteristic;
    private ICharacteristic? _notifyCharacteristic;
    private string _status = "Not connected";
    private bool _isScanning;
    private readonly List<byte> _responseBuffer = [];
    private int _expectedCellCount;
    private BleDeviceInfo? _selectedDevice;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<bool>? ConnectionChanged;
    public event EventHandler<BmsBasicInfo>? BatteryInfoReceived;
    public event EventHandler<BleDeviceInfo>? DeviceDiscovered;

    public Guid DeviceId => _selectedDevice?.Id ?? Guid.Empty;
    public bool IsConnected => _connectedDevice != null;
    public bool IsScanning => _isScanning;
    public string Status => _status;
    public string? SelectedDeviceName => _selectedDevice?.DisplayName;

    public BatteryService()
    {
        _connectionManager = BleConnectionManager.Instance;
        _adapter = _connectionManager.Adapter;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
        _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
    }

    public BatteryService(BleDeviceInfo device) : this()
    {
        _selectedDevice = device;
    }

    private void SetStatus(string status)
    {
        _status = status;
        StatusChanged?.Invoke(this, status);
    }

    private string LogSource => $"BMS:{_selectedDevice?.DisplayName ?? "unknown"}";

    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        if (_connectedDevice != null && e.Device.Id == _connectedDevice.Id)
        {
            BleLogger.Log(LogSource, $"Device disconnected: {e.Device.Name}");
            _connectedDevice = null;
            _writeCharacteristic = null;
            _notifyCharacteristic = null;
            SetStatus("Disconnected");
            ConnectionChanged?.Invoke(this, false);
        }
    }

    private void OnDeviceConnectionLost(object? sender, DeviceErrorEventArgs e)
    {
        if (_connectedDevice != null && e.Device.Id == _connectedDevice.Id)
        {
            BleLogger.LogError(LogSource, $"Connection lost: {e.Device.Name}", e.ErrorMessage != null ? new Exception(e.ErrorMessage) : null);
            _connectedDevice = null;
            _writeCharacteristic = null;
            _notifyCharacteristic = null;
            SetStatus("Connection lost");
            ConnectionChanged?.Invoke(this, false);
        }
    }

    public async Task StartScanAsync()
    {
        if (!_connectionManager.IsBluetoothOn)
        {
            SetStatus("Bluetooth is not enabled");
            return;
        }

        if (_isScanning) return;

        _isScanning = true;
        SetStatus("Scanning for devices...");

        try
        {
            await _connectionManager.StartScanForDevicesAsync(device =>
            {
                DeviceDiscovered?.Invoke(this, device);
            });
        }
        finally
        {
            _isScanning = false;
            SetStatus("Scan complete");
        }
    }

    public async Task StopScanAsync()
    {
        if (_isScanning)
        {
            await _connectionManager.StopScanAsync();
            _isScanning = false;
            SetStatus("Scan stopped");
        }
    }

    public void ClearSelectedDevice()
    {
        _selectedDevice = null;
    }

    public async Task<bool> ConnectToDeviceAsync(BleDeviceInfo device)
    {
        _selectedDevice = device;
        return await ConnectAsync();
    }

    public async Task<bool> ConnectAsync()
    {
        if (!_connectionManager.IsBluetoothOn)
        {
            SetStatus("Bluetooth is not enabled");
            return false;
        }

        if (_selectedDevice == null)
        {
            SetStatus("No device selected");
            return false;
        }

        var batteryDevice = await _connectionManager.FindAndConnectDeviceAsync(
            _selectedDevice.Id,
            _selectedDevice.DisplayName,
            SetStatus);

        if (batteryDevice == null)
        {
            SetStatus($"{_selectedDevice.DisplayName} not found");
            return false;
        }

        SetStatus($"Connecting to {batteryDevice.Name ?? "device"}...");

        try
        {
            await _adapter.ConnectToDeviceAsync(batteryDevice);
            _connectedDevice = batteryDevice;

            var service = await batteryDevice.GetServiceAsync(ServiceUuid);
            if (service == null)
            {
                SetStatus("BLE service not found");
                await DisconnectAsync();
                return false;
            }

            _writeCharacteristic = await service.GetCharacteristicAsync(WriteCharacteristicUuid);
            _notifyCharacteristic = await service.GetCharacteristicAsync(NotifyCharacteristicUuid);

            if (_writeCharacteristic == null || _notifyCharacteristic == null)
            {
                SetStatus("BLE characteristics not found");
                await DisconnectAsync();
                return false;
            }

            // Subscribe to notifications
            _notifyCharacteristic.ValueUpdated += OnNotificationReceived;
            await _notifyCharacteristic.StartUpdatesAsync();

            SetStatus($"Connected to {batteryDevice.Name ?? "device"}");
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

            if (_notifyCharacteristic != null)
            {
                try
                {
                    _notifyCharacteristic.ValueUpdated -= OnNotificationReceived;
                    await _notifyCharacteristic.StopUpdatesAsync();
                }
                catch
                {
                    // Ignore
                }
            }

            try
            {
                await _adapter.DisconnectDeviceAsync(device);
            }
            catch
            {
                _connectedDevice = null;
                _writeCharacteristic = null;
                _notifyCharacteristic = null;
                SetStatus("Disconnected");
                ConnectionChanged?.Invoke(this, false);
            }
        }
    }

    public async Task<bool> RequestBasicInfoAsync()
    {
        if (_writeCharacteristic == null)
        {
            SetStatus("Not connected");
            return false;
        }

        try
        {
            _responseBuffer.Clear();
            await _writeCharacteristic.WriteAsync(BasicInfoCommand);
            SetStatus("Requesting battery info...");
            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"Request error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RequestCellVoltagesAsync()
    {
        if (_writeCharacteristic == null)
        {
            SetStatus("Not connected");
            return false;
        }

        try
        {
            _responseBuffer.Clear();
            await _writeCharacteristic.WriteAsync(CellVoltageCommand);
            SetStatus("Requesting cell voltages...");
            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"Request error: {ex.Message}");
            return false;
        }
    }

    private void OnNotificationReceived(object? sender, CharacteristicUpdatedEventArgs e)
    {
        var data = e.Characteristic.Value;
        if (data == null || data.Length == 0)
        {
            BleLogger.Log(LogSource, "Notification received: null or empty data");
            return;
        }

        BleLogger.LogData(LogSource, $"Notification chunk (buffer was {_responseBuffer.Count} bytes)", data);

        _responseBuffer.AddRange(data);

        // Check for complete message (ends with 0x77)
        if (_responseBuffer.Count > 0 && _responseBuffer[^1] == 0x77)
        {
            BleLogger.Log(LogSource, $"Complete message detected, total buffer: {_responseBuffer.Count} bytes");
            ProcessCompleteResponse([.. _responseBuffer]);
            _responseBuffer.Clear();
        }
    }

    private void ProcessCompleteResponse(byte[] response)
    {
        BleLogger.LogData(LogSource, "Processing complete response", response);

        if (response.Length < 7)
        {
            BleLogger.LogError(LogSource, $"Response too short: {response.Length} bytes (min 7)");
            return;
        }

        // Verify start byte
        if (response[0] != 0xDD)
        {
            BleLogger.LogError(LogSource, $"Invalid start byte: 0x{response[0]:X2} (expected 0xDD)");
            return;
        }

        byte command = response[1];
        byte status = response[2];
        byte length = response[3];

        BleLogger.Log(LogSource, $"Header: cmd=0x{command:X2}, status=0x{status:X2}, payloadLen={length}, totalLen={response.Length}");

        if (status != 0x00)
        {
            BleLogger.LogError(LogSource, $"BMS returned error status: 0x{status:X2}");
            SetStatus("BMS returned error");
            return;
        }

        // Verify response has enough data for the payload
        // Header (4 bytes) + payload (length bytes) + checksum (2 bytes) + end byte (1 byte)
        int expectedMinLength = 4 + length + 3;
        if (response.Length < expectedMinLength)
        {
            BleLogger.LogError(LogSource, $"Response incomplete: have {response.Length} bytes, need {expectedMinLength} (header=4 + payload={length} + checksum=2 + end=1)");
            return;
        }

        // Extract payload (skip header, exclude checksum and end byte)
        var payload = new byte[length];
        Array.Copy(response, 4, payload, 0, length);

        BleLogger.LogData(LogSource, $"Extracted payload for cmd 0x{command:X2}", payload);

        switch (command)
        {
            case 0x03:
                BleLogger.Log(LogSource, "Parsing as BasicInfo (0x03)");
                ParseBasicInfo(payload);
                break;
            case 0x04:
                BleLogger.Log(LogSource, "Parsing as CellVoltages (0x04)");
                ParseCellVoltages(payload);
                break;
            default:
                BleLogger.Log(LogSource, $"Unknown command: 0x{command:X2}");
                break;
        }
    }

    private void ParseBasicInfo(byte[] data)
    {
        if (data.Length < 23) return;

        var info = new BmsBasicInfo
        {
            TotalVoltage = ReadUInt16BE(data, 0) * 0.01,
            RemainingCapacity = ReadUInt16BE(data, 4) * 0.01,
            FullCapacity = ReadUInt16BE(data, 6) * 0.01,
            CycleCount = ReadUInt16BE(data, 8),
            ProtectionStatus = ReadUInt16BE(data, 16),
            StateOfCharge = data[19],
            CellCount = data[21]
        };

        // Current is signed
        int rawCurrent = ReadUInt16BE(data, 2);
        info.Current = rawCurrent <= 0x7FFF ? rawCurrent * 0.01 : (rawCurrent - 0x10000) * 0.01;

        // FET status
        byte fetStatus = data[20];
        info.ChargeFetOn = (fetStatus & 0x01) != 0;
        info.DischargeFetOn = (fetStatus & 0x02) != 0;

        // Temperatures
        int ntcCount = data[22];
        for (int i = 0; i < ntcCount && (23 + i * 2 + 1) < data.Length; i++)
        {
            int rawTemp = ReadUInt16BE(data, 23 + i * 2);
            double tempC = (rawTemp - 2731) * 0.1;
            info.Temperatures.Add(tempC);
        }

        _expectedCellCount = info.CellCount;

        SetStatus($"SOC: {info.StateOfCharge}% | {info.TotalVoltage:F2}V | {info.Current:F2}A");
        BatteryInfoReceived?.Invoke(this, info);
    }

    private void ParseCellVoltages(byte[] data)
    {
        var info = new BmsBasicInfo { CellCount = _expectedCellCount };

        for (int i = 0; i < _expectedCellCount && (i * 2 + 1) < data.Length; i++)
        {
            int mv = ReadUInt16BE(data, i * 2);
            info.CellVoltages.Add(mv * 0.001);
        }

        SetStatus($"Received {info.CellVoltages.Count} cell voltages");
        BatteryInfoReceived?.Invoke(this, info);
    }

    private static int ReadUInt16BE(byte[] data, int offset)
    {
        return (data[offset] << 8) | data[offset + 1];
    }
}
