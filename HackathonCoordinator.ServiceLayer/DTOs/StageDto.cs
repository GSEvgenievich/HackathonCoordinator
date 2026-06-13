using DocumentFormat.OpenXml.InkML;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class StageDto
    {
        public int Id { get; set; }
        public int CompetitionId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public int Order { get; set; }
        public bool IsFinal { get; set; }
        public bool IsStartNotified { get; set; }

        // Вычисляемые свойства
        public bool IsActive => DateTime.Now >= StartTime && DateTime.Now <= EndTime;
        public bool IsUpcoming => DateTime.Now < StartTime;
        public bool IsCompleted => DateTime.Now > EndTime;

        public string StatusText => IsActive ? "Сейчас идет" : (IsUpcoming ? "Предстоит" : "Завершен");
        public string StatusEmoji => IsActive ? "🟢" : (IsUpcoming ? "⏳" : "✅");

        public string TimeRangeText => $"{StartTime:dd.MM.yyyy HH:mm} - {EndTime:dd.MM.yyyy HH:mm}";
        public string DurationText => $"{(EndTime - StartTime).TotalHours:F1} ч";
    }

    public class StageSaveDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public int Order { get; set; }
        public bool IsFinal { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}