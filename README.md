# WakFocus - Window Switcher for Multi-Accounting

A lightweight, fast, and safe window switcher designed for multi-accounting in games like Wakfu. Press F4 (configurable) to instantly cycle through your game windows.

## Features

- **Global Hotkey**: Configurable hotkey (default F4) with easy key capture interface
- **Smart Window Detection**: Filters by process name and optional title regex
- **Manual Window Ordering**: Drag-and-drop interface to set your preferred cycle order
- **Settings UI**: User-friendly window for all configuration options
- **System Tray UI**: Quick access to settings, enable/disable, and exit
- **Custom Icon**: Includes a custom tray icon for easy identification
- **Safe & ToS-Compliant**: Only switches OS focus - no input broadcasting, no memory manipulation
- **Robust Activation**: Handles minimized windows, with fallback strategies for OS restrictions

## Quick Start

1. **Build the application**:
   ```bash
   dotnet build WakFocus.sln -c Release
   ```

2. **Run WakFocus**:
   ```bash
   dotnet run --project WakFocus/WakFocus.csproj
   ```
   Or navigate to `WakFocus/bin/Release/net8.0-windows/WakFocus.exe`

3. **Configure via Settings**:
   - Right-click the tray icon and select "Settings"
   - Set your target process name (e.g., java.exe for Wakfu)
   - Configure your preferred hotkey using the key capture interface
   - Use "Scan Windows" to detect and drag-reorder your windows

4. **Use the hotkey**:
   - Launch your game windows (e.g., multiple java.exe instances for Wakfu)
   - Press your configured hotkey (default **F4**) to cycle through them

## Configuration

Configuration is stored in `%APPDATA%\WakFocus\config.json`

### Settings Window

The easiest way to configure WakFocus is through the Settings window:
- **Global Hotkey**: Click the hotkey box and press your desired key combination
- **Window Order**: Use "Scan Windows" to detect target windows, then drag to reorder
- **Target Windows**:
  - Process Name: Enter the executable name (e.g., java.exe)
  - Window Title Contains: Optional regex pattern to filter by title
- **Options**:
  - Include minimized windows: Restore and activate minimized windows
  - Enable compatibility fallback: Uses AttachThreadInput for stubborn windows

### Example Configuration File

```json
{
  "version": 1,
  "hotkey": {
    "modifiers": ["NONE"],
    "key": "F4"
  },
  "targets": [
    { "type": "process", "match": "java.exe" }
  ],
  "ordering": {
    "mode": "manual",
    "manualOrder": [123456, 234567, 345678]
  },
  "includeMinimized": true,
  "skipInvisible": true,
  "compatFallback": false
}
```

### Configuration Options

**Hotkey**:
- `modifiers`: Array of "CTRL", "ALT", "SHIFT", "WIN", or "NONE"
- `key`: Virtual key name like "F2", "F3", "F4", "A", etc.

**Targets** (window matchers):
- `type`: "process" (exact name) or "title" (regex)
- `match`: String to match against

**Ordering**:
- `mode`: "manual" (uses manualOrder list of window handles)
- `manualOrder`: Array of window handle integers in desired cycle order

**Options**:
- `includeMinimized`: Include minimized windows in cycle
- `skipInvisible`: Skip invisible windows
- `compatFallback`: Enable thread-attach fallback for stubborn activation issues

## Testing

Test with any windows on your system:

1. Open multiple Notepad windows:
   ```bash
   notepad
   notepad
   notepad
   ```

2. Configure WakFocus:
   - Right-click tray icon → Settings
   - Set Process Name to: `notepad.exe`
   - Click "Scan Windows" to detect them
   - Drag to reorder if desired
   - Click Save

3. Press your configured hotkey (default F4) to cycle through Notepad windows

## Architecture

```
WakFocus/
├── NativeAPI/          # Windows API P/Invoke (User32, Kernel32)
├── Core/               # Core logic
│   ├── HotkeyManager   # Global hotkey registration (WM_HOTKEY)
│   ├── WindowEnumerator # Window discovery and filtering
│   ├── FocusController  # Window activation with fallbacks
│   ├── WindowOrderer    # Manual ordering by window handle list
│   └── WindowInfo       # Window metadata model
├── Config/             # Configuration system
│   ├── AppConfig       # Configuration models
│   └── ConfigService   # JSON load/save
├── UI/                 # User Interface
│   └── SettingsWindow  # Settings dialog with drag-drop ordering
├── Resources/          # Application resources
│   └── icon.ico        # Custom tray icon
└── App.xaml.cs         # Main app with tray UI
```

## Safety & Game ToS

WakFocus is designed to be safe and compliant:

- ✅ Uses only public Windows APIs for window switching
- ✅ No input injection or broadcasting
- ✅ No memory scanning or process injection
- ✅ No automation of gameplay actions
- ⚠️ Always verify game rules - some games may have restrictions on any third-party tools

## Important Notes

**Elevated Games**: If your game runs as administrator, WakFocus must also run elevated to interact with it (Windows security restriction).

**Hotkey Conflicts**: If your chosen hotkey is already in use, WakFocus will display an error with suggested alternatives. Use the Settings window to easily configure a different hotkey.

**Fullscreen Games**: Works with borderless/fullscreen, though OS restrictions may occasionally prevent activation (fallback flashes the taskbar).

## Future Enhancements

- macOS support (using Carbon EventHotKey and CGWindowListCopyWindowInfo)
- Per-monitor DPI awareness improvements
- Multi-profile support for different game configurations

## Requirements

- Windows 10 22H2+ or Windows 11 23H2+
- .NET 8.0 Runtime

## License

MIT License - Free to use and modify.

## Acknowledgments

Built to the specification provided, implementing best practices for Windows window management, focus activation, and global hotkeys.
