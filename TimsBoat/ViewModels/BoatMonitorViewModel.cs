using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimsBoat.Services;

namespace TimsBoat.ViewModels;

public partial class BoatMonitorViewModel : ObservableObject
{
    private readonly IBoatMonitorService _service;
    private CancellationTokenSource? _pollCts;
    private const int PollIntervalSeconds = 2;

    [ObservableProperty]
    private string _status = "Not connected";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabStatusText))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabStatusText))]
    private bool _isConnecting;

    [ObservableProperty]
    private bool _isDisconnecting;

    [ObservableProperty]
    private double _voltage1;

    [ObservableProperty]
    private double _voltage2;

    [ObservableProperty]
    private bool _stripOn;

    [ObservableProperty]
    private byte _stripRed = 255;

    [ObservableProperty]
    private byte _stripGreen = 255;

    [ObservableProperty]
    private byte _stripBlue = 255;

    [ObservableProperty]
    private byte _stripWhite;

    [ObservableProperty]
    private Color _selectedColor = Colors.White;

    [ObservableProperty]
    private bool _showLog;

    [ObservableProperty]
    private string _logText = "";

    public string TabStatusText => IsConnecting ? "Connecting..." : (IsConnected ? $"{Voltage1:F1}V / {Voltage2:F1}V" : "Not connected");

    public BoatMonitorViewModel()
    {
        _service = new BoatMonitorService();
        _service.StatusChanged += OnStatusChanged;
        _service.ConnectionChanged += OnConnectionChanged;
        _service.DataReceived += OnDataReceived;

        BleLogger.LogUpdated += OnLogUpdated;
        LogText = BleLogger.GetLog();
    }

    private void OnLogUpdated(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogText = BleLogger.GetLog();
        });
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
                // Read initial strip state
                _ = _service.ReadLedStripStateAsync();
            }
            else
            {
                StopPolling();
            }
        });
    }

    private void OnDataReceived(object? sender, BoatMonitorData data)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (data.Voltage1 > 0 || data.Voltage2 > 0)
            {
                Voltage1 = data.Voltage1;
                Voltage2 = data.Voltage2;
                OnPropertyChanged(nameof(TabStatusText));
            }

            if (data.StripRed > 0 || data.StripGreen > 0 || data.StripBlue > 0 || data.StripWhite > 0 || data.StripOn)
            {
                StripOn = data.StripOn;
                StripRed = data.StripRed;
                StripGreen = data.StripGreen;
                StripBlue = data.StripBlue;
                StripWhite = data.StripWhite;
                SelectedColor = Color.FromRgb(StripRed, StripGreen, StripBlue);
            }
        });
    }

    private void StartPolling()
    {
        StopPolling();
        _pollCts = new CancellationTokenSource();
        _ = PollDataAsync(_pollCts.Token);
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }

    private async Task PollDataAsync(CancellationToken ct)
    {
        await Task.Delay(200, ct);

        while (!ct.IsCancellationRequested && _service.IsConnected)
        {
            try
            {
                await _service.ReadAnalogInputsAsync();
                await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Ignore and continue
            }
        }
    }

    [RelayCommand]
    private async Task Connect()
    {
        if (IsConnected)
        {
            IsDisconnecting = true;
            await _service.DisconnectAsync();
            IsDisconnecting = false;
        }
        else
        {
            IsConnecting = true;
            await _service.ConnectAsync();
            IsConnecting = false;
        }
    }

    public async Task AutoConnectAsync()
    {
        IsConnecting = true;
        await _service.ConnectAsync();
        IsConnecting = false;
    }

    [RelayCommand]
    private async Task ToggleStrip()
    {
        StripOn = !StripOn;
        await _service.SetLedStripAsync(StripOn, StripRed, StripGreen, StripBlue, StripWhite);
    }

    [RelayCommand]
    private async Task SetStripColor(Color color)
    {
        SelectedColor = color;
        StripRed = (byte)(color.Red * 255);
        StripGreen = (byte)(color.Green * 255);
        StripBlue = (byte)(color.Blue * 255);
        StripWhite = 0; // Reset white when setting RGB color

        if (StripOn)
        {
            await _service.SetLedStripAsync(true, StripRed, StripGreen, StripBlue, StripWhite);
        }
    }

    [RelayCommand]
    private async Task SetPresetColor(string colorName)
    {
        var color = colorName switch
        {
            "Red" => Colors.Red,
            "Green" => Colors.Green,
            "Blue" => Colors.Blue,
            "White" => Colors.White,
            "Yellow" => Colors.Yellow,
            "Cyan" => Colors.Cyan,
            "Magenta" => Colors.Magenta,
            "Orange" => Colors.Orange,
            "Off" => Colors.Black,
            _ => Colors.White
        };

        await SetStripColor(color);

        if (colorName == "Off")
        {
            StripOn = false;
            await _service.SetLedStripAsync(false, 0, 0, 0, 0);
        }
        else if (!StripOn)
        {
            StripOn = true;
            await _service.SetLedStripAsync(true, StripRed, StripGreen, StripBlue, StripWhite);
        }
    }

    [RelayCommand]
    private async Task SetWarmWhite(string level)
    {
        // Four levels of warm white: 25%, 50%, 75%, 100%
        StripWhite = level switch
        {
            "25" => 64,
            "50" => 128,
            "75" => 192,
            "100" => 255,
            _ => 128
        };

        // Turn off RGB when using warm white only
        StripRed = 0;
        StripGreen = 0;
        StripBlue = 0;
        SelectedColor = Colors.Black;

        if (!StripOn)
        {
            StripOn = true;
        }

        await _service.SetLedStripAsync(true, StripRed, StripGreen, StripBlue, StripWhite);
    }

    [RelayCommand]
    private void ToggleLog()
    {
        if (ShowLog)
        {
            // Hiding log - clear it
            ShowLog = false;
            BleLogger.Clear();
            LogText = "";
        }
        else
        {
            // Showing log - get current log
            LogText = BleLogger.GetLog();
            ShowLog = true;
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        BleLogger.Clear();
    }

    public void Cleanup()
    {
        StopPolling();
        _service.StatusChanged -= OnStatusChanged;
        _service.ConnectionChanged -= OnConnectionChanged;
        _service.DataReceived -= OnDataReceived;
        BleLogger.LogUpdated -= OnLogUpdated;
    }
}
