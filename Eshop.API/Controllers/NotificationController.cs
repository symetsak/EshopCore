using AutoMapper; 
using Eshop.Application.DTOs;
using Eshop.Application.Services;
using Eshop.Core.DTOs; 
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationRepository _notificationRepo;
        private readonly IEshopNotificationService _eshopNotificationService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IMapper _mapper; 

        // Inject το Service, το Repo, τα Notification Services, τον TenantProvider ΚΑΙ τον AutoMapper
        public NotificationsController(
            INotificationService notificationService,
            INotificationRepository notificationRepo,
            IEshopNotificationService eshopNotificationService,
            ITenantProvider tenantProvider,
            IMapper mapper) 
        {
            _notificationService = notificationService;
            _notificationRepo = notificationRepo;
            _eshopNotificationService = eshopNotificationService;
            _tenantProvider = tenantProvider;
            _mapper = mapper; 
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
        [Authorize(Roles = "Administrator, Employee")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            // 1. Τραβάμε τις οντότητες από τη βάση
            var notifications = await _notificationRepo.GetAdminNotificationsAsync();

            // 2. Τις μετατρέπουμε με ασφάλεια σε DTOs μέσω του AutoMapper!
            var resultDtos = _mapper.Map<IEnumerable<NotificationResponseDto>>(notifications);

            // 3. Στέλνουμε το καθαρό DTO στο Blazor
            return Ok(resultDtos);
        }

        [HttpPut("admin/{id}/read")]
        [Authorize(Roles = "Administrator, Employee")]
        public async Task<IActionResult> MarkAdminAsRead(int id)
        {
            var notification = await _notificationRepo.GetByIdAsync(id);
            if (notification == null)
            {
                return NotFound(new { message = "Η ειδοποίηση δεν βρέθηκε." });
            }

            notification.IsRead = true;
            await _notificationRepo.SaveChangesAsync();

            return Ok(new { message = "Η ειδοποίηση σημάνθηκε ως διαβασμένη από τον Admin." });
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
                    // Μόλις αλλάξει στη βάση, σπρώχνουμε live το νέο Count σε όλα τα ανοιχτά tabs του χρήστη!
                    await _eshopNotificationService.SyncCustomerUnreadCountAsync(_tenantProvider.TenantId!, customerId);

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