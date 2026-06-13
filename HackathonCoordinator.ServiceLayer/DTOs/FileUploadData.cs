namespace HackathonCoordinator.ServiceLayer.Services
{
    public class FileUploadData
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
            [".ts"] = "🎬",

            // Аудио
            [".mp3"] = "🎵",
            [".wav"] = "🎵",
            [".flac"] = "🎵",
            [".ogg"] = "🎵",
            [".aac"] = "🎵",
            [".m4a"] = "🎵",
            [".wma"] = "🎵",
            [".opus"] = "🎵",
            [".amr"] = "🎵",

            // Изображения
            [".jpg"] = "🖼️",
            [".jpeg"] = "🖼️",
            [".png"] = "🖼️",
            [".gif"] = "🎞️",
            [".bmp"] = "🖼️",
            [".webp"] = "🖼️",
            [".svg"] = "📐",
            [".ico"] = "🎨",
            [".tiff"] = "🖼️",
            [".raw"] = "🖼️",
            [".heic"] = "🖼️",

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
            [".iso"] = "💿",

            // Код и разработка
            [".cs"] = "💻",
            [".cpp"] = "💻",
            [".c"] = "💻",
            [".h"] = "💻",
            [".py"] = "🐍",
            [".js"] = "💻",
            [".ts"] = "💻",
            [".jsx"] = "⚛️",
            [".tsx"] = "⚛️",
            [".html"] = "🌐",
            [".htm"] = "🌐",
            [".css"] = "🎨",
            [".scss"] = "🎨",
            [".sass"] = "🎨",
            [".less"] = "🎨",
            [".json"] = "📋",
            [".xml"] = "📋",
            [".yaml"] = "📋",
            [".yml"] = "📋",
            [".sql"] = "🗄️",
            [".php"] = "🐘",
            [".rb"] = "💎",
            [".go"] = "🐹",
            [".rs"] = "🦀",
            [".swift"] = "🐦",
            [".kt"] = "🎯",
            [".java"] = "☕",
            [".sh"] = "💻",
            [".bat"] = "💻",
            [".ps1"] = "💻",

            // Дизайн
            [".psd"] = "🎨",
            [".ai"] = "🎨",
            [".eps"] = "🎨",
            [".cdr"] = "🎨",
            [".fig"] = "🎨",
            [".sketch"] = "🎨",

            // Шрифты
            [".ttf"] = "🔤",
            [".otf"] = "🔤",
            [".woff"] = "🔤",
            [".woff2"] = "🔤",
            [".eot"] = "🔤",

            // Системные
            [".exe"] = "⚙️",
            [".msi"] = "⚙️",
            [".dll"] = "🔧",
            [".sys"] = "⚙️",

            // Базы данных
            [".db"] = "🗄️",
            [".sqlite"] = "🗄️",
            [".mdb"] = "🗄️",

            // Прочее
            [".torrent"] = "🔗",
            [".url"] = "🔗",
            [".lnk"] = "🔗"
        };

        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public long Length { get; set; }

        public string FormattedSize
        {
            get
            {
                var size = Length;
                return size switch
                {
                    < 1024 => $"{size} B",
                    < 1024 * 1024 => $"{size / 1024.0:F1} KB",
                    < 1024 * 1024 * 1024 => $"{size / (1024.0 * 1024):F1} MB",
                    _ => $"{size / (1024.0 * 1024 * 1024):F1} GB"
                };
            }
        }

        public string FileIcon
        {
            get
            {
                var ext = Path.GetExtension(FileName).ToLower();
                if (_fileIconMap.TryGetValue(ext, out var icon))
                    return icon;

                if (ContentType.StartsWith("video/")) return "🎬";
                if (ContentType.StartsWith("audio/")) return "🎵";
                if (ContentType.StartsWith("image/")) return "🖼️";

                return "📎";
            }
        }
    }
}