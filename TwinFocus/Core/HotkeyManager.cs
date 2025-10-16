using System;
using System.Windows.Interop;
using TwinFocus.NativeAPI;

namespace TwinFocus.Core;

/// <summary>
/// Manages global hotkey registration and routing WM_HOTKEY messages
/// </summary>
public class HotkeyManager : IDisposable
{
    private const int HOTKEY_ID = 1;
    private HwndSource? _hwndSource;
    private bool _isRegistered;
    private User32.HotkeyModifiers _currentModifiers;
    private uint _currentVirtualKey;

    /// <summary>
    /// Event raised when the registered hotkey is pressed
    /// </summary>
    public event EventHandler? HotkeyPressed;

    /// <summary>
    /// Initialize the HotkeyManager with a message-only window
    /// </summary>
    public void Initialize()
    {
        if (_hwndSource != null)
            return;

        // Create a message-only window to receive WM_HOTKEY messages
        var parameters = new HwndSourceParameters("TwinFocusHotkeyWindow")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    /// <summary>
    /// Register a global hotkey
    /// </summary>
    /// <param name="modifiers">Modifier keys (Ctrl, Alt, Shift, Win)</param>
    /// <param name="virtualKey">Virtual key code (e.g., F2 = 0x71)</param>
    public void Register(User32.HotkeyModifiers modifiers, uint virtualKey)
    {
        if (_hwndSource == null)
            throw new InvalidOperationException("HotkeyManager not initialized. Call Initialize() first.");

        // Unregister existing hotkey if any
        if (_isRegistered)
            Unregister();

        // Add MOD_NOREPEAT to prevent rapid-fire when holding the key
        var modsWithNoRepeat = modifiers | User32.HotkeyModifiers.MOD_NOREPEAT;

        bool success = User32.RegisterHotKey(
            _hwndSource.Handle,
            HOTKEY_ID,
            modsWithNoRepeat,
            virtualKey);

        if (!success)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to register hotkey. Error code: {error}. " +
                $"The hotkey may already be in use by another application.");
        }

        _isRegistered = true;
        _currentModifiers = modifiers;
        _currentVirtualKey = virtualKey;
    }

    /// <summary>
    /// Unregister the current hotkey
    /// </summary>
    public void Unregister()
    {
        if (_hwndSource != null && _isRegistered)
        {
            User32.UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    /// <summary>
    /// Window procedure to handle WM_HOTKEY messages
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == User32.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            // Hotkey pressed - raise event
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Get the current registered hotkey as a string
    /// </summary>
    public string GetHotkeyDescription()
    {
        if (!_isRegistered)
            return "None";

        var parts = new System.Collections.Generic.List<string>();

        var mods = _currentModifiers & ~User32.HotkeyModifiers.MOD_NOREPEAT;
        if ((mods & User32.HotkeyModifiers.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((mods & User32.HotkeyModifiers.MOD_ALT) != 0) parts.Add("Alt");
        if ((mods & User32.HotkeyModifiers.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((mods & User32.HotkeyModifiers.MOD_WIN) != 0) parts.Add("Win");

        parts.Add(GetKeyName(_currentVirtualKey));

        return string.Join("+", parts);
    }

    /// <summary>
    /// Convert virtual key code to readable name
    /// </summary>
    private static string GetKeyName(uint vk)
    {
        // Function keys
        if (vk >= 0x70 && vk <= 0x87)
            return $"F{vk - 0x6F}";

        // Number keys
        if (vk >= 0x30 && vk <= 0x39)
            return ((char)vk).ToString();

        // Letter keys
        if (vk >= 0x41 && vk <= 0x5A)
            return ((char)vk).ToString();

        // Special keys
        return vk switch
        {
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x1B => "Esc",
            0x20 => "Space",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0x2D => "Insert",
            0x2E => "Delete",
            _ => $"Key{vk:X2}"
        };
    }

    public void Dispose()
    {
        Unregister();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
