namespace HackathonCoordinator.WebAPI.DTOs
{
    public class SendMessageWithAttachmentsDto
    {
        public int ChatId { get; set; }
        public string Text { get; set; }
        public List<IFormFile> Attachments { get; set; } = new();
    }
}
