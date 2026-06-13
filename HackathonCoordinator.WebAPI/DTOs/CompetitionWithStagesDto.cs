namespace HackathonCoordinator.WebAPI.DTOs
{
    public class CreateCompetitionWithStagesDto
    {
        public CreateCompetitionDto Competition { get; set; }
        public List<StageSaveDto> Stages { get; set; }
    }

    public class UpdateCompetitionWithStagesDto
    {
        public CreateCompetitionDto Competition { get; set; }
        public List<StageSaveDto> Stages { get; set; }
    }
}
