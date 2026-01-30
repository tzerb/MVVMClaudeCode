using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimsBoat.Services;

namespace TimsBoat.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBatteryStorageService _storageService;
    private bool _isInitialized;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _showDeviceList;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoatMonitorSelected))]
    [NotifyPropertyChangedFor(nameof(IsBatterySelected))]
    private BatteryTabViewModel? _selectedBattery;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoatMonitorSelected))]
    [NotifyPropertyChangedFor(nameof(IsBatterySelected))]
    private bool _boatMonitorTabSelected = true;

    public bool IsBoatMonitorSelected => BoatMonitorTabSelected;
    public bool IsBatterySelected => !BoatMonitorTabSelected && SelectedBattery != null;

    public BoatMonitorViewModel BoatMonitor { get; } = new();

    public ObservableCollection<BatteryTabViewModel> Batteries { get; } = [];
    public ObservableCollection<BleDeviceInfo> DiscoveredDevices { get; } = [];

    private readonly BatteryService _scanService;

    public MainViewModel(IBatteryStorageService storageService)
    {
        _storageService = storageService;
        _scanService = new BatteryService();
        _scanService.DeviceDiscovered += OnDeviceDiscovered;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        var storedBatteries = await _storageService.GetStoredBatteriesAsync();

        foreach (var stored in storedBatteries)
        {
            var vm = new BatteryTabViewModel(stored, _storageService, OnBatteryDeleted);
            Batteries.Add(vm);
        }

        // Select first battery if any
        if (Batteries.Count > 0)
        {
            SelectedBattery = Batteries[0];
        }

        // Auto-connect to BoatMonitor and all batteries in parallel
        _ = BoatMonitor.AutoConnectAsync();
        foreach (var battery in Batteries)
        {
            _ = battery.AutoConnectAsync();
        }
    }

    private void OnBatteryDeleted(BatteryTabViewModel battery)
    {
        battery.Cleanup();
        Batteries.Remove(battery);

        if (SelectedBattery == battery)
        {
            SelectedBattery = Batteries.FirstOrDefault();
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
    private async Task ScanForDevices()
    {
        if (IsScanning)
        {
            await _scanService.StopScanAsync();
            IsScanning = false;
        }
        else
        {
            DiscoveredDevices.Clear();
            ShowDeviceList = true;
            IsScanning = true;
            await _scanService.StartScanAsync();
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task SelectDevice(BleDeviceInfo? device)
    {
        if (device == null) return;

        ShowDeviceList = false;
        await _scanService.StopScanAsync();
        IsScanning = false;

        // Check if battery already exists
        if (Batteries.Any(b => b.BatteryId == device.Id))
        {
            // Select the existing battery
            SelectedBattery = Batteries.First(b => b.BatteryId == device.Id);
            return;
        }

        // Add to storage
        var storedBattery = new StoredBattery
        {
            Id = device.Id,
            Name = device.Name
        };
        await _storageService.AddBatteryAsync(storedBattery);

        // Create new tab
        var vm = new BatteryTabViewModel(storedBattery, _storageService, OnBatteryDeleted);
        Batteries.Add(vm);
        SelectedBattery = vm;

        // Auto-connect
        _ = vm.AutoConnectAsync();
    }

    [RelayCommand]
    private void CancelScan()
    {
        ShowDeviceList = false;
        _ = _scanService.StopScanAsync();
        IsScanning = false;
    }

    [RelayCommand]
    private void SelectBatteryTab(BatteryTabViewModel? battery)
    {
        if (battery != null)
        {
            BoatMonitorTabSelected = false;
            SelectedBattery = battery;
        }
    }

    [RelayCommand]
    private void SelectBoatMonitorTab()
    {
        BoatMonitorTabSelected = true;
    }
}
