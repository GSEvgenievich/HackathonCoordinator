namespace HackathonCoordinator.WebAPI.DTOs
{
    public class MessageAttachmentDto
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string FilePath { get; set; }
        public string? ThumbnailBase64 { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
