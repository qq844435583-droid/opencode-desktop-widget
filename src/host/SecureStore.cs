using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenCodeDesktopWidget;

internal static class SecureStore
{
    private const int CryptProtectUiForbidden = 0x1;

    public static string Protect(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var input = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectBytes(input);
        return "enc:" + Convert.ToBase64String(protectedBytes);
    }

    public static string Reveal(string stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return "";
        if (stored.StartsWith("plain:", StringComparison.Ordinal))
            return Encoding.UTF8.GetString(Convert.FromBase64String(stored[6..]));
        if (!stored.StartsWith("enc:", StringComparison.Ordinal)) return stored;

        try
        {
            var encrypted = Convert.FromBase64String(stored[4..]);
            return Encoding.UTF8.GetString(UnprotectBytes(encrypted));
        }
        catch (Exception error)
        {
            throw new InvalidOperationException(AppText.T("旧版登录凭据无法解密，请重新登录 OpenCode。", "Legacy sign-in credentials could not be decrypted. Please sign in to OpenCode again."), error);
        }
    }

    private static byte[] ProtectBytes(byte[] input)
    {
        var inputBlob = ToBlob(input);
        try
        {
            if (!CryptProtectData(ref inputBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CryptProtectUiForbidden, out var outputBlob))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            try { return FromBlob(outputBlob); }
            finally { LocalFree(outputBlob.Data); }
        }
        finally { Marshal.FreeHGlobal(inputBlob.Data); }
    }

    private static byte[] UnprotectBytes(byte[] input)
    {
        var inputBlob = ToBlob(input);
        try
        {
            if (!CryptUnprotectData(ref inputBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CryptProtectUiForbidden, out var outputBlob))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            try { return FromBlob(outputBlob); }
            finally { LocalFree(outputBlob.Data); }
        }
        finally { Marshal.FreeHGlobal(inputBlob.Data); }
    }

    private static DataBlob ToBlob(byte[] bytes)
    {
        var data = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, data, bytes.Length);
        return new DataBlob { Length = bytes.Length, Data = data };
    }

    private static byte[] FromBlob(DataBlob blob)
    {
        var bytes = new byte[blob.Length];
        Marshal.Copy(blob.Data, bytes, 0, blob.Length);
        return bytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Length;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob dataIn, string? description, IntPtr optionalEntropy,
        IntPtr reserved, IntPtr promptStruct, int flags, out DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr description, IntPtr optionalEntropy,
        IntPtr reserved, IntPtr promptStruct, int flags, out DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
