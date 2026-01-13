namespace TimsBoat.Services;

public class BmsBasicInfo
{
    public double TotalVoltage { get; set; }
    public double Current { get; set; }
    public double RemainingCapacity { get; set; }
    public double FullCapacity { get; set; }
    public int CycleCount { get; set; }
    public int StateOfCharge { get; set; }
    public int CellCount { get; set; }
    public bool ChargeFetOn { get; set; }
    public bool DischargeFetOn { get; set; }
    public int ProtectionStatus { get; set; }
    public List<double> Temperatures { get; set; } = [];
    public List<double> CellVoltages { get; set; } = [];
}

public class BleDeviceInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName => string.IsNullOrEmpty(Name) ? $"Unknown ({Id.ToString()[..8]})" : Name;
}

public interface IBatteryService
{
    event EventHandler<string>? StatusChanged;
    event EventHandler<bool>? ConnectionChanged;
    event EventHandler<BmsBasicInfo>? BatteryInfoReceived;
    event EventHandler<BleDeviceInfo>? DeviceDiscovered;

    Guid DeviceId { get; }
    bool IsConnected { get; }
    bool IsScanning { get; }
    string Status { get; }
    string? SelectedDeviceName { get; }

    Task<bool> ConnectAsync();
    Task<bool> ConnectToDeviceAsync(BleDeviceInfo device);
    Task DisconnectAsync();
    Task<bool> RequestBasicInfoAsync();
    Task<bool> RequestCellVoltagesAsync();
    Task StartScanAsync();
    Task StopScanAsync();
    void ClearSelectedDevice();
}
