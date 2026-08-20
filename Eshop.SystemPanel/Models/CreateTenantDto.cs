namespace Eshop.SystemPanel.Models
{
    public class CreateTenantDto
    {
        // Τα απολύτως υποχρεωτικά για τη δημιουργία της βάσης
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;

        // Προεπιλογή να είναι ενεργός ο νέος πελάτης
        public bool IsActive { get; set; } = true;

        // Προαιρετικά στοιχεία
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
    }
}