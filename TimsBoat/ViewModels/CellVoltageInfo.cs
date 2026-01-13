namespace TimsBoat.ViewModels;

public class CellVoltageInfo
{
    public int CellNumber { get; set; }
    public double Voltage { get; set; }
    public bool IsHighest { get; set; }
    public bool IsLowest { get; set; }
    public string Display => $"Cell {CellNumber}: {Voltage:F3}V";
}
