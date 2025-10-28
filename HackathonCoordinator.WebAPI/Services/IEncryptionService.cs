namespace HackathonCoordinator.WebAPI.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string encryptedText);
    }
}
