namespace WebApplication1.DTO
{
    public class CreateDisposalRequestDto
    {
        public Guid DisposalPointId { get; set; } // Если в базе Guid, ставим Guid
        public Guid WasteTypeId { get; set; }    // Обязательно для связи с WasteTypes
        public string? WasteType { get; set; }    // Для обратной совместимости с вашим кодом комментария
        public string? Comment { get; set; }
        public IFormFile Photo { get; set; } = null!;
    }
}