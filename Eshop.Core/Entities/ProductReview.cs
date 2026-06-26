using System;

namespace Eshop.Core.Entities
{
    public class ProductReview
    {
        // 1. Το μοναδικό ID της κριτικής (Primary Key)
        public int Id { get; set; }

        // 2. Σύνδεση με το Προϊόν (Foreign Key)
        public int ProductId { get; set; }

        // Navigation Property: Επιτρέπει στο EF να καταλάβει τη σχέση με τον πίνακα Products
        public Product Product { get; set; } = null!;

        // 3. Σύνδεση με τον πελάτη που κάνει την κριτική
        // Το ID του πελάτη έρχεται ως string από το Identity (JWT Token)
        public int CustomerId { get; set; }

        // 4. Η βαθμολογία (1 έως 5 αστέρια)
        public int Rating { get; set; }

        // 5. Προαιρετικός Τίτλος (π.χ. "Πολύ καλό!")
        public string? Title { get; set; }

        // 6. Προαιρετικό και το κυρίως σχόλιο της κριτικής
        public string? Comment { get; set; }

        // 7. Flag έγκρισης 
        public bool IsApproved { get; set; } = true;

        // 8. Ημερομηνία δημιουργίας
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}