using System;
using System.Collections.Generic;

namespace Eshop.Core.Entities
{
    public class Cart
    {
        public int Id { get; set; }

        // Σχέση με τον Customer (1 Καλάθι ανά Customer)
        public int CustomerId { get; set; }

        // Η λίστα με τα προϊόντα που έχει μέσα το καλάθι
        public List<CartItem> CartItems { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}