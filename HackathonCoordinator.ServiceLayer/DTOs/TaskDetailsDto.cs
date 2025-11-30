namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class TaskDetailsDto : TaskDto
    {
        public bool CanEdit { get; set; }
        public bool CanAssign { get; set; }
        public bool CanComplete { get; set; } // Для исполнителя - запросить завершение
        public bool CanCancel { get; set; }   // Для исполнителя - запросить отмену
        public bool CanConfirmCompletion { get; set; } // Для капитана - подтвердить завершение
        public bool CanRejectCompletion { get; set; }  // Для капитана - отклонить завершение
        public bool CanCancelTaskAsCaptain { get; set; } // Для капитана - отменить задачу
        public bool HasChat { get; set; }
        public int? TaskChatId { get; set; }
    }
}
