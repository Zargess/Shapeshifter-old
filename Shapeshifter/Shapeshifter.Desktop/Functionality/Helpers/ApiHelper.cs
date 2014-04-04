using System;
using System.Runtime.InteropServices;

namespace Shapeshifter.Desktop.Functionality.Helpers
{
    public static class ApiHelper
    {
        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            IntPtr pointer = IntPtr.Zero;
            try
            {
                int size = Marshal.SizeOf(typeof (T));
                pointer = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, pointer, size);
                object obj = Marshal.PtrToStructure(pointer, typeof (T));
                return (T) obj;
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(pointer);
            }
        }

        public static byte[] StructureToByteArray<T>(T obj) where T : struct
        {
            IntPtr pointer = IntPtr.Zero;
            try
            {
                int size = Marshal.SizeOf(typeof (T));
                pointer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, pointer, true);
                var bytes = new byte[size];
                Marshal.Copy(pointer, bytes, 0, size);
                return bytes;
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(pointer);
            }
        }
    }
}