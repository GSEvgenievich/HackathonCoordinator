using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class SendMessageWithAttachmentsDto
    {
        public int ChatId { get; set; }
        public string Text { get; set; }
        public List<byte[]> Attachments { get; set; } = new();
        public List<string> FileNames { get; set; } = new();
        public List<string> ContentTypes { get; set; } = new();
    }
}
