using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WakFocus.Config;

/// <summary>
/// Application configuration model
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Config schema version for migrations
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Global hotkey configuration
    /// </summary>
    [JsonPropertyName("hotkey")]
    public HotkeyConfig Hotkey { get; set; } = new();

    /// <summary>
    /// Target window matchers
    /// </summary>
    [JsonPropertyName("targets")]
    public List<TargetMatcher> Targets { get; set; } = new();

    /// <summary>
    /// Window ordering configuration
    /// </summary>
    [JsonPropertyName("ordering")]
    public OrderingConfig Ordering { get; set; } = new();

    /// <summary>
    /// Include minimized windows in cycle
    /// </summary>
    [JsonPropertyName("includeMinimized")]
    public bool IncludeMinimized { get; set; } = true;

    /// <summary>
    /// Skip invisible windows
    /// </summary>
    [JsonPropertyName("skipInvisible")]
    public bool SkipInvisible { get; set; } = true;

    /// <summary>
    /// Enable compatibility fallback (thread attach)
    /// </summary>
    [JsonPropertyName("compatFallback")]
    public bool CompatFallback { get; set; } = false;

    /// <summary>
    /// OSD (on-screen display) configuration
    /// </summary>
    [JsonPropertyName("osd")]
    public OsdConfig Osd { get; set; } = new();
}

/// <summary>
/// Hotkey configuration
/// </summary>
public class HotkeyConfig
{
    /// <summary>
    /// Modifier keys: "CTRL", "ALT", "SHIFT", "WIN", "NONE"
    /// </summary>
    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = new() { "NONE" };

    /// <summary>
    /// Virtual key name (e.g., "F2", "A", "1")
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = "F2";
}

/// <summary>
/// Target window matcher
/// </summary>
public class TargetMatcher
{
    /// <summary>
    /// Matcher type: "process", "class", "title"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "process";

    /// <summary>
    /// Match string (exact for process, regex for class/title)
    /// </summary>
    [JsonPropertyName("match")]
    public string Match { get; set; } = string.Empty;

    /// <summary>
    /// Optional regex for full process path (process type only)
    /// </summary>
    [JsonPropertyName("pathRegex")]
    public string? PathRegex { get; set; }
}

/// <summary>
/// Window ordering configuration
/// </summary>
public class OrderingConfig
{
    /// <summary>
    /// Ordering mode: "manual", "lastActive", "screenYThenX"
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "screenYThenX";

    /// <summary>
    /// Manual order list (used when mode is "manual")
    /// </summary>
    [JsonPropertyName("manual")]
    public List<TargetMatcher>? Manual { get; set; }
}

/// <summary>
/// OSD (on-screen display) configuration
/// </summary>
public class OsdConfig
{
    /// <summary>
    /// Enable OSD
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Display duration in milliseconds
    /// </summary>
    [JsonPropertyName("ms")]
    public int DurationMs { get; set; } = 500;
}
