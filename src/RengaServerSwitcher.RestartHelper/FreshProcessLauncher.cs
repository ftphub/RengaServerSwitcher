using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RengaServerSwitcher.RestartHelper;

/// <summary>
/// Starts Renga with Explorer as the logical parent process. This is intended to
/// reproduce the process ancestry of a normal manual launch from Windows Explorer
/// as closely as possible while keeping the restart fully automatic.
/// </summary>
internal static class FreshProcessLauncher
{
    private const uint PROCESS_CREATE_PROCESS = 0x0080;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint TOKEN_QUERY = 0x0008;

    private const uint CREATE_NEW_PROCESS_GROUP = 0x00000200;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;

    private static readonly IntPtr PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = new(0x00020000);

    public static int Start(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            throw new FileNotFoundException("Renga executable not found.", executablePath);

        var workingDirectory = Path.GetDirectoryName(executablePath)
                               ?? throw new InvalidOperationException("Cannot determine Renga working directory.");

        var explorerPid = GetShellExplorerProcessId();
        if (explorerPid == 0)
            throw new InvalidOperationException("Windows Explorer shell process was not found.");

        IntPtr explorerHandle = IntPtr.Zero;
        IntPtr explorerToken = IntPtr.Zero;
        IntPtr environment = IntPtr.Zero;
        IntPtr attributeList = IntPtr.Zero;
        IntPtr parentHandleValue = IntPtr.Zero;

        try
        {
            explorerHandle = OpenProcess(
                PROCESS_CREATE_PROCESS | PROCESS_QUERY_LIMITED_INFORMATION,
                false,
                explorerPid);

            if (explorerHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Cannot open explorer.exe PID={explorerPid}.");

            if (!OpenProcessToken(explorerHandle, TOKEN_QUERY, out explorerToken))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken(explorer.exe) failed.");

            // Use Explorer's normal per-user environment, not RestartHelper's inherited block.
            if (!CreateEnvironmentBlock(out environment, explorerToken, false))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateEnvironmentBlock(explorer.exe token) failed.");

            nuint attributeListSize = 0;
            _ = InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeListSize);
            var initError = Marshal.GetLastWin32Error();
            if (attributeListSize == 0)
                throw new Win32Exception(initError, "Cannot determine STARTUPINFOEX attribute-list size.");

            attributeList = Marshal.AllocHGlobal(checked((int)attributeListSize));
            if (!InitializeProcThreadAttributeList(attributeList, 1, 0, ref attributeListSize))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "InitializeProcThreadAttributeList failed.");

            parentHandleValue = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(parentHandleValue, explorerHandle);

            if (!UpdateProcThreadAttribute(
                    attributeList,
                    0,
                    PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
                    parentHandleValue,
                    (nuint)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateProcThreadAttribute(PARENT_PROCESS) failed.");
            }

            var startupInfoEx = new STARTUPINFOEX();
            startupInfoEx.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            startupInfoEx.lpAttributeList = attributeList;

            var commandLine = new StringBuilder($"\"{executablePath}\"");
            var flags = CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_PROCESS_GROUP | EXTENDED_STARTUPINFO_PRESENT;

            HelperLogger.Write(
                "INFO",
                $"Explorer-parent launch: explorerPid={explorerPid}, cwd='{workingDirectory}', env=explorer.exe, flags=0x{flags:X8}.");

            var ok = CreateProcessW(
                executablePath,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                flags,
                environment,
                workingDirectory,
                ref startupInfoEx,
                out var processInfo);

            if (!ok)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcessW with explorer.exe parent failed.");

            try
            {
                return unchecked((int)processInfo.dwProcessId);
            }
            finally
            {
                if (processInfo.hThread != IntPtr.Zero)
                    CloseHandle(processInfo.hThread);
                if (processInfo.hProcess != IntPtr.Zero)
                    CloseHandle(processInfo.hProcess);
            }
        }
        finally
        {
            if (attributeList != IntPtr.Zero)
            {
                DeleteProcThreadAttributeList(attributeList);
                Marshal.FreeHGlobal(attributeList);
            }

            if (parentHandleValue != IntPtr.Zero)
                Marshal.FreeHGlobal(parentHandleValue);

            if (environment != IntPtr.Zero)
                DestroyEnvironmentBlock(environment);

            if (explorerToken != IntPtr.Zero)
                CloseHandle(explorerToken);

            if (explorerHandle != IntPtr.Zero)
                CloseHandle(explorerHandle);
        }
    }

    private static uint GetShellExplorerProcessId()
    {
        var shellWindow = GetShellWindow();
        if (shellWindow != IntPtr.Zero)
        {
            _ = GetWindowThreadProcessId(shellWindow, out var pid);
            if (pid != 0)
                return pid;
        }

        // Fallback for unusual shells/session states: choose explorer.exe from our session.
        var currentSession = Process.GetCurrentProcess().SessionId;
        foreach (var process in Process.GetProcessesByName("explorer"))
        {
            try
            {
                if (process.SessionId == currentSession)
                    return unchecked((uint)process.Id);
            }
            catch
            {
                // Ignore inaccessible explorer instances and continue.
            }
            finally
            {
                process.Dispose();
            }
        }

        return 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("userenv.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, [MarshalAs(UnmanagedType.Bool)] bool inherit);

    [DllImport("userenv.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyEnvironmentBlock(IntPtr environment);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(
        IntPtr attributeList,
        int attributeCount,
        int flags,
        ref nuint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(
        IntPtr attributeList,
        uint flags,
        IntPtr attribute,
        IntPtr value,
        nuint size,
        IntPtr previousValue,
        IntPtr returnSize);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr attributeList);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(
        string? applicationName,
        StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref STARTUPINFOEX startupInfo,
        out PROCESS_INFORMATION processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
