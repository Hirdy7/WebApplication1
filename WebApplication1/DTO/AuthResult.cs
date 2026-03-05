namespace WebApplication1.DTO
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public AuthResponse? Data { get; set; }
        public string? Error { get; set; }

        public static AuthResult Success(AuthResponse data) => new() { IsSuccess = true, Data = data };
        public static AuthResult Fail(string error) => new() { IsSuccess = false, Error = error };
    }
}
