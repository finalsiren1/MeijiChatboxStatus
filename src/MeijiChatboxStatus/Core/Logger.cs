namespace MeijiChatboxStatus.Core;

public sealed record LogEntry(DateTime Time, string Level, string Text);

/// <summary>Thread-safe log kept in memory for the UI and mirrored to %LOCALAPPDATA%\MeijiChatboxStatus\app.log.</summary>
public sealed class Logger
{
    private const int Capacity = 300;
    private readonly LinkedList<LogEntry> _entries = new();
    private readonly object _lock = new();
    private readonly string? _filePath;

    public event Action<LogEntry>? Added;

    public Logger()
    {
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeijiChatboxStatus");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "app.log");
            File.WriteAllText(_filePath, $"=== Meiji Chatbox Status {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
        }
        catch (Exception) { _filePath = null; } // logging to disk is best-effort; the in-app log still works
    }

    public void Info(string text) => Add("info", text);
    public void Warn(string text) => Add("warn", text);
    public void Error(string text) => Add("error", text);

    public IReadOnlyList<LogEntry> Snapshot()
    {
        lock (_lock) return _entries.ToList();
    }

    private void Add(string level, string text)
    {
        var entry = new LogEntry(DateTime.Now, level, text);
        lock (_lock)
        {
            _entries.AddLast(entry);
            if (_entries.Count > Capacity) _entries.RemoveFirst();
            if (_filePath is not null)
            {
                try { File.AppendAllText(_filePath, $"[{entry.Time:HH:mm:ss}] {level.ToUpperInvariant(),-5} {text}{Environment.NewLine}"); }
                catch (IOException) { }
            }
        }
        Added?.Invoke(entry);
    }
}
