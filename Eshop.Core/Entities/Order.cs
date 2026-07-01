namespace Eshop.Core.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        // Workflow States: Pending, Paid, Shipped, Completed, Cancelled
        public string Status { get; set; } = "Pending";

        // Foreign Key για τον Πελάτη 
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Σχέση: Μια παραγγελία έχει πολλά προϊόντα (μέσω του OrderItem)
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}