namespace WebApplication1.Service
{
    public class ServiceResponse<T>
    {
        public T? Data { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string Error { get; set; } = string.Empty;
    }
}
