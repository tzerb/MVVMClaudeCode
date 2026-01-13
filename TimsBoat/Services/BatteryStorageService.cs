using System.Text.Json;

namespace TimsBoat.Services;

public class BatteryStorageService : IBatteryStorageService
{
    private const string StorageKey = "stored_batteries";

    public Task<List<StoredBattery>> GetStoredBatteriesAsync()
    {
        var json = Preferences.Get(StorageKey, "[]");
        var batteries = JsonSerializer.Deserialize<List<StoredBattery>>(json) ?? [];
        return Task.FromResult(batteries);
    }

    public async Task AddBatteryAsync(StoredBattery battery)
    {
        var batteries = await GetStoredBatteriesAsync();

        // Check if battery already exists
        if (batteries.Any(b => b.Id == battery.Id))
            return;

        batteries.Add(battery);
        var json = JsonSerializer.Serialize(batteries);
        Preferences.Set(StorageKey, json);
    }

    public async Task RemoveBatteryAsync(Guid batteryId)
    {
        var batteries = await GetStoredBatteriesAsync();
        batteries.RemoveAll(b => b.Id == batteryId);
        var json = JsonSerializer.Serialize(batteries);
        Preferences.Set(StorageKey, json);
    }

    public Task ClearAllBatteriesAsync()
    {
        Preferences.Remove(StorageKey);
        return Task.CompletedTask;
    }
}
