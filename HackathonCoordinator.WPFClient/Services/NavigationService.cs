using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Services
{
    public class NavigationService
    {
        private Frame _currentFrame;
        private Border _sideBar;
        private Page _currentPage;

        // Простой стек только для назад
        private Stack<Page> _backStack = new Stack<Page>();

        public void Initialize(Frame frame, Border sideBar)
        {
            _currentFrame = frame;
            _sideBar = sideBar;

            if (_currentFrame != null)
            {
                _currentFrame.Navigated += OnFrameNavigated;
                // Полностью отключаем встроенную навигацию Frame
                _currentFrame.JournalOwnership = System.Windows.Navigation.JournalOwnership.OwnsJournal;
                _currentFrame.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;

                // Отключаем навигацию по кнопкам мыши через событие
                _currentFrame.Navigating += OnFrameNavigating;
            }
        }

        // Отключаем встроенную навигацию
        private void OnFrameNavigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back ||
                e.NavigationMode == System.Windows.Navigation.NavigationMode.Forward)
            {
                e.Cancel = true;
            }
        }

        private void OnFrameNavigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            _currentPage = e.Content as Page;

            if (_currentPage != null)
            {
                if (_currentPage is AuthorizationPage || _currentPage is RegistrationPage)
                {
                    SideBarHide();
                }
                else
                {
                    SideBarOpen();
                }
            }
        }

        public void SideBarHide()
        {
            if (_sideBar != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _sideBar.Visibility = Visibility.Collapsed;
                });
            }
        }

        public void SideBarOpen()
        {
            if (_sideBar != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _sideBar.Visibility = Visibility.Visible;
                });
            }
        }

        /// <summary>
        /// Навигация на новую страницу
        /// </summary>
        public void NavigateTo(Page page)
        {
            if (_currentFrame != null && _currentPage != page)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Сохраняем текущую страницу в стек назад
                    if (_currentPage != null)
                    {
                        _backStack.Push(_currentPage);
                    }

                    _currentFrame.Navigate(page);
                });
            }
        }

        /// <summary>
        /// Возврат на предыдущую страницу
        /// </summary>
        public bool GoBack()
        {
            if (_backStack.Count > 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var previousPage = _backStack.Pop();
                    _currentFrame.Navigate(previousPage);
                });
                return true;
            }
            return false;
        }

        /// <summary>
        /// Проверка, можно ли вернуться назад
        /// </summary>
        public bool CanGoBack => _backStack.Count > 0;

        /// <summary>
        /// Возврат на главную страницу (очищает стек)
        /// </summary>
        public async void GoToMainPage()
        {
            if (_currentFrame != null)
            {
                try
                {
                    var teamService = new TeamService();
                    var teamId = await teamService.GetCurrentTeamIdAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Page mainPage;
                        if (!teamId.Success || teamId.Data == 0)
                        {
                            mainPage = new CompetitionsPage();
                        }
                        else
                        {
                            mainPage = new TeamPage();
                        }

                        // Очищаем стек навигации
                        _backStack.Clear();

                        _currentFrame.Navigate(mainPage);
                    });
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _backStack.Clear();
                        _currentFrame.Navigate(new CompetitionsPage());
                    });
                }
            }
        }

        /// <summary>
        /// Очистка истории навигации
        /// </summary>
        public void ClearHistory()
        {
            _backStack.Clear();
        }

        /// <summary>
        /// Текущая страница
        /// </summary>
        public Page CurrentPage => _currentPage;
    }
}