namespace Eshop.Core.DTOs
{
    // Τι στέλνει το Frontend για να κάνει παραγγελία (το "Καλάθι")
    public class OrderCreateDto
    {
        public List<OrderItemCreateDto> OrderItems { get; set; } = new List<OrderItemCreateDto>();

        public decimal? OverrideTotalAmount { get; set; }

        // "CashOnDelivery" ή "Card"
        public string PaymentMethod { get; set; } = "CashOnDelivery";

        public string Street { get; set; } = string.Empty;
        public string StreetNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
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
        public string CustomerName { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string StreetNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public List<OrderItemResponseDto> OrderItems { get; set; } = new List<OrderItemResponseDto>();
        public string PaymentMethod { get; set; } = null!;
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