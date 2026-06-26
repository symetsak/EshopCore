using System;

namespace Eshop.Core.Entities
{
    public class Notification
    {
        // Το ID της ειδοποίησης
        public int Id { get; set; }

        // Σε ποιον πελάτη απευθύνεται
        public int CustomerId { get; set; }

        // Ο τίτλος και το κυρίως μήνυμα της ειδοποίησης
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;

        // Ο τύπος (π.χ. Order, Info, Promo) για φιλτράρισμα ή εικονίδια στο frontend
        public string Type { get; set; } = "Info";

        // Flag για το αν διαβάστηκε
        public bool IsRead { get; set; } = false;

        // Πότε δημιουργήθηκε
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}