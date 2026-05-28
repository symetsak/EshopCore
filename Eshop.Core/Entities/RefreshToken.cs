using System;

namespace Eshop.Core.Entities // Προσάρμοσε το namespace με τα δικά σου
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }

        // Navigation Property για να ξέρουμε σε ποιον χρήστη ανήκει
        public User User { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Έλεγχος αν το token έχει λήξει βάσει ημερομηνίας
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}