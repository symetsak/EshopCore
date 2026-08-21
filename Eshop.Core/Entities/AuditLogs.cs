namespace Eshop.Core.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        // Ποιο μαγαζί αφορά η αλλαγή
        public string TenantId { get; set; } = string.Empty;

        // Ποιος χρήστης έκανε την αλλαγή
        public string Username { get; set; } = string.Empty;

        // Ποιον πίνακα πείραξε (π.χ. "Products", "Orders")
        public string TableName { get; set; } = string.Empty;

        // Τι ακριβώς έκανε ("INSERT", "UPDATE", "DELETE")
        public string Action { get; set; } = string.Empty;

        // Παλιές και νέες τιμές σε μορφή JSON για να βλέπουμε ακριβώς την αλλαγή
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        // Πότε έγινε
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}