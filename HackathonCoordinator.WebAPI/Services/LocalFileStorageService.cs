using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace HackathonCoordinator.WebAPI.Services
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadFileAsync(byte[] fileData, string fileName, string contentType, int messageId);
        Task<byte[]> GetFileBytesAsync(string filePath);
        Task DeleteFileAsync(string filePath);
    }

    public class FileUploadResult
    {
        public string FilePath { get; set; }
        public byte[]? Thumbnail { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
    }

    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadsFolder;

        public LocalFileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _uploadsFolder = Path.Combine(environment.ContentRootPath, "Uploads", "Files");
            Directory.CreateDirectory(_uploadsFolder);
        }

        public async Task<FileUploadResult> UploadFileAsync(byte[] fileData, string fileName, string contentType, int messageId)
        {
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{messageId}_{DateTime.Now.Ticks}{extension}";
            var relativePath = $"/uploads/files/{uniqueFileName}";
            var absolutePath = Path.Combine(_uploadsFolder, uniqueFileName);

            // Сохраняем файл
            await File.WriteAllBytesAsync(absolutePath, fileData);

            // Создаем эскиз для изображений
            byte[]? thumbnail = null;
            if (contentType.StartsWith("image/"))
            {
                thumbnail = await CreateThumbnailAsync(fileData);
            }

            return new FileUploadResult
            {
                FilePath = relativePath,
                Thumbnail = thumbnail,
                FileName = fileName,
                FileSize = fileData.Length,
                ContentType = contentType
            };
        }

        private async Task<byte[]> CreateThumbnailAsync(byte[] imageData)
        {
            using var stream = new MemoryStream(imageData);
            using var image = await Image.LoadAsync(stream);

            var targetWidth = 100;
            var ratio = (double)targetWidth / image.Width;
            var targetHeight = (int)(image.Height * ratio);

            image.Mutate(x => x
                .Resize(targetWidth, targetHeight)
                .GaussianBlur(2));

            using var ms = new MemoryStream();
            await image.SaveAsync(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
            return ms.ToArray();
        }

        public async Task<byte[]> GetFileBytesAsync(string filePath)
        {
            var absolutePath = Path.Combine(_environment.ContentRootPath, filePath.TrimStart('/'));
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Файл не найден");

            return await File.ReadAllBytesAsync(absolutePath);
        }

        public Task DeleteFileAsync(string filePath)
        {
            var absolutePath = Path.Combine(_environment.ContentRootPath, filePath.TrimStart('/'));
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);

            return Task.CompletedTask;
        }
    }
}