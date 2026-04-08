
    namespace WebApplication1.Models
{
    public class DisposalPointWasteType
    {
        public Guid DisposalPointId { get; set; }
        public DisposalPoint? DisposalPoint { get; set; }

        public Guid WasteTypeId { get; set; } 
        public WasteType? WasteType { get; set; }
    }
}


