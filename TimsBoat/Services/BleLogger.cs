using System.Text;

namespace TimsBoat.Services;

public static class BleLogger
{
    private static readonly object _lock = new();
    private static readonly StringBuilder _log = new();
    private const int MaxLogLength = 50000;

    public static event EventHandler? LogUpdated;

    public static void Log(string source, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var entry = $"[{timestamp}] [{source}] {message}\n";

        lock (_lock)
        {
            _log.Append(entry);

            // Trim if too long (keep last portion)
            if (_log.Length > MaxLogLength)
            {
                var trimmed = _log.ToString().Substring(_log.Length - MaxLogLength / 2);
                _log.Clear();
                _log.Append("... (log trimmed) ...\n");
                _log.Append(trimmed);
            }
        }

        LogUpdated?.Invoke(null, EventArgs.Empty);
    }

    public static void LogData(string source, string label, byte[] data)
    {
        var hex = BitConverter.ToString(data).Replace("-", " ");
        Log(source, $"{label}: [{data.Length} bytes] {hex}");
    }

    public static void LogError(string source, string message, Exception? ex = null)
    {
        if (ex != null)
        {
            Log(source, $"ERROR: {message} - {ex.GetType().Name}: {ex.Message}");
        }
        else
        {
            Log(source, $"ERROR: {message}");
        }
    }

    public static string GetLog()
    {
        lock (_lock)
        {
            return _log.ToString();
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _log.Clear();
        }
        LogUpdated?.Invoke(null, EventArgs.Empty);
    }
}
