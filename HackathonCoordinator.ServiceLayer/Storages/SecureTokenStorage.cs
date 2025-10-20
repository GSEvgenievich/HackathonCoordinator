using System.Security.Cryptography;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Storages
{
    public static class SecureTokenStorage
    {
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HackathonCoordinator",
            "token.dat"
        );

        public static void SaveToken(string token)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TokenFilePath));

            var data = Encoding.UTF8.GetBytes(token);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(TokenFilePath, encrypted);
        }

        public static string GetToken()
        {
            if (!File.Exists(TokenFilePath)) return null;

            var encrypted = File.ReadAllBytes(TokenFilePath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decrypted);
        }

        public static void ClearToken()
        {
            if (File.Exists(TokenFilePath))
                File.Delete(TokenFilePath);
        }
    }
}
