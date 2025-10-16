using System;
using System.Threading.Tasks;
using WakFocus.NativeAPI;

namespace WakFocus.Core;

/// <summary>
/// Controls window focus and activation with fallback strategies
/// </summary>
public class FocusController
{
    private readonly bool _compatFallback;

    public FocusController(bool compatFallback = false)
    {
        _compatFallback = compatFallback;
    }

    /// <summary>
    /// Event raised when activation fails even after fallbacks
    /// </summary>
    public event EventHandler<ActivationFailedEventArgs>? ActivationFailed;

    /// <summary>
    /// Activate a window, restoring it if minimized
    /// </summary>
    /// <param name="window">Window to activate</param>
    /// <returns>True if activation succeeded, false otherwise</returns>
    public async Task<bool> ActivateWindowAsync(WindowInfo window)
    {
        if (window.Handle == IntPtr.Zero)
            return false;

        // Step 1: Restore if minimized
        if (window.IsMinimized || User32.IsIconic(window.Handle))
        {
            User32.ShowWindowAsync(window.Handle, User32.ShowWindowCommands.SW_RESTORE);
            // Give it a moment to restore
            await Task.Delay(10);
        }

        // Step 2: Attempt primary activation
        if (TryActivate(window.Handle))
        {
            window.LastActivated = DateTime.Now;
            return true;
        }

        // Step 3: Compat fallback if enabled (attach thread input)
        if (_compatFallback)
        {
            if (TryActivateWithThreadAttach(window.Handle))
            {
                window.LastActivated = DateTime.Now;
                return true;
            }
        }

        // Step 4: Failed - flash taskbar and notify
        User32.FlashWindow(window.Handle, true);
        ActivationFailed?.Invoke(this, new ActivationFailedEventArgs(window));
        return false;
    }

    /// <summary>
    /// Primary activation attempt using SetForegroundWindow
    /// </summary>
    private bool TryActivate(IntPtr hWnd)
    {
        // Because we're called from a hotkey event, we have "recent user input"
        // which usually allows SetForegroundWindow to succeed
        bool result = User32.SetForegroundWindow(hWnd);

        // Verify activation worked
        if (result)
        {
            // Small delay to let the activation take effect
            System.Threading.Thread.Sleep(5);
            IntPtr foreground = User32.GetForegroundWindow();
            return foreground == hWnd;
        }

        return false;
    }

    /// <summary>
    /// Fallback activation by temporarily attaching to foreground thread
    /// This is a documented workaround for activation restrictions
    /// </summary>
    private bool TryActivateWithThreadAttach(IntPtr hWnd)
    {
        try
        {
            // Get foreground window's thread
            IntPtr fgWindow = User32.GetForegroundWindow();
            if (fgWindow == IntPtr.Zero)
                return false;

            uint fgThreadId = User32.GetWindowThreadProcessId(fgWindow, out _);
            uint currentThreadId = User32.GetCurrentThreadId();

            if (fgThreadId == currentThreadId)
                return TryActivate(hWnd); // Already same thread

            // Attach our thread to the foreground thread
            bool attached = User32.AttachThreadInput(currentThreadId, fgThreadId, true);

            try
            {
                // Try activation while attached
                bool result = User32.SetForegroundWindow(hWnd);

                if (result)
                {
                    System.Threading.Thread.Sleep(5);
                    IntPtr foreground = User32.GetForegroundWindow();
                    return foreground == hWnd;
                }

                return false;
            }
            finally
            {
                // Always detach
                if (attached)
                {
                    User32.AttachThreadInput(currentThreadId, fgThreadId, false);
                }
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if a window is still valid and activatable
    /// </summary>
    public bool IsWindowValid(IntPtr hWnd)
    {
        // Check if handle is still valid
        if (hWnd == IntPtr.Zero)
            return false;

        // Try to get window info - will fail if window was closed
        try
        {
            return User32.IsWindowVisible(hWnd) || User32.IsIconic(hWnd);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Event args for activation failure
/// </summary>
public class ActivationFailedEventArgs : EventArgs
{
    public WindowInfo Window { get; }

    public ActivationFailedEventArgs(WindowInfo window)
    {
        Window = window;
    }
}
