# JBD BMS Bluetooth Protocol for Amped Outdoors Batteries

## Overview

This project communicates with Amped Outdoors LiFePO4 batteries via Bluetooth Low Energy (BLE). These batteries use the JBD/Xiaoxiang BMS protocol.

## BLE Connection Details

- **Device Name**: "Amped 24v" (or similar, varies by battery)
- **Service UUID**: `0000FF00-0000-1000-8000-00805F9B34FB` (short form: `FF00`)
- **Write Characteristic**: `0000FF02-0000-1000-8000-00805F9B34FB` (short form: `FF02`)
- **Notify Characteristic**: `0000FF01-0000-1000-8000-00805F9B34FB` (short form: `FF01`)

## Communication Pattern

1. Subscribe to notifications on FF01
2. Write commands to FF02
3. Receive response via notifications on FF01 (may arrive in multiple packets)
4. Concatenate packets and parse response

## Commands

### Basic Info (Command 0x03)

Request battery status including voltage, current, SOC, temperatures, and protection status.

**Command bytes:** `DD A5 03 00 FF FD 77`

```csharp
byte[] basicInfoCommand = new byte[] { 0xDD, 0xA5, 0x03, 0x00, 0xFF, 0xFD, 0x77 };
```

### Cell Voltages (Command 0x04)

Request individual cell voltages.

**Command bytes:** `DD A5 04 00 FF FC 77`

```csharp
byte[] cellVoltageCommand = new byte[] { 0xDD, 0xA5, 0x04, 0x00, 0xFF, 0xFC, 0x77 };
```

## Response Format

Responses start with `DD`, followed by the command ID, status byte, data length, data payload, 2-byte checksum, and end with `77`.

```
DD [CMD] [STATUS] [LENGTH] [DATA...] [CHECKSUM_HI] [CHECKSUM_LO] 77
```

**Important:** BLE may fragment responses into multiple notification packets. Accumulate packets until you receive the `0x77` end byte.

## Parsing Basic Info Response (Command 0x03)

After stripping the header (`DD 03 00 [length]`) and trailer (checksum + `77`), parse the data payload:

| Offset | Bytes | Field | Conversion |
|--------|-------|-------|------------|
| 0 | 2 | Total Voltage | value × 0.01 = Volts |
| 2 | 2 | Current | value × 0.01 = Amps (signed, 0x8000 offset for negative) |
| 4 | 2 | Remaining Capacity | value × 0.01 = Ah |
| 6 | 2 | Full Capacity | value × 0.01 = Ah |
| 8 | 2 | Cycle Count | value = count |
| 10 | 2 | Production Date | encoded date (see below) |
| 12 | 2 | Balance Status Low | bitmask for cells 1-16 |
| 14 | 2 | Balance Status High | bitmask for cells 17-32 |
| 16 | 2 | Protection Status | bitmask (see protection flags) |
| 18 | 1 | Software Version | value |
| 19 | 1 | SOC (State of Charge) | value = percent |
| 20 | 1 | FET Status | bit 0 = charge, bit 1 = discharge |
| 21 | 1 | Cell Count | number of cells in series |
| 22 | 1 | NTC Count | number of temperature sensors |
| 23 | 2×n | NTC Temperatures | (value - 2731) × 0.1 = °C |

### C# Parsing Example

```csharp
public class BmsBasicInfo
{
    public double TotalVoltage { get; set; }      // Volts
    public double Current { get; set; }           // Amps
    public double RemainingCapacity { get; set; } // Ah
    public double FullCapacity { get; set; }      // Ah
    public int CycleCount { get; set; }
    public int StateOfCharge { get; set; }        // Percent
    public int CellCount { get; set; }
    public bool ChargeFetOn { get; set; }
    public bool DischargeFetOn { get; set; }
    public int ProtectionStatus { get; set; }
    public List<double> Temperatures { get; set; } // °C
}

public BmsBasicInfo ParseBasicInfo(byte[] data)
{
    // data should be the payload after DD 03 00 [len] and before checksum
    var info = new BmsBasicInfo();
    
    info.TotalVoltage = ReadUInt16BE(data, 0) * 0.01;
    
    // Current is signed - values >= 0x8000 are negative (discharge)
    int rawCurrent = ReadUInt16BE(data, 2);
    info.Current = (rawCurrent <= 0x7FFF) ? rawCurrent * 0.01 : (rawCurrent - 0x10000) * 0.01;
    
    info.RemainingCapacity = ReadUInt16BE(data, 4) * 0.01;
    info.FullCapacity = ReadUInt16BE(data, 6) * 0.01;
    info.CycleCount = ReadUInt16BE(data, 8);
    info.ProtectionStatus = ReadUInt16BE(data, 16);
    info.StateOfCharge = data[19];
    
    byte fetStatus = data[20];
    info.ChargeFetOn = (fetStatus & 0x01) != 0;
    info.DischargeFetOn = (fetStatus & 0x02) != 0;
    
    info.CellCount = data[21];
    int ntcCount = data[22];
    
    info.Temperatures = new List<double>();
    for (int i = 0; i < ntcCount; i++)
    {
        int rawTemp = ReadUInt16BE(data, 23 + (i * 2));
        double tempC = (rawTemp - 2731) * 0.1;
        info.Temperatures.Add(tempC);
    }
    
    return info;
}

private int ReadUInt16BE(byte[] data, int offset)
{
    return (data[offset] << 8) | data[offset + 1];
}
```

## Parsing Cell Voltages Response (Command 0x04)

The payload contains 2 bytes per cell (big-endian), voltage in millivolts.

```csharp
public List<double> ParseCellVoltages(byte[] data, int cellCount)
{
    var voltages = new List<double>();
    for (int i = 0; i < cellCount; i++)
    {
        int mv = ReadUInt16BE(data, i * 2);
        voltages.Add(mv * 0.001); // Convert mV to V
    }
    return voltages;
}
```

## Protection Status Flags

| Bit | Flag |
|-----|------|
| 0 | Cell overvoltage |
| 1 | Cell undervoltage |
| 2 | Pack overvoltage |
| 3 | Pack undervoltage |
| 4 | Charge overtemperature |
| 5 | Charge undertemperature |
| 6 | Discharge overtemperature |
| 7 | Discharge undertemperature |
| 8 | Charge overcurrent |
| 9 | Discharge overcurrent |
| 10 | Short circuit |
| 11 | IC failure |
| 12 | FET lock |

## Checksum Calculation

The checksum is calculated by summing all bytes between (but not including) the start byte `DD` and the checksum itself, then subtracting from 0x10000.

```csharp
public byte[] CalculateChecksum(byte[] data)
{
    int sum = 0;
    foreach (byte b in data)
    {
        sum += b;
    }
    int checksum = 0x10000 - sum;
    return new byte[] { (byte)(checksum >> 8), (byte)(checksum & 0xFF) };
}
```

## MAUI BLE Implementation Notes

- Use `Plugin.BLE` NuGet package or platform-specific APIs
- Always subscribe to FF01 notifications before writing commands
- Implement packet accumulation - responses often arrive in 2-3 fragments
- Look for `0x77` as the end-of-message marker
- Typical response time is 100-500ms
- Disconnect gracefully when done to preserve battery BMS power

## Response Accumulation Example

```csharp
private List<byte> _responseBuffer = new List<byte>();

private void OnNotificationReceived(byte[] data)
{
    _responseBuffer.AddRange(data);
    
    // Check if we have a complete message (ends with 0x77)
    if (_responseBuffer.Count > 0 && _responseBuffer.Last() == 0x77)
    {
        ProcessCompleteResponse(_responseBuffer.ToArray());
        _responseBuffer.Clear();
    }
}
```
## Real Data
Have Bluetooth connect directly to the battery.  Assume the name is 'Amped 24v'.

## References

- JBD Protocol Documentation: Various community sources
- ESPHome JBD-BMS component: https://github.com/syssi/esphome-jbd-bms
- Protocol details: https://blog.ja-ke.tech/2020/02/07/ltt-power-bms-chinese-protocol.html
