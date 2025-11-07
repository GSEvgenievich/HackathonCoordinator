using HackathonCoordinator.WPFClient.Views;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Services
{
    public class NavigationService
    {
        private Frame _currentFrame;
        private Border _sideBar;
        private Stack<Page> _navigationStack = new Stack<Page>();

        public void Initialize(Frame frame, Border sideBar)
        {
            _currentFrame = frame;
            _sideBar = sideBar;
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
        public void GoBack()
        {
            if (_navigationStack.Count > 0)
            {
                var previousPage = _navigationStack.Pop();
                NavigateTo(previousPage);
            }
            else if (_currentFrame?.CanGoBack == true)
            {
                _currentFrame.GoBack();
            }
        }

        public void NavigateTo(Page page)
        {
            if (_currentFrame != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_currentFrame.Content is Page currentPage)
                    {
                        _navigationStack.Push(currentPage);
                    }

                    _currentFrame.Navigate(page);

                    if (page is AuthorizationPage || page is RegistrationPage)
                    {
                        SideBarHide();
                    }
                    else
                    {
                        SideBarOpen();
                    }
                });
            }
        }
    }
}