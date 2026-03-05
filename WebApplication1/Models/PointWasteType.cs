namespace WebApplication1.Models
{
    public class PointWasteType
    {
        public Guid Id { get; set; }

        public Guid PointId { get; set; }
        public DisposalPoint Point { get; set; } = null!;

        public Guid WasteTypeId { get; set; }
        public WasteType WasteType { get; set; } = null!;

    }
}
