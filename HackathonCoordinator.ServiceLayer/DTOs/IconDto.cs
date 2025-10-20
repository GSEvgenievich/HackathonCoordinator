namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class IconDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path => $"/Assets/Images/Profile/{Name}.png";
        public bool IsSelected { get; set; }
    }
}
