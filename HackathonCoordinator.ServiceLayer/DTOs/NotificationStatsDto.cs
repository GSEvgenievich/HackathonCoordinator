namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class NotificationStatsDto
    {
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int TeamNotifications { get; set; }
        public int TaskNotifications { get; set; }
        public int SystemNotifications { get; set; }
    }
}
