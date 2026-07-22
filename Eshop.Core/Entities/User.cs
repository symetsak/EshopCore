namespace Eshop.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
        public bool IsFirstLogin { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Σχέση: Ένας χρήστης μπορεί να έχει πολλές παραγγελίες
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<UserNote> Notes { get; set; } = new List<UserNote>();
    }
}