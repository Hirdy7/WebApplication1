using WebApplication1.Models;

namespace WebApplication1.DTO
{
    public class UpdateStatusDto
    {
        public DisposalRequestStatus NewStatus { get; set; }
        public int Points { get; set; }
        public string Response { get; set; }
    }
}
