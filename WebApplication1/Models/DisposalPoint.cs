

namespace WebApplication1.Models
{
    public class DisposalPoint
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; } = true;

       
        public ICollection<DisposalPointWasteType> DisposalPointWasteTypes { get; set; }
            = new List<DisposalPointWasteType>();
    }
}
