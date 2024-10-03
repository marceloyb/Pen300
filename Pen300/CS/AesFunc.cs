using System.Security.Cryptography;
using System.Text;

namespace Pen300.CS
{
    public class AesFunc
    {
        static (byte[], byte[], byte[]) EncryptShellcode(byte[] shellcode)
        {
            byte[] key;
            byte[] iv;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateKey();
                aesAlg.GenerateIV();
                key = aesAlg.Key;
                iv = aesAlg.IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                byte[] encrypted;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(shellcode, 0, shellcode.Length);
                        csEncrypt.FlushFinalBlock();
                    }
                    encrypted = msEncrypt.ToArray();
                }

                return (encrypted, key, iv);
            }
        }

        static void PrintByteArray(string arrayName, byte[] array)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("byte[] {0} = new byte[] {{ ", arrayName);
            builder.AppendLine();

            for (int i = 0; i < array.Length; i++)
            {
                builder.AppendFormat("0x{0:x2}", array[i]);

                if (i < array.Length - 1)
                    builder.Append(",");

                if ((i + 1) % 12 == 0 && i != array.Length - 1)
                    builder.AppendLine();
            }

            builder.Append(" };");

            Console.WriteLine(builder.ToString());
        }

        static byte[] DecryptShellcode(byte[] encryptedShellcode, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                byte[] decrypted;

                using (MemoryStream msDecrypt = new MemoryStream(encryptedShellcode))
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

        public static void Main(string[] args)
        {
            // calc shellcode
            // msfvenom -p windows/x64/exec cmd=calc.exe exitfunc=thread -f csharp
            if (args.Length == 0)
            {
                Console.WriteLine("need shellcode as arg");
            }

            byte[] buf = File.ReadAllBytes(args[0]);

            byte[] encryptedShellcode;
            byte[] key;
            byte[] iv;

            (encryptedShellcode, key, iv) = EncryptShellcode(buf);

            PrintByteArray("encrypted", encryptedShellcode);
            PrintByteArray("key", key);
            PrintByteArray("iv", iv);
        }
    }
}