using System.Security.Cryptography;
using System.Text;

namespace HackathonCoordinator.WebAPI.Services
{
    /// <summary>
    /// Сервис для шифрования и дешифрования данных с использованием AES
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesEncryptionService(IConfiguration configuration)
        {
            var encryptionKey = configuration["Encryption:Key"] ??
                               throw new ArgumentException("Ключ шифрования не настроен");

            // Генерация 256-битного ключа из конфигурации
            _key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
            _iv = Encoding.UTF8.GetBytes("1234567890123456"); // 128-битный вектор инициализации
        }

        /// <summary>
        /// Шифрование текста
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);

            sw.Write(plainText);
            sw.Flush();
            cs.FlushFinalBlock();

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Дешифрование текста
        /// </summary>
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream(Convert.FromBase64String(encryptedText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}