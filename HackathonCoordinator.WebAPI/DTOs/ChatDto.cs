namespace HackathonCoordinator.WebAPI.DTOs
{
    public class ChatDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int? TeamId { get; set; }
        public int? TaskId { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
        public List<ChatParticipantDto> Participants { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool CanSendMessages { get; set; }
    }
}
