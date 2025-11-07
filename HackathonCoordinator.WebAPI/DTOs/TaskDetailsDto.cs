namespace HackathonCoordinator.WebAPI.DTOs
{
    public class TaskDetailsDto : TaskDto
    {
        public bool CanEdit { get; set; }
        public bool CanAssign { get; set; }
        public bool CanComplete { get; set; }
        public bool CanCancel { get; set; }
        public bool HasChat { get; set; }
        public int? TaskChatId { get; set; }
    }
}
