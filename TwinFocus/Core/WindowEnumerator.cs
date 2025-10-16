using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using TwinFocus.Config;
using TwinFocus.NativeAPI;

namespace TwinFocus.Core;

/// <summary>
/// Enumerates and filters windows based on configuration rules
/// </summary>
public class WindowEnumerator
{
    private readonly AppConfig _config;

    public WindowEnumerator(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Find all windows matching the configured target rules
    /// </summary>
    public List<WindowInfo> FindTargetWindows()
    {
        var windows = new List<WindowInfo>();

        // Enumerate all top-level windows
        User32.EnumWindows((hWnd, lParam) =>
        {
            try
            {
                var windowInfo = GetWindowInfo(hWnd);

                if (windowInfo != null && ShouldIncludeWindow(windowInfo))
                {
                    windows.Add(windowInfo);
                }
            }
            catch
            {
                // Skip windows we can't access
            }

            return true; // Continue enumeration
        }, IntPtr.Zero);

        return windows;
    }

    /// <summary>
    /// Get detailed information about a window
    /// </summary>
    private WindowInfo? GetWindowInfo(IntPtr hWnd)
    {
        // Get window text (title)
        int titleLength = User32.GetWindowTextLength(hWnd);
        var titleBuffer = new StringBuilder(titleLength + 1);
        User32.GetWindowText(hWnd, titleBuffer, titleBuffer.Capacity);
        string title = titleBuffer.ToString();

        // Get class name
        var classBuffer = new StringBuilder(256);
        User32.GetClassName(hWnd, classBuffer, classBuffer.Capacity);
        string className = classBuffer.ToString();

        // Get process/thread ID
        uint threadId = User32.GetWindowThreadProcessId(hWnd, out uint processId);

        // Get process path and name
        string processPath = string.Empty;
        string processName = string.Empty;
        try
        {
            IntPtr hProcess = Kernel32.OpenProcess(
                Kernel32.ProcessAccessFlags.QueryLimitedInformation,
                false,
                processId);

            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    var pathBuffer = new StringBuilder(1024);
                    uint size = (uint)pathBuffer.Capacity;

                    if (Kernel32.QueryFullProcessImageName(hProcess, 0, pathBuffer, ref size))
                    {
                        processPath = pathBuffer.ToString();
                        processName = Path.GetFileName(processPath);
                    }
                }
                finally
                {
                    Kernel32.CloseHandle(hProcess);
                }
            }
        }
        catch
        {
            // May fail for protected processes
        }

        // Get visibility and minimized state
        bool isVisible = User32.IsWindowVisible(hWnd);
        bool isMinimized = User32.IsIconic(hWnd);

        // Get window bounds
        User32.GetWindowRect(hWnd, out var rect);
        var bounds = new WindowBounds(rect.Left, rect.Top, rect.Right, rect.Bottom);

        return new WindowInfo
        {
            Handle = hWnd,
            Title = title,
            ClassName = className,
            ProcessId = processId,
            ThreadId = threadId,
            ProcessPath = processPath,
            ProcessName = processName,
            IsVisible = isVisible,
            IsMinimized = isMinimized,
            Bounds = bounds
        };
    }

    /// <summary>
    /// Check if a window should be included based on config rules
    /// </summary>
    private bool ShouldIncludeWindow(WindowInfo window)
    {
        // Skip invisible windows if configured
        if (_config.SkipInvisible && !window.IsVisible)
            return false;

        // Skip minimized windows if configured
        if (!_config.IncludeMinimized && window.IsMinimized)
            return false;

        // Check if window has an owner (skip owned windows like dialogs)
        IntPtr owner = User32.GetWindow(window.Handle, User32.GetWindowCmd.GW_OWNER);
        if (owner != IntPtr.Zero)
            return false;

        // Check extended window styles (skip tool windows)
        IntPtr exStyle = User32.GetWindowLongPtr(window.Handle, User32.GWL_EXSTYLE);
        bool isToolWindow = (exStyle.ToInt32() & User32.WS_EX_TOOLWINDOW) != 0;
        if (isToolWindow)
            return false;

        // Apply target matchers
        if (_config.Targets == null || _config.Targets.Count == 0)
            return true; // No filters = include all

        return MatchesAnyTarget(window);
    }

    /// <summary>
    /// Check if window matches configured target rules
    /// Uses AND logic: all specified filter types must match
    /// </summary>
    private bool MatchesAnyTarget(WindowInfo window)
    {
        // Group targets by type
        var processTargets = new List<TargetMatcher>();
        var classTargets = new List<TargetMatcher>();
        var titleTargets = new List<TargetMatcher>();

        foreach (var target in _config.Targets)
        {
            switch (target.Type)
            {
                case "process":
                    processTargets.Add(target);
                    break;
                case "class":
                    classTargets.Add(target);
                    break;
                case "title":
                    titleTargets.Add(target);
                    break;
            }
        }

        // If process filters exist, at least one must match
        if (processTargets.Count > 0)
        {
            bool processMatched = false;
            foreach (var target in processTargets)
            {
                try
                {
                    if (MatchesProcess(window, target.Match, target.PathRegex))
                    {
                        processMatched = true;
                        break;
                    }
                }
                catch
                {
                    // Invalid matcher - skip
                }
            }
            if (!processMatched)
                return false; // Process filter exists but didn't match
        }

        // If class filters exist, at least one must match
        if (classTargets.Count > 0)
        {
            bool classMatched = false;
            foreach (var target in classTargets)
            {
                try
                {
                    if (MatchesClass(window, target.Match))
                    {
                        classMatched = true;
                        break;
                    }
                }
                catch
                {
                    // Invalid matcher - skip
                }
            }
            if (!classMatched)
                return false; // Class filter exists but didn't match
        }

        // If title filters exist, at least one must match
        if (titleTargets.Count > 0)
        {
            bool titleMatched = false;
            foreach (var target in titleTargets)
            {
                try
                {
                    if (MatchesTitle(window, target.Match))
                    {
                        titleMatched = true;
                        break;
                    }
                }
                catch
                {
                    // Invalid matcher - skip
                }
            }
            if (!titleMatched)
                return false; // Title filter exists but didn't match
        }

        // All specified filter types matched
        return true;
    }

    private bool MatchesProcess(WindowInfo window, string match, string? pathRegex)
    {
        // Match by process name (case-insensitive)
        if (!string.IsNullOrEmpty(match))
        {
            if (window.ProcessName.Equals(match, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Optional: match by full path regex
        if (!string.IsNullOrEmpty(pathRegex) && !string.IsNullOrEmpty(window.ProcessPath))
        {
            var regex = new Regex(pathRegex, RegexOptions.IgnoreCase);
            if (regex.IsMatch(window.ProcessPath))
                return true;
        }

        return false;
    }

    private bool MatchesClass(WindowInfo window, string match)
    {
        if (string.IsNullOrEmpty(match))
            return false;

        // Support regex for class name
        var regex = new Regex(match, RegexOptions.IgnoreCase);
        return regex.IsMatch(window.ClassName);
    }

    private bool MatchesTitle(WindowInfo window, string match)
    {
        if (string.IsNullOrEmpty(match))
            return false;

        // Support regex for title
        var regex = new Regex(match, RegexOptions.IgnoreCase);
        return regex.IsMatch(window.Title);
    }
}
