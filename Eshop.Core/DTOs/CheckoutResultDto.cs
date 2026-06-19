namespace Eshop.Core.DTOs
{
    public class CheckoutResultDto
    {
        public int OrderId { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}