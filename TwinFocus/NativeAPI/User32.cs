using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TwinFocus.NativeAPI;

/// <summary>
/// P/Invoke declarations for User32.dll - Window management, hotkeys, and input
/// </summary>
public static class User32
{
    private const string DllName = "user32.dll";

    // Constants for hotkey registration
    public const int WM_HOTKEY = 0x0312;

    // Hotkey modifiers
    [Flags]
    public enum HotkeyModifiers : uint
    {
        None = 0,
        MOD_ALT = 0x0001,
        MOD_CONTROL = 0x0002,
        MOD_SHIFT = 0x0004,
        MOD_WIN = 0x0008,
        MOD_NOREPEAT = 0x4000
    }

    // ShowWindow commands
    public enum ShowWindowCommands : int
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_NORMAL = 1,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
        SW_SHOWDEFAULT = 10,
        SW_FORCEMINIMIZE = 11
    }

    // Window styles (extended)
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_APPWINDOW = 0x00040000;

    // GetWindow constants
    public enum GetWindowCmd : uint
    {
        GW_HWNDFIRST = 0,
        GW_HWNDLAST = 1,
        GW_HWNDNEXT = 2,
        GW_HWNDPREV = 3,
        GW_OWNER = 4,
        GW_CHILD = 5
    }

    // Delegate for EnumWindows callback
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // RegisterHotKey - Register a global hotkey
    [DllImport(DllName, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        HotkeyModifiers fsModifiers,
        uint vk);

    // UnregisterHotKey - Unregister a global hotkey
    [DllImport(DllName, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // EnumWindows - Enumerate all top-level windows
    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    // GetWindowText - Get window title
    [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    // GetWindowTextLength - Get window title length
    [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    // GetClassName - Get window class name
    [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    // IsWindowVisible - Check if window is visible
    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    // IsIconic - Check if window is minimized
    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    // GetWindowThreadProcessId - Get process/thread ID of window owner
    [DllImport(DllName, SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // SetForegroundWindow - Activate a window
    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // GetForegroundWindow - Get currently active window
    [DllImport(DllName)]
    public static extern IntPtr GetForegroundWindow();

    // ShowWindow - Show/hide/minimize/maximize window
    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

    // ShowWindowAsync - Async version of ShowWindow
    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindowAsync(IntPtr hWnd, ShowWindowCommands nCmdShow);

    // AttachThreadInput - Attach input processing of two threads
    [DllImport(DllName, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    // AllowSetForegroundWindow - Allow a process to set foreground window
    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AllowSetForegroundWindow(uint dwProcessId);

    // GetWindowLong - Get window information (32-bit compatible)
    [DllImport(DllName, SetLastError = true, EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    // GetWindowLongPtr - Get window information (64-bit)
    [DllImport(DllName, SetLastError = true, EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    // Platform-agnostic GetWindowLongPtr
    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return GetWindowLongPtr64(hWnd, nIndex);
        else
            return new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    // GetWindow - Get related window (owner, etc.)
    [DllImport(DllName, SetLastError = true)]
    public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCmd uCmd);

    // GetWindowRect - Get window position and size
    [DllImport(DllName, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // FlashWindow - Flash window in taskbar
    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

    // GetCurrentThreadId - Get current thread ID
    [DllImport(DllName)]
    public static extern uint GetCurrentThreadId();
}

/// <summary>
/// RECT structure for window bounds
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
