using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class CompetitionDto : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByUsername { get; set; }

        private bool _hasResults;
        public bool HasResults
        {
            get => _hasResults;
            set
            {
                if (_hasResults != value)
                {
                    _hasResults = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ResultsInfo));
                }
            }
        }

        public bool IsArchived { get; set; }
        public List<TeamDto> Teams { get; set; } = new();

        private DateTime? _resultsCreatedAt;
        public DateTime? ResultsCreatedAt
        {
            get => _resultsCreatedAt;
            set
            {
                if (_resultsCreatedAt != value)
                {
                    _resultsCreatedAt = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ResultsInfo));
                }
            }
        }

        public int? ResultsCreatedById { get; set; }

        private string _resultsCreatedByUsername;
        public string ResultsCreatedByUsername
        {
            get => _resultsCreatedByUsername;
            set
            {
                if (_resultsCreatedByUsername != value)
                {
                    _resultsCreatedByUsername = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ResultsInfo));
                }
            }
        }

        private DateTime? _resultsUpdatedAt;
        public DateTime? ResultsUpdatedAt
        {
            get => _resultsUpdatedAt;
            set
            {
                if (_resultsUpdatedAt != value)
                {
                    _resultsUpdatedAt = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ResultsInfo));
                }
            }
        }

        public int? ResultsUpdatedById { get; set; }

        private string _resultsUpdatedByUsername;
        public string ResultsUpdatedByUsername
        {
            get => _resultsUpdatedByUsername;
            set
            {
                if (_resultsUpdatedByUsername != value)
                {
                    _resultsUpdatedByUsername = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ResultsInfo));
                }
            }
        }

        public string ResultsInfo
        {
            get
            {
                if (!HasResults) return "Результаты не подведены";

                var info = "";

                if (ResultsCreatedAt.HasValue)
                {
                    info = $"Результаты подведены: ⏰ {ResultsCreatedAt:dd.MM.yyyy HH:mm}";
                    if (!string.IsNullOrEmpty(ResultsCreatedByUsername))
                    {
                        info += $", 👤 {ResultsCreatedByUsername}";
                    }
                }

                if (ResultsUpdatedAt.HasValue)
                {
                    if (!string.IsNullOrEmpty(info)) info += "\n";
                    info += $"Результаты обновлены: ⏰ {ResultsUpdatedAt:dd.MM.yyyy HH:mm}";
                    if (!string.IsNullOrEmpty(ResultsUpdatedByUsername))
                    {
                        info += $", 👤 {ResultsUpdatedByUsername}";
                    }
                }

                return string.IsNullOrEmpty(info) ? "Результаты подведены" : info;
            }
        }

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

        public string StatusColor => StatusText switch
        {
            "Активно" => "Green",
            "Ожидается" => "Orange",
            "Завершено" => "Gray",
            _ => "Gray"
        };

        public string StatusIcon => StatusText switch
        {
            "Активно" => "🏃",
            "Ожидается" => "⏰",
            "Завершено" => "✅",
            _ => "❓"
        };

        public string TeamsCountText => Teams?.Count switch
        {
            0 => "Нет команд",
            1 => "1 команда",
            _ => $"{Teams.Count} команд"
        };

        public bool IsCompleted => EndDate < DateTime.Now;
        public bool IsActive => StartDate <= DateTime.Now && EndDate >= DateTime.Now;
        public bool IsUpcoming => StartDate > DateTime.Now;
        public string DateRangeText => $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
        public string DateTimeRangeText => $"{StartDate:dd.MM.yyyy HH:mm} - {EndDate:dd.MM.yyyy HH:mm}";
        public bool HasNoTeams => Teams?.Count == 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}