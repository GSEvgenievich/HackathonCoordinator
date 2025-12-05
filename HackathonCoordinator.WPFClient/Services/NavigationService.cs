using HackathonCoordinator.WPFClient.Views;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Services
{
    public class NavigationService
    {
        private Frame _currentFrame;
        private Border _sideBar;

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

        public void NavigateTo(Page page)
        {
            if (_currentFrame != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
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