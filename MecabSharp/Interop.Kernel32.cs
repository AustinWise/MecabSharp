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
        public static DllDirectorySafeHandle AddToSearchPath(string path)
        {
            try
            {
                var ret = AddDllDirectory(path);
                if (ret.IsInvalid)
                    throw new Win32Exception();
                if (!SetDefaultDllDirectories(SearchLocation.SEARCH_USER_DIRS | SearchLocation.DEFAULT_DIRS))
                    throw new Win32Exception();
                return ret;
            }
            catch (EntryPointNotFoundException ex)
            {
                throw new Exception("Vista, 7, Server 2008, and Server 2008 R2 need KB2533623 installed. XP and earlier not supported.", ex);
            }
        }

        const string DLL_NAME = "kernel32.dll";

        [DllImport(DLL_NAME, SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        [DllImport(DLL_NAME, SetLastError = true)]
        static extern DllDirectorySafeHandle AddDllDirectory([MarshalAs(UnmanagedType.LPWStr)] string lpPathName);

        [DllImport(DLL_NAME, SetLastError = true)]
        static extern bool RemoveDllDirectory(IntPtr Cookie);

        [DllImport(DLL_NAME, SetLastError = true)]
        static extern bool SetDefaultDllDirectories(SearchLocation DirectoryFlags);

        [Flags]
        enum SearchLocation : uint
        {
            None = 0,
            APPLICATION_DIR = 0x00000200,
            DEFAULT_DIRS = 0x00001000,
            SEARCH_SYSTEM32 = 0x00000800,
            SEARCH_USER_DIRS = 0x00000400,
        }

        public class DllDirectorySafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            protected DllDirectorySafeHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                RemoveDllDirectory(handle);
                return true;
            }
        }
    }
}