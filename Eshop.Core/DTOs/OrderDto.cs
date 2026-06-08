namespace Eshop.Core.DTOs
{
    // Τι στέλνει το Frontend για να κάνει παραγγελία (το "Καλάθι")
    public class OrderCreateDto
    {
        public List<OrderItemCreateDto> OrderItems { get; set; } = new List<OrderItemCreateDto>();
    }

    // Το κάθε προϊόν μέσα στο καλάθι
    public class OrderItemCreateDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // Τι επιστρέφει το API όταν η παραγγελία ολοκληρωθεί με επιτυχία
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public List<OrderItemResponseDto> OrderItems { get; set; } = new List<OrderItemResponseDto>();
    }

    // Οι λεπτομέρειες των προϊόντων στην απάντηση
    public class OrderItemResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
    }
}