using System.ComponentModel.DataAnnotations;

namespace Eshop.AdminPanel.Client.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Το όνομα χρήστη (Username) είναι υποχρεωτικό.")]
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο κωδικός πρόσβασης είναι υποχρεωτικός.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Το όνομα του καταστήματος (Tenant) είναι υποχρεωτικό.")]
        public string TenantId { get; set; } = string.Empty; // π.χ. adidas-store
    }
}