using System;
using System.Collections.Generic;
using System.IO;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class MessageAttachmentDto
    {
        private static readonly Dictionary<string, string> _fileIconMap = new()
        {
            // Видео
            [".mp4"] = "🎬",
            [".avi"] = "🎬",
            [".mkv"] = "🎬",
            [".mov"] = "🎬",
            [".wmv"] = "🎬",
            [".flv"] = "🎬",
            [".webm"] = "🎬",
            [".mpeg"] = "🎬",
            [".mpg"] = "🎬",
            [".m4v"] = "🎬",
            [".3gp"] = "🎬",
            [".ogv"] = "🎬",

            // Аудио
            [".mp3"] = "🎵",
            [".wav"] = "🎵",
            [".flac"] = "🎵",
            [".ogg"] = "🎵",
            [".aac"] = "🎵",
            [".m4a"] = "🎵",
            [".wma"] = "🎵",

            // Изображения
            [".jpg"] = "🖼️",
            [".jpeg"] = "🖼️",
            [".png"] = "🖼️",
            [".gif"] = "🎞️",
            [".bmp"] = "🖼️",
            [".webp"] = "🖼️",
            [".svg"] = "📐",
            [".ico"] = "🎨",

            // Документы
            [".pdf"] = "📕",
            [".doc"] = "📘",
            [".docx"] = "📘",
            [".odt"] = "📘",
            [".rtf"] = "📘",
            [".xls"] = "📗",
            [".xlsx"] = "📗",
            [".ods"] = "📗",
            [".csv"] = "📗",
            [".ppt"] = "📙",
            [".pptx"] = "📙",
            [".odp"] = "📙",
            [".txt"] = "📃",
            [".md"] = "📃",
            [".log"] = "📋",

            // Архивы
            [".zip"] = "🗜️",
            [".rar"] = "🗜️",
            [".7z"] = "🗜️",
            [".tar"] = "🗜️",
            [".gz"] = "🗜️",
            [".bz2"] = "🗜️",
            [".xz"] = "🗜️",

            // Код
            [".cs"] = "💻",
            [".cpp"] = "💻",
            [".c"] = "💻",
            [".py"] = "🐍",
            [".js"] = "💻",
            [".ts"] = "💻",
            [".html"] = "🌐",
            [".css"] = "🎨",
            [".json"] = "📋",
            [".xml"] = "📋",
            [".sql"] = "🗄️",
            [".java"] = "☕",
            [".php"] = "🐘",
            [".rb"] = "💎",

            // Дизайн
            [".psd"] = "🎨",
            [".ai"] = "🎨",
            [".fig"] = "🎨",

            // Шрифты
            [".ttf"] = "🔤",
            [".otf"] = "🔤",
            [".woff"] = "🔤",

            // Системные
            [".exe"] = "⚙️",
            [".msi"] = "⚙️",
            [".dll"] = "🔧"
        };

        public int Id { get; set; }
        public int MessageId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string FilePath { get; set; }
        public string? ThumbnailBase64 { get; set; }
        public DateTime UploadedAt { get; set; }
        public string FileExtension => Path.GetExtension(FileName).ToUpper();
        public bool IsImage => ContentType?.StartsWith("image/") == true;

        public string FormattedSize => FileSize switch
        {
            < 1024 => $"{FileSize} B",
            < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{FileSize / (1024.0 * 1024):F1} MB",
            _ => $"{FileSize / (1024.0 * 1024 * 1024):F1} GB"
        };

        public string FileIcon
        {
            get
            {
                if (IsImage) return "🖼️";

                if (_fileIconMap.TryGetValue(FileExtension.ToLower(), out var icon))
                    return icon;

                // По ContentType
                if (ContentType?.StartsWith("video/") == true) return "🎬";
                if (ContentType?.StartsWith("audio/") == true) return "🎵";

                return "📎";
            }
        }
    }
}