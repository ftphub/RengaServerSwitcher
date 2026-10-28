using System.Runtime.InteropServices;
using System.Text;

namespace RengaServerSwitcher.RestartHelper;

internal static class DpapiProtector
{
    private const int CryptProtectUiForbidden = 0x1;
    [StructLayout(LayoutKind.Sequential)] private struct DataBlob { public int cbData; public IntPtr pbData; }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob pDataIn, IntPtr ppszDataDescr, IntPtr pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DataBlob pDataOut);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr LocalFree(IntPtr hMem);

    public static string Unprotect(string? protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText)) return string.Empty;
        var data = Convert.FromBase64String(protectedText);
        var ptr = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, ptr, data.Length);
        var input = new DataBlob { cbData = data.Length, pbData = ptr };
        try
        {
            if (!CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CryptProtectUiForbidden, out var output))
                throw new InvalidOperationException($"DPAPI unprotect failed. Win32={Marshal.GetLastWin32Error()}");
            try
            {
                var bytes = new byte[output.cbData];
                Marshal.Copy(output.pbData, bytes, 0, output.cbData);
                return Encoding.UTF8.GetString(bytes);
            }
            finally { if (output.pbData != IntPtr.Zero) LocalFree(output.pbData); }
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }
}
