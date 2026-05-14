namespace HackathonCoordinator.WebAPI.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserIcon { get; set; }
        public string Text { get; set; }
        public DateTime SentAt { get; set; }
        public bool HasAttachments { get; set; }
        public List<MessageAttachmentDto> Attachments { get; set; } = new();
        public bool IsEdited { get; set; }
        public bool IsMyMessage { get; set; }
    }
}
