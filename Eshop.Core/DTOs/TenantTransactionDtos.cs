namespace Eshop.Core.DTOs
{
    public class TenantTransactionDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Type { get; set; } // 1: Charge (Χρέωση), 2: Payment (Πληρωμή)
    }

    public class CreateTransactionDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Type { get; set; }
    }
}