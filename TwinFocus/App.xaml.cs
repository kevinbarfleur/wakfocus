using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using TwinFocus.Config;
using TwinFocus.Core;
using TwinFocus.NativeAPI;
using TwinFocus.UI;
using MessageBox = System.Windows.MessageBox;

namespace TwinFocus;

/// <summary>
/// TwinFocus - Lightweight window switcher for multi-accounting games
/// </summary>
public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;
    private HotkeyManager? _hotkeyManager;
    private FocusController? _focusController;
    private WindowEnumerator? _windowEnumerator;
    private WindowOrderer? _windowOrderer;
    private ConfigService? _configService;
    private AppConfig? _config;
    private bool _isEnabled = true;
    private IntPtr _lastActivatedWindow = IntPtr.Zero;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Load configuration
            _configService = new ConfigService();
            _config = _configService.Load();

            // Initialize core components
            _focusController = new FocusController(_config.CompatFallback);
            _focusController.ActivationFailed += OnActivationFailed;

            _windowEnumerator = new WindowEnumerator(_config);
            _windowOrderer = new WindowOrderer(_config);

            // Initialize hotkey manager
            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.Initialize();
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

            // Register default hotkey (F2)
            RegisterHotkey();

            // Create system tray icon
            CreateTrayIcon();

            // Don't show main window - tray-only app
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start TwinFocus: {ex.Message}", "TwinFocus Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void RegisterHotkey()
    {
        try
        {
            // Parse hotkey from config
            var modifiers = User32.HotkeyModifiers.None;
            if (_config?.Hotkey.Modifiers != null)
            {
                foreach (var mod in _config.Hotkey.Modifiers)
                {
                    modifiers |= mod.ToUpperInvariant() switch
                    {
                        "CTRL" => User32.HotkeyModifiers.MOD_CONTROL,
                        "ALT" => User32.HotkeyModifiers.MOD_ALT,
                        "SHIFT" => User32.HotkeyModifiers.MOD_SHIFT,
                        "WIN" => User32.HotkeyModifiers.MOD_WIN,
                        _ => User32.HotkeyModifiers.None
                    };
                }
            }

            // Parse virtual key
            uint vk = ParseVirtualKey(_config?.Hotkey.Key ?? "F2");

            _hotkeyManager?.Register(modifiers, vk);
        }
        catch (Exception ex)
        {
            string currentHotkey = _hotkeyManager?.GetHotkeyDescription() ?? _config?.Hotkey.Key ?? "F3";
            string message = $"Failed to register hotkey: {currentHotkey}\n\n" +
                           $"Error: {ex.Message}\n\n" +
                           $"The hotkey is already in use by another application.\n\n" +
                           $"To fix this:\n" +
                           $"1. Close other apps that may use this hotkey\n" +
                           $"2. Or edit the config file to use a different key:\n" +
                           $"   {_configService?.GetConfigPath()}\n\n" +
                           $"Suggested alternatives: F3, F4, F5, F6, Ctrl+Alt+W";

            MessageBox.Show(message, "TwinFocus - Hotkey Already In Use",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private uint ParseVirtualKey(string key)
    {
        // Function keys
        if (key.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(key.Substring(1), out int fNum) && fNum >= 1 && fNum <= 24)
        {
            return (uint)(0x70 + fNum - 1);
        }

        // Single character
        if (key.Length == 1)
        {
            char c = char.ToUpperInvariant(key[0]);
            if (c >= 'A' && c <= 'Z') return (uint)c;
            if (c >= '0' && c <= '9') return (uint)c;
        }

        // Default to F2
        return 0x71;
    }

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        if (!_isEnabled)
            return;

        try
        {
            // Find target windows
            var windows = _windowEnumerator?.FindTargetWindows();
            if (windows == null || windows.Count == 0)
                return;

            // Order windows
            var orderedWindows = _windowOrderer?.OrderWindows(windows);
            if (orderedWindows == null || orderedWindows.Count == 0)
                return;

            // Get next window
            var currentForeground = NativeAPI.User32.GetForegroundWindow();
            var nextWindow = _windowOrderer?.GetNext(orderedWindows, currentForeground);

            if (nextWindow != null)
            {
                // Activate the window
                bool success = await (_focusController?.ActivateWindowAsync(nextWindow) ?? System.Threading.Tasks.Task.FromResult(false));
                if (success)
                {
                    _lastActivatedWindow = nextWindow.Handle;
                }
            }
        }
        catch (Exception ex)
        {
            // Silent failure - don't interrupt gameplay
            System.Diagnostics.Debug.WriteLine($"Hotkey error: {ex.Message}");
        }
    }

    private void OnActivationFailed(object? sender, ActivationFailedEventArgs e)
    {
        // Could show OSD here - for now just log
        System.Diagnostics.Debug.WriteLine($"Failed to activate: {e.Window}");
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Text = "TwinFocus - Window Switcher",
            Visible = true
        };

        // Create a simple icon (you can replace this with an actual .ico file)
        _trayIcon.Icon = SystemIcons.Application;

        // Double-click to open settings
        _trayIcon.DoubleClick += (s, e) => OnOpenSettings(s, e);

        // Create context menu
        var contextMenu = new ContextMenuStrip();

        var enableItem = new ToolStripMenuItem("Enabled", null, OnToggleEnabled)
        {
            Checked = _isEnabled,
            CheckOnClick = true
        };
        contextMenu.Items.Add(enableItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        contextMenu.Items.Add("Next Window", null, (s, e) => OnHotkeyPressed(s, e));

        contextMenu.Items.Add(new ToolStripSeparator());

        contextMenu.Items.Add("Settings...", null, OnOpenSettings);

        contextMenu.Items.Add(new ToolStripSeparator());

        var hotkeyInfo = new ToolStripMenuItem($"Hotkey: {_hotkeyManager?.GetHotkeyDescription()}")
        {
            Enabled = false
        };
        contextMenu.Items.Add(hotkeyInfo);

        contextMenu.Items.Add(new ToolStripSeparator());

        contextMenu.Items.Add("Exit", null, OnExit);

        _trayIcon.ContextMenuStrip = contextMenu;
    }

    private void OnToggleEnabled(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            _isEnabled = item.Checked;
        }
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        try
        {
            if (_configService == null || _config == null)
                return;

            // Pause hotkey while settings window is open
            _hotkeyManager?.Unregister();

            var settingsWindow = new SettingsWindow(_configService, _config);
            settingsWindow.SettingsSaved += OnSettingsSaved;
            settingsWindow.Closed += (s, args) =>
            {
                // Re-register hotkey when settings closes (if not already re-registered by save)
                if (_hotkeyManager != null && !settingsWindow.WasSaved)
                {
                    RegisterHotkey();
                }
            };
            settingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open settings: {ex.Message}",
                "TwinFocus - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        // Reload config and re-register hotkey
        try
        {
            // Unregister old hotkey
            _hotkeyManager?.Unregister();

            // Reload config
            _config = _configService?.Load();

            // Re-initialize components with new config
            if (_config != null)
            {
                _focusController = new FocusController(_config.CompatFallback);
                _focusController.ActivationFailed += OnActivationFailed;

                _windowEnumerator = new WindowEnumerator(_config);
                _windowOrderer = new WindowOrderer(_config);
            }

            // Re-register hotkey with new settings
            RegisterHotkey();

            // Update tray menu to show new hotkey
            UpdateTrayIcon();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to apply settings: {ex.Message}",
                "TwinFocus - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon?.ContextMenuStrip == null)
            return;

        // Find and update the hotkey info label
        foreach (ToolStripItem item in _trayIcon.ContextMenuStrip.Items)
        {
            if (item is ToolStripMenuItem menuItem && !menuItem.Enabled)
            {
                menuItem.Text = $"Hotkey: {_hotkeyManager?.GetHotkeyDescription()}";
                break;
            }
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}

