using System;

namespace Eshop.Application.DTOs
{
    public class WishlistResponseDto
    {
        // 1. Το ID της εγγραφής στη Wishlist (χρήσιμο για αν θέλουμε να κάνουμε delete με βάση αυτό το ID)
        public int Id { get; set; }

        // 2. Το ID του προϊόντος
        public int ProductId { get; set; }

        // 3. Πληροφορίες του προϊόντος που χρειάζεται το frontend για να τις δείξει στην οθόνη
        public string ProductName { get; set; } = null!;
        public decimal ProductPrice { get; set; }
        public string? ProductImageUrl { get; set; }

        // 4. Πότε προστέθηκε
        public DateTime AddedAt { get; set; }
    }
}