using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TwinFocus.NativeAPI;

/// <summary>
/// P/Invoke declarations for Kernel32.dll - Process and thread management
/// </summary>
public static class Kernel32
{
    private const string DllName = "kernel32.dll";

    // Process access rights
    [Flags]
    public enum ProcessAccessFlags : uint
    {
        QueryInformation = 0x0400,
        QueryLimitedInformation = 0x1000,
        Synchronize = 0x00100000
    }

    // OpenProcess - Open a handle to a process
    [DllImport(DllName, SetLastError = true)]
    public static extern IntPtr OpenProcess(
        ProcessAccessFlags dwDesiredAccess,
        bool bInheritHandle,
        uint dwProcessId);

    // CloseHandle - Close a handle
    [DllImport(DllName, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    // QueryFullProcessImageName - Get full path of process executable
    [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryFullProcessImageName(
        IntPtr hProcess,
        uint dwFlags,
        StringBuilder lpExeName,
        ref uint lpdwSize);

    // GetCurrentThreadId - Get current thread ID
    [DllImport(DllName)]
    public static extern uint GetCurrentThreadId();
}
