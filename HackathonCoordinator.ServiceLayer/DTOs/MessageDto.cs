using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class MessageDto : INotifyPropertyChanged
    {
        private string _text;
        private bool _isEdited;

        public int Id { get; set; }
        public int ChatId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserIcon { get; set; }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime SentAt { get; set; }
        public string SentAtFormatted => SentAt.ToString("HH:mm");

        public bool IsMyMessage { get; set; }
        public bool HasAttachments { get; set; }
        public List<MessageAttachmentDto> Attachments { get; set; } = new();

        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                if (_isEdited != value)
                {
                    _isEdited = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}