using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

partial class Interop
{
    public class Kernel32
    {
        const string DLL_NAME = "kernel32.dll";

        [DllImport(DLL_NAME, SetLastError = true)]
        public static extern LibraryHandle LoadLibrary(string lpFileName);

        [DllImport(DLL_NAME, SetLastError = true)]
        public static extern IntPtr GetProcAddress(LibraryHandle hModule, string lpProcName);

        [DllImport(DLL_NAME, SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        public class LibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            protected LibraryHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                var ret = FreeLibrary(handle);
                handle = IntPtr.Zero;
                return ret;
            }
        }
    }
}