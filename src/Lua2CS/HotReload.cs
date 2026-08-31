using Microsoft.Extensions.Logging;
using ThreadingTimer = System.Threading.Timer;

namespace Lua2CS;

public sealed class HotReload : IDisposable
{
    private readonly LuaPluginManager _manager;
    private readonly ILogger _logger;
    private readonly Action<Action> _scheduleOnGameThread;
    private readonly int _debounceMilliseconds;
    private readonly FileSystemWatcher _watcher;
    private readonly object _sync = new();
    private readonly HashSet<string> _changedPaths = new(StringComparer.Ordinal);
    private ThreadingTimer? _timer;
    private bool _disposed;

    public HotReload(LuaPluginManager manager, ILogger logger, int debounceMilliseconds, Action<Action> scheduleOnGameThread)
    {
        _manager = manager;
        _logger = logger;
        _scheduleOnGameThread = scheduleOnGameThread;
        _debounceMilliseconds = Math.Clamp(debounceMilliseconds, 100, 5000);
        _watcher = new FileSystemWatcher(manager.ScriptsDirectory, "*.lua")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        lock (_sync)
        {
            _timer?.Dispose();
            _timer = null;
            _changedPaths.Clear();
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs args) => Queue(args.FullPath);

    private void OnRenamed(object sender, RenamedEventArgs args)
    {
        Queue(args.OldFullPath);
        Queue(args.FullPath);
    }

    private void Queue(string path)
    {
        if (_disposed || !path.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)) return;
        lock (_sync)
        {
            _changedPaths.Add(Path.GetFullPath(path));
            _timer?.Dispose();
            _timer = new ThreadingTimer(_ => Flush(), null, _debounceMilliseconds, Timeout.Infinite);
        }
    }

    private void Flush()
    {
        string[] paths;
        lock (_sync)
        {
            if (_disposed) return;
            paths = _changedPaths.ToArray();
            _changedPaths.Clear();
            _timer?.Dispose();
            _timer = null;
        }

        _scheduleOnGameThread(() =>
        {
            try
            {
                _manager.RefreshFiles(paths);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Automatic Lua reload failed");
            }
        });
    }

    private void OnError(object sender, ErrorEventArgs args) =>
        _logger.LogError(args.GetException(), "Lua script file watcher failed");
}
