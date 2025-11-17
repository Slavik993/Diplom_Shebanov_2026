using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class EncryptionAES
{
    private static readonly string DefaultKey = "SDGVM2026Secret!"; // 16+ символов

    public static byte[] Encrypt(byte[] data, string key = null)
    {
        if (key == null) key = DefaultKey;
        if (data == null || data.Length == 0) throw new ArgumentException("Data is empty");

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16]; // В продакшене — рандомный IV!

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }

    public static byte[] Decrypt(byte[] data, string key = null)
    {
        if (key == null) key = DefaultKey;
        if (data == null || data.Length == 0) throw new ArgumentException("Data is empty");

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16];

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream(data))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (MemoryStream result = new MemoryStream())
            {
                cs.CopyTo(result);
                return result.ToArray();
            }
        }
    }

    // Пример использования: шифровать файл
    public static void EncryptFile(string inputPath, string outputPath)
    {
        byte[] data = File.ReadAllBytes(inputPath);
        byte[] encrypted = Encrypt(data);
        File.WriteAllBytes(outputPath, encrypted);
    }
}
