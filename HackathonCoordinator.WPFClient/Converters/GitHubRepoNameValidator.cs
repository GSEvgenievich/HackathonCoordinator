using System.Text.RegularExpressions;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class GitHubRepoNameValidator
    {
        private static readonly Regex _validRepoNameRegex = new Regex(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

        public static bool IsValidRepoName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            // GitHub правила для названий репозиториев
            if (name.Length > 100) return false; // Максимальная длина
            if (name.StartsWith(".") || name.EndsWith(".")) return false;
            if (name.StartsWith("-") || name.EndsWith("-")) return false;
            if (name.Contains("..")) return false;
            if (name.Contains(" ")) return false;
            if (name.Contains("--")) return false;

            return _validRepoNameRegex.IsMatch(name);
        }

        public static string SanitizeRepoName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;

            // Удаляем недопустимые символы
            var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9._-]", "");

            // Убираем точки и дефисы в начале/конце
            sanitized = sanitized.Trim('.', '-');

            // Заменяем множественные точки и дефисы
            sanitized = Regex.Replace(sanitized, @"\.{2,}", ".");
            sanitized = Regex.Replace(sanitized, @"-{2,}", "-");

            // Ограничиваем длину
            if (sanitized.Length > 100)
                sanitized = sanitized.Substring(0, 100);

            return sanitized;
        }
    }
}
