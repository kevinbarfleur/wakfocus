# WakFocus - Window Switcher for Wakfu Multi-Accounting

[![CodeQL](https://github.com/kevinbarfleur/wakfocus/workflows/CodeQL%20Security%20Scan/badge.svg)](https://github.com/kevinbarfleur/wakfocus/actions/workflows/codeql.yml)
[![Security Policy](https://img.shields.io/badge/security-policy-blue.svg)](SECURITY.md)

Quickly switch between multiple Wakfu windows using a hotkey. Press F4 to cycle through your game windows.

## Security

**WakFocus is safe and transparent:**
- Source code scanned with CodeQL security analysis
- Release binaries scanned with VirusTotal (70+ antivirus engines)
- Built automatically by GitHub Actions
- SHA256 checksums provided for all releases
- See [SECURITY.md](SECURITY.md) for full details

## Features

- **Fast Window Switching**: Press F4 to cycle through Wakfu windows
- **Drag-and-Drop Ordering**: Set your preferred window order
- **System Tray**: Easy access to settings
- **Safe**: Only switches OS focus - no automation or memory manipulation

## Quick Start

1. **Download** the latest release
2. **Run WakFocus.exe**
3. **Configure**:
   - Right-click the tray icon → Settings
   - Process name: `java.exe`
   - Click "Scan Windows" to detect Wakfu windows
   - Drag to reorder windows
4. **Launch Wakfu** and press F4 to switch windows

## Important Notes

- If Wakfu runs as administrator, WakFocus must also run elevated
- Safe to use - only switches window focus, no automation

## Requirements

- Windows 10/11

## License

MIT License
