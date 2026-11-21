using System.IO;
using System.Runtime.InteropServices;

namespace WindowsBulkPhotoDetailsEditor;

internal static class PhotoMetadata
{
    private const uint ReadWrite = 2;
    private const ushort VariantString = 31; // VT_LPWSTR
    private static readonly Guid SummaryInformation = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9");
    private static readonly PropertyKey Title = new(SummaryInformation, 2);
    private static readonly PropertyKey Subject = new(SummaryInformation, 3);
    private static readonly PropertyKey Comment = new(SummaryInformation, 6);

    public static PhotoDetails Read(string path)
    {
        Guid interfaceId = typeof(IPropertyStore).GUID;
        CheckResult(SHGetPropertyStoreFromParsingName(path, IntPtr.Zero, 0, in interfaceId, out var store),
            "Open the Windows file properties");
        try
        {
            return new PhotoDetails(ReadValue(Title), ReadValue(Subject), ReadValue(Comment));
        }
        finally
        {
            Marshal.ReleaseComObject(store);
        }

        string ReadValue(PropertyKey key)
        {
            int result = store.GetValue(in key, out var value);
            try
            {
                CheckResult(result, "Read the Windows file property");
                return value.Type == VariantString ? Marshal.PtrToStringUni(value.Pointer) ?? "" : "";
            }
            finally
            {
                PropVariantClear(ref value);
            }
        }
    }

    public static void Save(string path, PhotoDetails saved, PhotoDetails current)
    {
        bool hasSharedTitleFallback = Path.GetExtension(path).ToLowerInvariant()
            is ".jpg" or ".jpeg" or ".jpe" or ".jfif" or ".tif" or ".tiff";

        Guid interfaceId = typeof(IPropertyStore).GUID;
        CheckResult(SHGetPropertyStoreFromParsingName(path, IntPtr.Zero, ReadWrite, in interfaceId, out var store),
            "Open the file properties for writing");

        try
        {
            SetChangedValue(store, Title, saved.Title, current.Title, "Title");
            // Windows can write Title to ImageDescription, which is also a fallback
            // for Subject. Keep an explicit XPSubject value, even when it is empty.
            if (current.Subject != saved.Subject || (hasSharedTitleFallback && current.Title != saved.Title))
            {
                SetValue(store, Subject, current.Subject, "Subject", keepEmpty: hasSharedTitleFallback);
            }
            SetChangedValue(store, Comment, saved.Comments, current.Comments, "Comments");
            CheckResult(store.Commit(), "Save the file properties");
        }
        finally
        {
            Marshal.ReleaseComObject(store);
        }
    }

    private static void SetChangedValue(IPropertyStore store, PropertyKey key, string saved, string current, string field)
    {
        if (current != saved)
        {
            SetValue(store, key, current, field);
        }
    }

    private static void SetValue(IPropertyStore store, PropertyKey key, string current, string field, bool keepEmpty = false)
    {
        // VT_EMPTY removes the property when the user clears the cell.
        var value = current.Length == 0 && !keepEmpty ? default : new PropVariant
        {
            Type = VariantString,
            Pointer = Marshal.StringToCoTaskMemUni(current)
        };

        try
        {
            CheckResult(store.SetValue(in key, in value), $"Set {field}");
        }
        finally
        {
            Marshal.FreeCoTaskMem(value.Pointer);
        }
    }

    private static void CheckResult(int result, string operation)
    {
        if (result != 0)
        {
            string? message = Marshal.GetExceptionForHR(result)?.Message;
            throw new IOException($"{operation} failed (0x{result:X8}). {message}".TrimEnd());
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHGetPropertyStoreFromParsingName(string path, IntPtr bindContext,
        uint flags, in Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore store);

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int PropVariantClear(ref PropVariant value);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PropertyKey(Guid formatId, uint propertyId)
    {
        private readonly Guid format = formatId;
        private readonly uint property = propertyId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort Type;
        private ushort reserved1, reserved2, reserved3;
        public IntPtr Pointer;
        private IntPtr unionPadding; // The native union can hold a count and a pointer.
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(in PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(in PropertyKey key, in PropVariant value);
        [PreserveSig] int Commit();
    }
}
