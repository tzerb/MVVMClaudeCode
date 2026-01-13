using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace TimsBoat.Services;

public class BleConnectionManager
{
    private static BleConnectionManager? _instance;
    public static BleConnectionManager Instance => _instance ??= new BleConnectionManager();

    private readonly IBluetoothLE _bluetoothLE;
    private readonly IAdapter _adapter;
    private readonly Dictionary<Guid, TaskCompletionSource<IDevice?>> _pendingConnections = [];
    private readonly object _lock = new();
    private bool _isScanning;

    public event EventHandler<BleDeviceInfo>? DeviceDiscovered;

    private BleConnectionManager()
    {
        _bluetoothLE = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
    }

    public IAdapter Adapter => _adapter;
    public bool IsBluetoothOn => _bluetoothLE.State == BluetoothState.On;

    public async Task<IDevice?> FindAndConnectDeviceAsync(Guid deviceId, string deviceName, Action<string>? statusCallback = null)
    {
        if (!IsBluetoothOn)
        {
            statusCallback?.Invoke("Bluetooth is not enabled");
            return null;
        }

        TaskCompletionSource<IDevice?> tcs;
        bool shouldStartScan;

        lock (_lock)
        {
            // Check if we're already waiting for this device
            if (_pendingConnections.TryGetValue(deviceId, out var existingTcs))
            {
                return existingTcs.Task.Result;
            }

            tcs = new TaskCompletionSource<IDevice?>();
            _pendingConnections[deviceId] = tcs;
            shouldStartScan = !_isScanning;
        }

        statusCallback?.Invoke($"Scanning for {deviceName}...");

        if (shouldStartScan)
        {
            _ = RunSharedScanAsync();
        }

        // Wait for device to be found (with timeout)
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15));
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        lock (_lock)
        {
            _pendingConnections.Remove(deviceId);
        }

        if (completedTask == timeoutTask)
        {
            statusCallback?.Invoke($"{deviceName} not found");
            tcs.TrySetResult(null);
            return null;
        }

        return await tcs.Task;
    }

    private async Task RunSharedScanAsync()
    {
        lock (_lock)
        {
            if (_isScanning) return;
            _isScanning = true;
        }

        _adapter.DeviceDiscovered += OnDeviceDiscovered;

        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await _adapter.StartScanningForDevicesAsync(cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
            lock (_lock)
            {
                _isScanning = false;
                // Complete any remaining pending connections with null
                foreach (var tcs in _pendingConnections.Values)
                {
                    tcs.TrySetResult(null);
                }
            }
        }
    }

    private void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        var deviceInfo = new BleDeviceInfo
        {
            Id = e.Device.Id,
            Name = e.Device.Name ?? ""
        };
        DeviceDiscovered?.Invoke(this, deviceInfo);

        lock (_lock)
        {
            if (_pendingConnections.TryGetValue(e.Device.Id, out var tcs))
            {
                tcs.TrySetResult(e.Device);
            }
        }
    }

    public async Task StartScanForDevicesAsync(Action<BleDeviceInfo> onDeviceFound)
    {
        if (!IsBluetoothOn) return;

        void handler(object? s, DeviceEventArgs e)
        {
            onDeviceFound(new BleDeviceInfo
            {
                Id = e.Device.Id,
                Name = e.Device.Name ?? ""
            });
        }

        _adapter.DeviceDiscovered += handler;

        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _adapter.StartScanningForDevicesAsync(cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            _adapter.DeviceDiscovered -= handler;
        }
    }

    public async Task StopScanAsync()
    {
        await _adapter.StopScanningForDevicesAsync();
    }
}
