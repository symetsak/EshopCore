using Eshop.Core.Interfaces;
using Eshop.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Eshop.API.Services
{
    public class EshopNotificationService : IEshopNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotificationRepository _notificationRepo;

        public EshopNotificationService(IHubContext<NotificationHub> hubContext, INotificationRepository notificationRepo)
        {
            _hubContext = hubContext;
            _notificationRepo = notificationRepo;
        }

        // Στέλνει το μήνυμα ΜΟΝΟ στο group των Admins του συγκεκριμένου Tenant
        public async Task SendToAdminsAsync(string tenantId, string title, string message, object? data = null)
        {
            var adminGroupName = $"Group_{tenantId.ToLower().Trim()}_Admins";

            // Ψάχνουμε πόσα unread alerts έχει ο Admin (CustomerId == null)
            var adminNotifications = await _notificationRepo.GetAdminNotificationsAsync();
            int unreadCount = adminNotifications.Count(n => !n.IsRead);

            await _hubContext.Clients.Group(adminGroupName).SendAsync("ReceiveNotification", new
            {
                title = title,
                message = message,
                timestamp = DateTime.UtcNow,
                unreadCount = unreadCount,
                payload = data
            });
        }

        // Στέλνει το μήνυμα ΑΥΣΤΗΡΑ στο προσωπικό group του συγκεκριμένου Customer
        public async Task SendToCustomerAsync(string tenantId, int customerId, string title, string message, object? data = null)
        {
            var customerGroupName = $"Group_{tenantId.ToLower().Trim()}_Customer_{customerId}";

            int unreadCount = await _notificationRepo.GetUnreadCountAsync(customerId);

            await _hubContext.Clients.Group(customerGroupName).SendAsync("ReceiveNotification", new
            {
                title = title,
                message = message,
                timestamp = DateTime.UtcNow,
                unreadCount = unreadCount,
                payload = data
            });
        }

        // Αυτή η μέθοδος θα καλείται όταν ο χρήστης διαβάζει μια ειδοποίηση (στο Controller)
        public async Task SyncCustomerUnreadCountAsync(string tenantId, int customerId)
        {
            var customerGroupName = $"Group_{tenantId.ToLower().Trim()}_Customer_{customerId}";
            int unreadCount = await _notificationRepo.GetUnreadCountAsync(customerId);

            // Στέλνουμε ένα ειδικό event "UpdateUnreadCount" σε όλα τα tabs του χρήστη
            await _hubContext.Clients.Group(customerGroupName).SendAsync("UpdateUnreadCount", new{unreadCount = unreadCount});
        }
    }
}