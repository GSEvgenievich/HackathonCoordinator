namespace HackathonCoordinator.WebAPI
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ApiResponse<T> Ok(T data, string message = null) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

        public static ApiResponse<T> Fail(string error, List<string> errors = null) => new()
        {
            Success = false,
            Message = error,
            Errors = errors ?? new List<string>()
        };
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ApiResponse Ok(string message = null) => new()
        {
            Success = true,
            Message = message
        };

        public static ApiResponse Fail(string error, List<string> errors = null) => new()
        {
            Success = false,
            Message = error,
            Errors = errors ?? new List<string>()
        };
    }
}