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
        public string SentAtFormatted => SentAt.ToString("HH:mm");
        public string SentAtFull => SentAt.ToString("dd.MM.yyyy HH:mm");
        public bool IsEdited { get; set; }
        public bool IsMyMessage { get; set; }
    }
}
