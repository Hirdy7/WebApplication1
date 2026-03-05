

namespace WebApplication1.Models
{
    public class WasteType
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public int Rewards { get; set; }

        public ICollection<DisposalPointWasteType> DisposalPointWasteTypes { get; set; }
           = new List<DisposalPointWasteType>();
    }
}

