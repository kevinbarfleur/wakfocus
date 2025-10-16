using System;
using System.Collections.Generic;
using System.Linq;
using TwinFocus.Config;

namespace TwinFocus.Core;

/// <summary>
/// Orders windows for cycling based on configured strategy
/// </summary>
public class WindowOrderer
{
    private readonly AppConfig _config;

    public WindowOrderer(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Order windows according to the configured strategy
    /// </summary>
    public List<WindowInfo> OrderWindows(List<WindowInfo> windows)
    {
        if (windows == null || windows.Count == 0)
            return new List<WindowInfo>();

        return _config.Ordering.Mode.ToLowerInvariant() switch
        {
            "manual" => OrderManual(windows),
            "lastactive" => OrderByLastActive(windows),
            "screenythenx" => OrderByScreenLayout(windows),
            _ => OrderByScreenLayout(windows) // Default
        };
    }

    /// <summary>
    /// Get the next window in the cycle after the current one
    /// </summary>
    public WindowInfo? GetNext(List<WindowInfo> orderedWindows, IntPtr currentHwnd)
    {
        if (orderedWindows == null || orderedWindows.Count == 0)
            return null;

        if (orderedWindows.Count == 1)
            return orderedWindows[0];

        // Find current window index
        int currentIndex = -1;
        for (int i = 0; i < orderedWindows.Count; i++)
        {
            if (orderedWindows[i].Handle == currentHwnd)
            {
                currentIndex = i;
                break;
            }
        }

        // If current not found, or if it's the last one, wrap to first
        int nextIndex = (currentIndex + 1) % orderedWindows.Count;
        return orderedWindows[nextIndex];
    }

    /// <summary>
    /// Order by manual configuration list
    /// </summary>
    private List<WindowInfo> OrderManual(List<WindowInfo> windows)
    {
        if (_config.Ordering.Manual == null || _config.Ordering.Manual.Count == 0)
            return OrderByScreenLayout(windows); // Fallback

        var ordered = new List<WindowInfo>();
        var remaining = new List<WindowInfo>(windows);

        // Process each manual matcher in order
        foreach (var matcher in _config.Ordering.Manual)
        {
            var matches = remaining.Where(w => MatchesMatcher(w, matcher)).ToList();
            ordered.AddRange(matches);

            foreach (var match in matches)
            {
                remaining.Remove(match);
            }
        }

        // Add any remaining windows that didn't match
        ordered.AddRange(remaining);

        return ordered;
    }

    /// <summary>
    /// Order by last activation time (most recently activated first, then cycle)
    /// </summary>
    private List<WindowInfo> OrderByLastActive(List<WindowInfo> windows)
    {
        return windows
            .OrderByDescending(w => w.LastActivated)
            .ToList();
    }

    /// <summary>
    /// Order by screen layout (top to bottom, then left to right)
    /// Perfect for stacked fullscreen game windows
    /// </summary>
    private List<WindowInfo> OrderByScreenLayout(List<WindowInfo> windows)
    {
        return windows
            .OrderBy(w => w.Bounds.Top)    // Primary: Y position (top first)
            .ThenBy(w => w.Bounds.Left)    // Secondary: X position (left first)
            .ToList();
    }

    /// <summary>
    /// Check if a window matches a manual matcher
    /// </summary>
    private bool MatchesMatcher(WindowInfo window, TargetMatcher matcher)
    {
        try
        {
            return matcher.Type.ToLowerInvariant() switch
            {
                "process" => window.ProcessName.Equals(matcher.Match, StringComparison.OrdinalIgnoreCase),
                "class" => System.Text.RegularExpressions.Regex.IsMatch(window.ClassName, matcher.Match, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                "title" => System.Text.RegularExpressions.Regex.IsMatch(window.Title, matcher.Match, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}
