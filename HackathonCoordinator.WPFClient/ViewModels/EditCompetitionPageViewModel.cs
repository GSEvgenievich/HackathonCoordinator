using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class EditCompetitionViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly CompetitionService _competitionService;

        private string _name = "";
        private string _description = "";
        private DateTime _startDate = DateTime.Today;
        private DateTime _endDate = DateTime.Today.AddDays(1);
        private string _startTime = "10:00";
        private string _endTime = "18:00";
        private bool _isActive = true;
        private string _errorMessage = "";
        private bool _isEditMode = false;
        private int _competitionId = 0;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public string StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public string EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);
        public bool IsEditMode => _isEditMode;

        public string PageTitle => _isEditMode ? "Редактирование соревнования" : "Создание соревнования";
        public string SaveButtonText => _isEditMode ? "Сохранить изменения" : "Создать соревнование";

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public EditCompetitionViewModel()
        {
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();

            SaveCommand = new RelayCommand(async () => await SaveCompetitionAsync());
            CancelCommand = new RelayCommand(() => _navigationService.NavigateTo(new CompetitionsPage()));
        }

        public void LoadCompetitionData(CompetitionDto competition)
        {
            _isEditMode = true;
            _competitionId = competition.Id;

            Name = competition.Name;
            Description = competition.Description;
            StartDate = competition.StartDate;
            EndDate = competition.EndDate;
            StartTime = competition.StartDate.ToString("HH:mm");
            EndTime = competition.EndDate.ToString("HH:mm");
            IsActive = competition.IsActive;

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        private async Task SaveCompetitionAsync()
        {
            if (!ValidateForm())
                return;

            try
            {
                var startDateTime = CombineDateAndTime(StartDate, StartTime);
                var endDateTime = CombineDateAndTime(EndDate, EndTime);

                if (endDateTime <= startDateTime)
                {
                    ErrorMessage = "Дата окончания должна быть позже даты начала";
                    return;
                }

                var dto = new CreateCompetitionDto
                {
                    Name = Name.Trim(),
                    Description = Description?.Trim(),
                    StartDate = startDateTime,
                    EndDate = endDateTime
                };

                if (_isEditMode)
                {
                    var result = await _competitionService.UpdateCompetitionAsync(_competitionId, dto);
                    MessageBox.Show(result.Message);
                    if (result.Success)
                    {
                        _navigationService.NavigateTo(new CompetitionsPage());
                    }
                }
                else
                {
                    var result = await _competitionService.CreateCompetitionAsync(dto);
                    MessageBox.Show(result.Message);
                    if (result.Success)
                    {
                        _navigationService.NavigateTo(new CompetitionsPage());
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
            }
        }

        private bool ValidateForm()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Название соревнования обязательно для заполнения";
                return false;
            }

            if (!IsValidTimeFormat(StartTime))
            {
                ErrorMessage = "Неверный формат времени начала (используйте HH:mm)";
                return false;
            }

            if (!IsValidTimeFormat(EndTime))
            {
                ErrorMessage = "Неверный формат времени окончания (используйте HH:mm)";
                return false;
            }

            return true;
        }

        private DateTime CombineDateAndTime(DateTime date, string time)
        {
            if (TimeSpan.TryParse(time, out var timeSpan))
            {
                return date.Date + timeSpan;
            }
            return date;
        }

        private bool IsValidTimeFormat(string time)
        {
            return TimeSpan.TryParse(time, out _);
        }
    }
}