using Blue.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Blue.Services;

public class SettingsService : ObservableObject, ISettingsService
{
    private readonly string _settingsPath;
    private Dictionary<string, object?> _values;
    private readonly object _lock = new();
    private Timer? _debounceTimer;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "Blue");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
        _values = Load();
    }

    private Dictionary<string, object?> Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
        }
        return new Dictionary<string, object?>();
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true });
            // Atomic write: write to temp, then move/rename
            var tmp = _settingsPath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _settingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
        }
    }

    private void ScheduleSave()
    {
        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                lock (_lock)
                {
                    Save();
                    _debounceTimer?.Dispose();
                    _debounceTimer = null;
                }
            }, null, 300, Timeout.Infinite);
        }
    }

    private T Get<T>(string key, T defaultValue)
    {
        lock (_lock)
        {
            if (_values.TryGetValue(key, out var val) && val is JsonElement je)
            {
                try { return JsonSerializer.Deserialize<T>(je.GetRawText())!; }
                catch { }
            }
        }
        return defaultValue;
    }

    private void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            _values[key] = value;
            ScheduleSave();
        }
        OnPropertyChanged();
    }

    public bool AutoPin
    {
        get => Get("AutoPin", true);
        set { Set("AutoPin", value); OnPropertyChanged(); }
    }

    public bool EnableTray
    {
        get => Get("enableTray", true);
        set { Set("enableTray", value); OnPropertyChanged(); }
    }

    public bool TranslucentBackground
    {
        get => Get("TranslucentBackground", true);
        set { Set("TranslucentBackground", value); OnPropertyChanged(); }
    }

    public int Tokens
    {
        get => Get("Tokens", 100);
        set
        {
            if (value > 50 && value < 2000)
                Set("Tokens", value);
            else
                Set("Tokens", 100);
            OnPropertyChanged();
        }
    }

    public bool KeyboardEnabled
    {
        get => Get("KeyboardEnabled", true);
        set { Set("KeyboardEnabled", value); OnPropertyChanged(); }
    }
}
