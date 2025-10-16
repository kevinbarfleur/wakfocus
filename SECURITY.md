# Security Policy

## Security Measures

WakFocus takes security seriously. We implement multiple layers of security scanning and transparency:

### Automated Security Scanning

- **CodeQL Analysis**: Every commit is automatically scanned for security vulnerabilities using GitHub's CodeQL engine
- **VirusTotal Scanning**: All release binaries are scanned against 70+ antivirus engines
- **Dependency Scanning**: Dependencies are monitored for known vulnerabilities

### Build Transparency

- **Automated Builds**: All releases are built automatically by GitHub Actions from public source code
- **SHA256 Checksums**: Every release includes checksums to verify file integrity
- **Source Available**: Complete source code is publicly available for inspection

### Verify Downloads

You can verify the integrity of downloaded executables:

1. Download `checksums.txt` from the release
2. Compare with your downloaded file:
   ```powershell
   Get-FileHash -Path WakFocus.exe -Algorithm SHA256
   ```

## What WakFocus Does

WakFocus is a simple window management utility that:
- Monitors active window changes
- Sends window focus events to Wakfu game clients via UDP
- Does NOT collect any personal data
- Does NOT make external network connections (except localhost UDP)
- Does NOT access files outside its own directory

## Reporting Security Issues

If you discover a security vulnerability, please report it responsibly:

1. **DO NOT** open a public GitHub issue
2. Email the maintainer or use GitHub's private vulnerability reporting
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if available)

We will respond within 48 hours and work on a fix promptly.

## False Positives

New executables sometimes trigger false positives from antivirus software due to:
- Lack of code signing certificate
- Generic heuristic patterns
- Low prevalence (new/unknown executable)

If you see warnings:
1. Check the VirusTotal scan results in the release notes
2. Verify the SHA256 checksum matches
3. Review the source code
4. Build from source if desired: `dotnet publish -c Release`

## Supported Versions

Only the latest release receives security updates. Please update to the newest version.
