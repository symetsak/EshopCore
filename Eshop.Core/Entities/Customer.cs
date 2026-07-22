using System.ComponentModel.DataAnnotations;

namespace Eshop.Core.Entities
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Street { get; set; } = string.Empty;

        [MaxLength(20)]
        public string StreetNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiry { get; set; }

        // Σχέση 1-προς-Πολλά: Ένας πελάτης μπορεί να έχει πολλές παραγγελίες
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}