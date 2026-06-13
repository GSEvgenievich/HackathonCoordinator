using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class IconDto : INotifyPropertyChanged
    {
        private bool isSelected;
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path => $"/Assets/Images/Profile/{Name}.png";
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
