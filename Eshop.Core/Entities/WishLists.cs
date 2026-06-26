using System;

namespace Eshop.Core.Entities
{
    public class Wishlist
    {
        // 1. Το Primary Key της εγγραφής
        public int Id { get; set; }

        // 2. Ο Πελάτης στον οποίο ανήκει αυτό το αγαπημένο προϊόν
        public int CustomerId { get; set; }

        // 3. Το Προϊόν που έγινε match
        public int ProductId { get; set; }

        // Navigation Property για να μπορούμε να τραβάμε τις πληροφορίες του προϊόντος (Όνομα, Τιμή, Εικόνα)
        public Product Product { get; set; } = null!;

        // 4. Ημερομηνία προσθήκης (χρήσιμο για να τα δείχνουμε ταξινομημένα από τα πιο πρόσφατα)
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}