namespace WebApplication1.DTO
{
    public class CreateDisposalRequestDto
    {
        public Guid DisposalPointId { get; set; }
        public string WasteType { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public IFormFile Photo { get; set; } = null!;
    }
}
