namespace WebApplication1.DTO
{
    public class NicknameChangeResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? NewNickname { get; set; }
    }
}
