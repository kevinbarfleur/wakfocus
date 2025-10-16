using System;

namespace WakFocus.Core;

/// <summary>
/// Information about a discovered window
/// </summary>
public class WindowInfo
{
    /// <summary>
    /// Window handle
    /// </summary>
    public IntPtr Handle { get; init; }

    /// <summary>
    /// Window title
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Window class name
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    /// Process ID
    /// </summary>
    public uint ProcessId { get; init; }

    /// <summary>
    /// Thread ID
    /// </summary>
    public uint ThreadId { get; init; }

    /// <summary>
    /// Full path to process executable (may be empty if access denied)
    /// </summary>
    public string ProcessPath { get; init; } = string.Empty;

    /// <summary>
    /// Process name (filename without path)
    /// </summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>
    /// Whether the window is visible
    /// </summary>
    public bool IsVisible { get; init; }

    /// <summary>
    /// Whether the window is minimized
    /// </summary>
    public bool IsMinimized { get; init; }

    /// <summary>
    /// Window bounds (position and size)
    /// </summary>
    public required WindowBounds Bounds { get; init; }

    /// <summary>
    /// Last activation time (tracked by app, not OS)
    /// </summary>
    public DateTime LastActivated { get; set; } = DateTime.MinValue;

    public override string ToString()
    {
        return $"{ProcessName} - {Title} (HWND: {Handle:X8})";
    }
}

/// <summary>
/// Window position and size
/// </summary>
public record WindowBounds(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
