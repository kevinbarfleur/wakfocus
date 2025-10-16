using System;
using System.IO;
using System.Text.Json;

namespace WakFocus.Config;

/// <summary>
/// Manages loading and saving application configuration
/// </summary>
public class ConfigService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WakFocus");

    private static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Load configuration from disk, or return default if not found
    /// </summary>
    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return GetDefaultConfig();
            }

            string json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);

            return config ?? GetDefaultConfig();
        }
        catch (Exception ex)
        {
            // Log error (could add logging here)
            Console.WriteLine($"Failed to load config: {ex.Message}");
            return GetDefaultConfig();
        }
    }

    /// <summary>
    /// Save configuration to disk
    /// </summary>
    public void Save(AppConfig config)
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(AppDataFolder);

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save config: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get default configuration optimized for Wakfu multi-accounting
    /// </summary>
    private AppConfig GetDefaultConfig()
    {
        return new AppConfig
        {
            Version = 1,
            Hotkey = new HotkeyConfig
            {
                Modifiers = new() { "NONE" },
                Key = "F4"
            },
            Targets = new()
            {
                new TargetMatcher { Type = "process", Match = "java.exe" },
                new TargetMatcher { Type = "class", Match = "SunAwtFrame|LWJGL" },
                new TargetMatcher { Type = "title", Match = "(?i)wakfu" }
            },
            Ordering = new OrderingConfig
            {
                Mode = "manual",
                Manual = new()
            },
            IncludeMinimized = true,
            SkipInvisible = true,
            CompatFallback = false,
            Osd = new OsdConfig
            {
                Enabled = true,
                DurationMs = 500
            }
        };
    }

    /// <summary>
    /// Get the config file path (for display purposes)
    /// </summary>
    public string GetConfigPath() => ConfigFilePath;
}
