namespace Eshop.Core.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";

        // Foreign Key για τον Χρήστη
        public int UserId { get; set; }
        public User? User { get; set; }

        // Σχέση: Μια παραγγελία έχει πολλά προϊόντα (μέσω του OrderItem)
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}