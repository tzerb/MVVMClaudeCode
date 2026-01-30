namespace TimsBoat.Services;

public class BoatMonitorData
{
    public double Voltage1 { get; set; }
    public double Voltage2 { get; set; }
    public bool StatusLedOn { get; set; }
    public byte BlinkRate { get; set; }
    public bool StripOn { get; set; }
    public byte StripRed { get; set; }
    public byte StripGreen { get; set; }
    public byte StripBlue { get; set; }
    public byte StripWhite { get; set; }
}

public interface IBoatMonitorService
{
    event EventHandler<string>? StatusChanged;
    event EventHandler<bool>? ConnectionChanged;
    event EventHandler<BoatMonitorData>? DataReceived;

    bool IsConnected { get; }
    string Status { get; }

    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> ReadAnalogInputsAsync();
    Task<bool> SetStatusLedAsync(bool on);
    Task<bool> SetBlinkRateAsync(byte rate);
    Task<bool> SetLedStripAsync(bool on, byte red, byte green, byte blue, byte white);
    Task<bool> ReadLedStripStateAsync();
}
