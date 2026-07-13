using System.ComponentModel.DataAnnotations;

namespace Eshop.AdminPanel.Client.Models
{
    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Το όνομα του καταστήματος είναι υποχρεωτικό.")]
        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Το όνομα χρήστη είναι υποχρεωτικό.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο τωρινός κωδικός είναι υποχρεωτικός.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο νέος κωδικός είναι υποχρεωτικός.")]
        [MinLength(6, ErrorMessage = "Ο νέος κωδικός πρέπει να είναι τουλάχιστον 6 χαρακτήρες.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Η επιβεβαίωση κωδικού είναι υποχρεωτική.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Οι κωδικοί δεν ταιριάζουν.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}