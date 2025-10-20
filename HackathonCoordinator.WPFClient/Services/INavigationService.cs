namespace HackathonCoordinator.WPFClient.Services
{
    public interface INavigationService
    {
        event Action CurrentViewModelChanged;
        event Action<bool> MenuVisibilityChanged;

        object CurrentViewModel { get; }
        bool IsMenuVisible { get; set; }

        void NavigateTo<T>() where T : class;
        void NavigateTo(object viewModel);
        void GoBack();
    }
}
