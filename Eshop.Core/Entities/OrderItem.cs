namespace Eshop.Core.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Foreign Key για την Παραγγελία
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        // Foreign Key για το Προϊόν
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}