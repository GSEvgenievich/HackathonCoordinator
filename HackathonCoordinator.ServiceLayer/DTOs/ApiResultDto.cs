namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class ApiResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        public static ApiResultDto Success() => new ApiResultDto { IsSuccess = true, Message = "Успешно" };
        public static ApiResultDto Success(string message) => new ApiResultDto { IsSuccess = true, Message = message };
        public static ApiResultDto Failure(string message) => new ApiResultDto { IsSuccess = false, Message = message };
    }
}
