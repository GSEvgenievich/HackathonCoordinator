namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class CompetitionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByUsername { get; set; }
        public List<TeamDto> Teams { get; set; } = new();

        public string StatusText
        {
            get
            {
                var now = DateTime.Now;
                if (now < StartDate) return "Ожидается";
                if (now > EndDate) return "Завершено";
                return "Активно";
            }
        }

        public string StatusColor
        {
            get
            {
                return StatusText switch
                {
                    "Активно" => "Green",
                    "Ожидается" => "Orange",
                    "Завершено" => "Gray",
                    _ => "Gray"
                };
            }
        }

        public string StatusIcon
        {
            get
            {
                return StatusText switch
                {
                    "Активно" => "🏃",
                    "Ожидается" => "⏰",
                    "Завершено" => "✅",
                    _ => "❓"
                };
            }
        }

        public string TeamsCountText => Teams?.Count switch
        {
            0 => "Нет команд",
            1 => "1 команда",
            _ => $"{Teams.Count} команд"
        };

        public bool IsActive => StartDate <= DateTime.Now && EndDate >= DateTime.Now;
        public string DateRangeText => $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
        public string DateTimeRangeText => $"{StartDate:dd.MM.yyyy HH:mm} - {EndDate:dd.MM.yyyy HH:mm}";
        public bool HasNoTeams => Teams?.Count == 0;
    }
}
