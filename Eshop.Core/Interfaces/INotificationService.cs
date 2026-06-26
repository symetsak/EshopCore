using System.Collections.Generic;
using System.Threading.Tasks;
using Eshop.Application.DTOs;

namespace Eshop.Application.Services
{
    public interface INotificationService
    {
        // Επιστρέφει όλες τις ειδοποιήσεις του πελάτη
        Task<IEnumerable<NotificationResponseDto>> GetCustomerNotificationsAsync(int customerId);

        // Επιστρέφει το unread count
        Task<UnreadNotificationCountDto> GetUnreadCountAsync(int customerId);

        // Σημαίνει μια ειδοποίηση ως διαβασμένη
        Task<bool> MarkAsReadAsync(int notificationId, int customerId);
    }
}