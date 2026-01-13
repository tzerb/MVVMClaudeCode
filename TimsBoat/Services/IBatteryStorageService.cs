namespace TimsBoat.Services;

public class StoredBattery
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName => string.IsNullOrEmpty(Name) ? $"Unknown ({Id.ToString()[..8]})" : Name;
}

public interface IBatteryStorageService
{
    Task<List<StoredBattery>> GetStoredBatteriesAsync();
    Task AddBatteryAsync(StoredBattery battery);
    Task RemoveBatteryAsync(Guid batteryId);
    Task ClearAllBatteriesAsync();
}
