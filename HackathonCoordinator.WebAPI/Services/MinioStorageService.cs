using Minio;
using Minio.DataModel.Args;
using SixLabors.ImageSharp.Processing;

namespace HackathonCoordinator.WebAPI.Services;

public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, long size, int messageId);
    Task<Stream> DownloadAsync(string objectName);
    Task DeleteAsync(string objectName);
    Task<string> GetPresignedUrlAsync(string objectName, int expirySeconds = 3600);
    Task<byte[]?> GetThumbnailAsync(string objectName);
}

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient;
        _configuration = configuration;
        _logger = logger;
        _bucketName = configuration["Minio:BucketName"] ?? "hackathon-files";

        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                _logger.LogInformation("✅ Бакет '{BucketName}' создан", _bucketName);

                // Устанавливаем политику публичного чтения
                await SetPublicReadPolicyAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании бакета");
        }
    }

    private async Task SetPublicReadPolicyAsync()
    {
        try
        {
            var policy = $@"
{{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {{
            ""Effect"": ""Allow"",
            ""Principal"": {{ ""AWS"": [""*""] }},
            ""Action"": [""s3:GetObject""],
            ""Resource"": [""arn:aws:s3:::{_bucketName}/*""]
        }}
    ]
}}";
            await _minioClient.SetPolicyAsync(new SetPolicyArgs()
                .WithBucket(_bucketName)
                .WithPolicy(policy));

            _logger.LogInformation("✅ Политика публичного доступа установлена для бакета '{BucketName}'", _bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось установить публичную политику");
        }
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, long size, int messageId)
    {
        var extension = Path.GetExtension(fileName);
        var objectName = $"{messageId}/{DateTime.Now.Ticks}{extension}";

        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(size)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putArgs);
        _logger.LogInformation("Файл загружен: {ObjectName}", objectName);

        return objectName;
    }

    public async Task<Stream> DownloadAsync(string objectName)
    {
        var memoryStream = new MemoryStream();

        var getArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithCallbackStream(async (stream, cancellationToken) =>
            {
                await stream.CopyToAsync(memoryStream, cancellationToken);
            });

        await _minioClient.GetObjectAsync(getArgs);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<string> GetPresignedUrlAsync(string objectName, int expirySeconds = 3600)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithExpiry(expirySeconds);

        var url = await _minioClient.PresignedGetObjectAsync(args);

        // Заменяем внутренний хост на публичный
        var publicEndpoint = _configuration["Minio:PublicEndpoint"] ?? "localhost:9000";
        var useSsl = _configuration.GetValue<bool>("Minio:UseSsl", false);
        var scheme = useSsl ? "https" : "http";

        var publicUrl = url.Replace($"http://minio:9000", $"{scheme}://{publicEndpoint}");

        return publicUrl;
    }

    public async Task<byte[]?> GetThumbnailAsync(string objectName)
    {
        try
        {
            var stream = await DownloadAsync(objectName);

            using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);

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
        catch
        {
            return null;
        }
    }

    public async Task DeleteAsync(string objectName)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName);

        await _minioClient.RemoveObjectAsync(args);
        _logger.LogInformation("Файл удален: {ObjectName}", objectName);
    }
}