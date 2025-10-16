# TwinFocus - Window Switcher for Multi-Accounting

A lightweight, fast, and safe window switcher designed for multi-accounting in games like Wakfu and Dofus. Press F2 to instantly cycle through your game windows.

## Features

- **Global Hotkey**: F2 (configurable) to cycle through target windows
- **Smart Window Detection**: Filters by process name, class, or title regex
- **Multiple Ordering Modes**:
  - Screen layout (top → bottom) - perfect for stacked fullscreen windows
  - Last active
  - Manual order
- **System Tray UI**: Enable/disable, manual window switching, exit
- **Safe & ToS-Compliant**: Only switches OS focus - no input broadcasting, no memory manipulation
- **Robust Activation**: Handles minimized windows, with fallback strategies for OS restrictions

## Quick Start

1. **Build the application**:
   ```bash
   dotnet build TwinFocus.sln -c Release
   ```

2. **Run TwinFocus**:
   ```bash
   dotnet run --project TwinFocus/TwinFocus.csproj
   ```
   Or navigate to `TwinFocus/bin/Debug/net8.0-windows/TwinFocus.exe`

3. **Use the hotkey**:
   - Launch your game windows (e.g., multiple java.exe instances for Wakfu/Dofus)
   - Press **F2** to cycle through them

4. **System Tray**:
   - Right-click the tray icon to access settings
   - Enable/Disable cycling
   - Manually trigger window switching
   - Exit the application

## Configuration

Configuration is stored in `%APPDATA%\TwinFocus\config.json`

### Default Configuration

```json
{
  "version": 1,
  "hotkey": {
    "modifiers": ["NONE"],
    "key": "F2"
  },
  "targets": [
    { "type": "process", "match": "java.exe" },
    { "type": "class", "match": "SunAwtFrame|LWJGL" },
    { "type": "title", "match": "(?i)wakfu|dofus" }
  ],
  "ordering": {
    "mode": "screenYThenX"
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
- `type`: "process" (exact name), "class" (regex), or "title" (regex)
- `match`: String to match against

**Ordering**:
- `mode`: "screenYThenX", "lastActive", or "manual"

**Options**:
- `includeMinimized`: Include minimized windows in cycle
- `skipInvisible`: Skip invisible windows
- `compatFallback`: Enable thread-attach fallback for stubborn activation issues

## Testing

Test with any windows on your system:

```bash
# Open multiple Notepad windows
notepad
notepad
notepad

# Update config to target Notepad
# Edit %APPDATA%\TwinFocus\config.json:
{
  "targets": [
    { "type": "process", "match": "notepad.exe" }
  ]
}

# Press F2 to cycle through Notepad windows
```

## Architecture

```
TwinFocus/
├── NativeAPI/          # Windows API P/Invoke (User32, Kernel32)
├── Core/               # Core logic
│   ├── HotkeyManager   # Global hotkey registration (WM_HOTKEY)
│   ├── WindowEnumerator # Window discovery and filtering
│   ├── FocusController  # Window activation with fallbacks
│   ├── WindowOrderer    # Ordering strategies
│   └── WindowInfo       # Window metadata model
├── Config/             # Configuration system
│   ├── AppConfig       # Configuration models
│   └── ConfigService   # JSON load/save
└── App.xaml.cs         # Main app with tray UI
```

## Safety & Game ToS

TwinFocus is designed to be safe and compliant:

- ✅ Uses only public Windows APIs for window switching
- ✅ No input injection or broadcasting
- ✅ No memory scanning or process injection
- ✅ No automation of gameplay actions
- ⚠️ Always verify game rules - some games may have restrictions on any third-party tools

## Important Notes

**Elevated Games**: If your game runs as administrator, TwinFocus must also run elevated to interact with it (Windows security restriction).

**Hotkey Conflicts**: If F2 is already in use, TwinFocus will display an error with suggested alternatives. Edit the config to use a different hotkey (F3, F4, F5, F6, or Ctrl+Alt+W are good choices).

**Fullscreen Games**: Works with borderless/fullscreen, though OS restrictions may occasionally prevent activation (fallback flashes the taskbar).

## Future Enhancements

- Settings UI window for easier configuration
- OSD (on-screen display) showing switched window name
- macOS support (using Carbon EventHotKey and CGWindowListCopyWindowInfo)
- Custom tray icon
- Per-monitor DPI awareness improvements

## Requirements

- Windows 10 22H2+ or Windows 11 23H2+
- .NET 8.0 Runtime

## License

MIT License - Free to use and modify.

## Acknowledgments

Built to the specification provided, implementing best practices for Windows window management, focus activation, and global hotkeys.
