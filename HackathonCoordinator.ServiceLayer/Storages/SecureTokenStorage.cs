using System.Security.Cryptography;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Storages
{
    public static class SecureTokenStorage
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HackathonCoordinator"
        );

        // Основные методы для JWT токена
        public static void SaveToken(string token)
        {
            var tokenPath = Path.Combine(AppDataPath, "token.dat");
            SaveEncryptedData(tokenPath, token);
        }

        public static string GetToken()
        {
            var tokenPath = Path.Combine(AppDataPath, "token.dat");
            return LoadEncryptedData(tokenPath);
        }

        public static void ClearToken()
        {
            var tokenPath = Path.Combine(AppDataPath, "token.dat");
            DeleteFileIfExists(tokenPath);
        }

        // Методы для временного state (OAuth защита от CSRF)
        public static void SaveTempState(string state)
        {
            var statePath = Path.Combine(AppDataPath, "github_state.dat");
            SaveEncryptedData(statePath, state);
        }

        public static string GetTempState()
        {
            var statePath = Path.Combine(AppDataPath, "github_state.dat");
            return LoadEncryptedData(statePath);
        }

        public static void ClearTempState()
        {
            var statePath = Path.Combine(AppDataPath, "github_state.dat");
            DeleteFileIfExists(statePath);
        }

        // Методы для GitHub Access Token (если нужно хранить отдельно)
        public static void SaveGitHubToken(string token)
        {
            var path = Path.Combine(AppDataPath, "github_token.dat");
            SaveEncryptedData(path, token);
        }

        public static string GetGitHubToken()
        {
            var path = Path.Combine(AppDataPath, "github_token.dat");
            return LoadEncryptedData(path);
        }

        public static void ClearGitHubToken()
        {
            var path = Path.Combine(AppDataPath, "github_token.dat");
            DeleteFileIfExists(path);
        }

        // Основные приватные методы
        private static void SaveEncryptedData(string filePath, string data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                var dataBytes = Encoding.UTF8.GetBytes(data);
                var encryptedData = ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(filePath, encryptedData);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                System.Diagnostics.Debug.WriteLine($"Error saving encrypted data: {ex.Message}");
                throw;
            }
        }

        private static string LoadEncryptedData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var encryptedData = File.ReadAllBytes(filePath);
                var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading encrypted data: {ex.Message}");
                return null;
            }
        }

        private static void DeleteFileIfExists(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        // Метод для полной очистки всех данных (при logout)
        public static void ClearAllData()
        {
            ClearToken();
            ClearTempState();
            ClearGitHubToken();

            // Также можно очистить другие временные данные
            var tempFiles = Directory.GetFiles(AppDataPath, "*.dat");
            foreach (var file in tempFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Игнорируем ошибки удаления
                }
            }
        }

        // Метод для проверки наличия токена
        public static bool HasToken()
        {
            return !string.IsNullOrEmpty(GetToken());
        }

        // Метод для получения информации о хранимых данных (для отладки)
        public static StorageInfo GetStorageInfo()
        {
            return new StorageInfo
            {
                HasJwtToken = HasToken(),
                HasGitHubToken = !string.IsNullOrEmpty(GetGitHubToken()),
                StoragePath = AppDataPath,
                Files = Directory.Exists(AppDataPath) ? Directory.GetFiles(AppDataPath) : new string[0]
            };
        }
    }

    public class StorageInfo
    {
        public bool HasJwtToken { get; set; }
        public bool HasGitHubToken { get; set; }
        public string StoragePath { get; set; }
        public string[] Files { get; set; }
    }
}