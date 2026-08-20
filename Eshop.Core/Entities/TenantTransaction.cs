namespace Eshop.Core.Entities
{
    public class TenantTransaction
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public Tenant? Tenant { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;

        // Το ποσό της συναλλαγής
        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }
    }

    public enum TransactionType
    {
        Charge = 1,  // Χρέωση (π.χ. Συνδρομή, Αγορά extra πακέτου)
        Payment = 2  // Πληρωμή (Εξόφληση από τον πελάτη)
    }
}