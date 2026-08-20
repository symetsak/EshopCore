namespace Eshop.SystemPanel.Models
{
    public class TenantTransactionDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Type { get; set; } // 1: Χρέωση, 2: Πληρωμή
    }

    public class CreateTransactionDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Type { get; set; } = 1; // Προεπιλογή: Χρέωση
    }
}
