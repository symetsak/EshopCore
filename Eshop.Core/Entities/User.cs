namespace Eshop.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // Σχέση: Ένας χρήστης μπορεί να έχει πολλές παραγγελίες
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}