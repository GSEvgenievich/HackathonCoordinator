using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class CompetitionsViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly CompetitionService _competitionService;
        private readonly UserService _userService;

        private ObservableCollection<CompetitionDto> _competitions = new();
        public ObservableCollection<CompetitionDto> Competitions
        {
            get => _competitions;
            set => SetProperty(ref _competitions, value);
        }

        public bool IsOrganizer { get; private set; }

        public RelayCommand AddCompetitionCommand { get; }
        public RelayCommand<CompetitionDto> SelectCompetitionCommand { get; }
        public RelayCommand<CompetitionDto> EditCompetitionCommand { get; }

        public CompetitionsViewModel()
        {
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _userService = new UserService();

            AddCompetitionCommand = new RelayCommand(() => AddCompetition());
            SelectCompetitionCommand = new RelayCommand<CompetitionDto>(SelectCompetition);
            EditCompetitionCommand = new RelayCommand<CompetitionDto>(EditCompetition);

            LoadCompetitionsAsync();
            CheckUserRole();
        }

        private async void LoadCompetitionsAsync()
        {
            var competitions = await _competitionService.GetCompetitionsAsync();

            if (IsOrganizer)
            {
                // Организатор видит все соревнования
                Competitions = new ObservableCollection<CompetitionDto>(competitions);
            }
            else
            {
                // Обычный пользователь видит только активные
                var activeCompetitions = competitions.Where(c => c.IsActive && c.EndDate >= DateTime.Now).ToList();
                Competitions = new ObservableCollection<CompetitionDto>(activeCompetitions);
            }
        }

        private async void CheckUserRole()
        {
            var user = await _userService.GetCurrentUserAsync();
            IsOrganizer = user?.RoleId == 3; // 3 = Organizer
            OnPropertyChanged(nameof(IsOrganizer));
        }

        private void AddCompetition()
        {
            _navigationService.NavigateTo(new EditCompetitionPage(null));
        }

        private void SelectCompetition(CompetitionDto competition)
        {
            if (competition != null)
            {
                _navigationService.NavigateTo(new CompetitionDetailsPage(competition));
            }
        }

        private void EditCompetition(CompetitionDto competition)
        {
            if (competition != null)
            {
                _navigationService.NavigateTo(new EditCompetitionPage(competition));
            }
        }
    }
}