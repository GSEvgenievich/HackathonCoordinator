namespace HackathonCoordinator.WebAPI.DTOs
{
    public class PositionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsProtected { get; set; }
    }

    public class CreatePositionDto
    {
        public string Name { get; set; }
    }

    public class UpdatePositionDto
    {
        public string Name { get; set; }
    }

    public class ChangeUserPositioDto
    {
        public int PositionId { get; set; }
    }
    
}
