namespace HackathonCoordinator.WebAPI.Services
{
    /// <summary>
    /// Интерфейс для сервиса шифрования
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Шифрование текста
        /// </summary>
        string Encrypt(string plainText);

        /// <summary>
        /// Дешифрование текста
        /// </summary>
        string Decrypt(string encryptedText);
    }
}