namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public int NotificationTypeId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}
