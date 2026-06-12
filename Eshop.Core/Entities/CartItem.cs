using System;

namespace Eshop.Core.Entities
{
    public class CartItem
    {
        public int Id { get; set; }

        // Σχέση με το Καλάθι
        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        // Σχέση με το Προϊόν
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Ποσότητα
        public int Quantity { get; set; }

        // Ημερομηνίες για analytics
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}