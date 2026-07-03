using Eshop.Application.Services;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize] // Ασφάλεια: Μόνο για συνδεδεμένους χρήστες!
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationRepository _notificationRepo;

        // Inject το Service των ειδοποιήσεων
        public NotificationsController(INotificationService notificationService, INotificationRepository notificationRepo)
        {
            _notificationService = notificationService;
            _notificationRepo = notificationRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;

            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            var result = await _notificationService.GetCustomerNotificationsAsync(customerId);
            return Ok(result);
        }

        [HttpGet("adminNotifications")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var notifications = await _notificationRepo.GetAdminNotificationsAsync();
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;

            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            var result = await _notificationService.GetUnreadCountAsync(customerId);
            return Ok(result);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;

            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            try
            {
                var success = await _notificationService.MarkAsReadAsync(id, customerId);

                if (success)
                {
                    return Ok(new { message = "Η ειδοποίηση σημάνθηκε ως διαβασμένη." });
                }

                return BadRequest("Δεν ήταν δυνατή η ενημέρωση της ειδοποίησης.");
            }
            catch (InvalidOperationException ex)
            {
                // Εδώ πιάνουμε το exception αν κάποιος πάει να διαβάσει ξένη ειδοποίηση!
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}