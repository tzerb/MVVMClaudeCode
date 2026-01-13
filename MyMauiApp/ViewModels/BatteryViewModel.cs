using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMauiApp.Services;

namespace MyMauiApp.ViewModels;

public class CellVoltageInfo
{
    public int CellNumber { get; set; }
    public double Voltage { get; set; }
    public bool IsHighest { get; set; }
    public bool IsLowest { get; set; }
    public string Display => $"Cell {CellNumber}: {Voltage:F3}V";
}

public partial class BatteryViewModel : ObservableObject
{
    private readonly IBatteryService _batteryService;
    private readonly INavigationService _navigationService;
    private CancellationTokenSource? _pollCts;
    private const int PollIntervalSeconds = 5;

    [ObservableProperty]
    private string _status = "Not connected";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    private bool _isConnecting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    private bool _isDisconnecting;

    [ObservableProperty]
    private bool _isScanning;

    public bool IsWorking => IsConnecting || IsDisconnecting;

    [ObservableProperty]
    private bool _showDeviceList;

    [ObservableProperty]
    private string? _selectedDeviceName;

    [ObservableProperty]
    private double _totalVoltage;

    [ObservableProperty]
    private double _current;

    [ObservableProperty]
    private int _stateOfCharge;

    [ObservableProperty]
    private double _remainingCapacity;

    [ObservableProperty]
    private double _fullCapacity;

    [ObservableProperty]
    private int _cycleCount;

    [ObservableProperty]
    private int _cellCount;

    [ObservableProperty]
    private bool _chargeFetOn;

    [ObservableProperty]
    private bool _dischargeFetOn;

    [ObservableProperty]
    private string _temperatures = "";

    public ObservableCollection<CellVoltageInfo> CellVoltages { get; } = [];
    public ObservableCollection<BleDeviceInfo> DiscoveredDevices { get; } = [];

    public BatteryViewModel(IBatteryService batteryService, INavigationService navigationService)
    {
        _batteryService = batteryService;
        _navigationService = navigationService;

        _batteryService.StatusChanged += OnStatusChanged;
        _batteryService.ConnectionChanged += OnConnectionChanged;
        _batteryService.BatteryInfoReceived += OnBatteryInfoReceived;
        _batteryService.DeviceDiscovered += OnDeviceDiscovered;

        Status = _batteryService.Status;
        IsConnected = _batteryService.IsConnected;
        SelectedDeviceName = _batteryService.SelectedDeviceName;

        // If already connected, start polling
        if (IsConnected)
        {
            StartPolling();
        }
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

            if (connected)
            {
                StartPolling();
            }
            else
            {
                StopPolling();
                CellVoltages.Clear();
            }
        });
    }

    private void StartPolling()
    {
        StopPolling();
        _pollCts = new CancellationTokenSource();
        _ = PollBatteryDataAsync(_pollCts.Token);
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }

    private async Task PollBatteryDataAsync(CancellationToken ct)
    {
        // Initial delay for BLE to stabilize
        await Task.Delay(200, ct);

        while (!ct.IsCancellationRequested && _batteryService.IsConnected)
        {
            try
            {
                await _batteryService.RequestBasicInfoAsync();
                await Task.Delay(300, ct);
                await _batteryService.RequestCellVoltagesAsync();

                // Wait for next poll interval
                await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Ignore errors and continue polling
            }
        }
    }

    private void OnBatteryInfoReceived(object? sender, BmsBasicInfo info)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (info.TotalVoltage > 0)
            {
                TotalVoltage = info.TotalVoltage;
                Current = info.Current;
                StateOfCharge = info.StateOfCharge;
                RemainingCapacity = info.RemainingCapacity;
                FullCapacity = info.FullCapacity;
                CycleCount = info.CycleCount;
                CellCount = info.CellCount;
                ChargeFetOn = info.ChargeFetOn;
                DischargeFetOn = info.DischargeFetOn;

                if (info.Temperatures.Count > 0)
                {
                    Temperatures = string.Join(", ", info.Temperatures.Select(t => $"{t:F1}C"));
                }
            }

            if (info.CellVoltages.Count > 0)
            {
                UpdateCellVoltages(info.CellVoltages);
            }
        });
    }

    private void UpdateCellVoltages(List<double> voltages)
    {
        if (voltages.Count == 0) return;

        double minVoltage = voltages.Min();
        double maxVoltage = voltages.Max();

        CellVoltages.Clear();
        for (int i = 0; i < voltages.Count; i++)
        {
            CellVoltages.Add(new CellVoltageInfo
            {
                CellNumber = i + 1,
                Voltage = voltages[i],
                IsHighest = voltages[i] == maxVoltage,
                IsLowest = voltages[i] == minVoltage
            });
        }
    }

    private void OnDeviceDiscovered(object? sender, BleDeviceInfo device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Avoid duplicates
            if (!DiscoveredDevices.Any(d => d.Id == device.Id))
            {
                DiscoveredDevices.Add(device);
            }
        });
    }

    [RelayCommand]
    private async Task Connect()
    {
        if (IsConnected)
        {
            IsDisconnecting = true;
            await _batteryService.DisconnectAsync();
            IsDisconnecting = false;
        }
        else
        {
            IsConnecting = true;
            await _batteryService.ConnectAsync();
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task ScanDevices()
    {
        if (IsScanning)
        {
            await _batteryService.StopScanAsync();
            IsScanning = false;
        }
        else
        {
            DiscoveredDevices.Clear();
            ShowDeviceList = true;
            IsScanning = true;
            await _batteryService.StartScanAsync();
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task SelectDevice(BleDeviceInfo? device)
    {
        if (device == null) return;

        ShowDeviceList = false;
        SelectedDeviceName = device.DisplayName;
        IsConnecting = true;
        await _batteryService.ConnectToDeviceAsync(device);
        IsConnecting = false;
    }

    [RelayCommand]
    private void ClearSelectedDevice()
    {
        _batteryService.ClearSelectedDevice();
        SelectedDeviceName = null;
    }

    [RelayCommand]
    private void CancelScan()
    {
        ShowDeviceList = false;
        _ = _batteryService.StopScanAsync();
        IsScanning = false;
    }

    [RelayCommand]
    private async Task GoBack()
    {
        StopPolling();
        await _navigationService.GoBackAsync();
    }
}
