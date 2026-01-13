using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimsBoat.Services;

namespace TimsBoat.ViewModels;

public partial class BatteryTabViewModel : ObservableObject
{
    private readonly IBatteryService _batteryService;
    private readonly IBatteryStorageService _storageService;
    private readonly Action<BatteryTabViewModel> _onDeleteRequested;
    private CancellationTokenSource? _pollCts;
    private const int PollIntervalSeconds = 5;

    [ObservableProperty]
    private string _tabTitle;

    [ObservableProperty]
    private Guid _batteryId;

    [ObservableProperty]
    private string _status = "Not connected";

    [ObservableProperty]
    private string _batteryStatus = "";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    [NotifyPropertyChangedFor(nameof(TabStatusText))]
    private bool _isConnecting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    private bool _isDisconnecting;

    public bool IsWorking => IsConnecting || IsDisconnecting;

    public string TabStatusText => IsConnecting ? "Connecting..." : "Not connected";

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

    public BatteryTabViewModel(
        StoredBattery storedBattery,
        IBatteryStorageService storageService,
        Action<BatteryTabViewModel> onDeleteRequested)
    {
        _storageService = storageService;
        _onDeleteRequested = onDeleteRequested;
        BatteryId = storedBattery.Id;
        TabTitle = storedBattery.DisplayName;

        var deviceInfo = new BleDeviceInfo
        {
            Id = storedBattery.Id,
            Name = storedBattery.Name
        };
        _batteryService = new BatteryService(deviceInfo);

        _batteryService.StatusChanged += OnStatusChanged;
        _batteryService.ConnectionChanged += OnConnectionChanged;
        _batteryService.BatteryInfoReceived += OnBatteryInfoReceived;

        Status = _batteryService.Status;
        IsConnected = _batteryService.IsConnected;
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

                // Update the consolidated battery status
                UpdateBatteryStatus();
            }

            if (info.CellVoltages.Count > 0)
            {
                UpdateCellVoltages(info.CellVoltages);
            }
        });
    }

    private void UpdateBatteryStatus()
    {
        var lines = new List<string>
        {
            $"SOC: {StateOfCharge}%  |  {TotalVoltage:F2}V  |  {Current:F2}A",
            $"Capacity: {RemainingCapacity:F1}/{FullCapacity:F1} Ah  |  Cycles: {CycleCount}",
            $"Cells: {CellCount}  |  Charge: {(ChargeFetOn ? "ON" : "OFF")}  |  Discharge: {(DischargeFetOn ? "ON" : "OFF")}"
        };

        if (!string.IsNullOrEmpty(Temperatures))
        {
            lines.Add($"Temps: {Temperatures}");
        }

        BatteryStatus = string.Join("\n", lines);
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
    private async Task Delete()
    {
        StopPolling();
        if (IsConnected)
        {
            await _batteryService.DisconnectAsync();
        }
        await _storageService.RemoveBatteryAsync(BatteryId);
        _onDeleteRequested(this);
    }

    public async Task AutoConnectAsync()
    {
        IsConnecting = true;
        await _batteryService.ConnectAsync();
        IsConnecting = false;
    }

    public void Cleanup()
    {
        StopPolling();
        _batteryService.StatusChanged -= OnStatusChanged;
        _batteryService.ConnectionChanged -= OnConnectionChanged;
        _batteryService.BatteryInfoReceived -= OnBatteryInfoReceived;
    }
}
