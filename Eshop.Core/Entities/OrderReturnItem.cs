namespace Eshop.Core.Entities
{
    public class OrderReturnItem
    {
        public int Id { get; set; }
        public int OrderReturnId { get; set; }
        public OrderReturn OrderReturn { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; } // Πόσα κομμάτια επιστρέφει από αυτό το προϊόν
        public decimal UnitPrice { get; set; } // Η τιμή που το είχε αγοράσει
    }
}