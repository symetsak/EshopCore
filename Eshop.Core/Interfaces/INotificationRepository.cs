using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface INotificationRepository
    {
        // Φέρνει όλες τις ειδοποιήσεις ενός πελάτη
        Task<IEnumerable<Notification>> GetByCustomerIdAsync(int customerId);

        // Φέρνει μια συγκεκριμένη ειδοποίηση με βάση το Id της
        Task<Notification?> GetByIdAsync(int id);

        // Επιστρέφει το πλήθος των unread ειδοποιήσεων
        Task<int> GetUnreadCountAsync(int customerId);

        // Προσθέτει μια νέα ειδοποίηση (π.χ. όταν γίνεται μια παραγγελία)
        Task AddAsync(Notification notification);

        // SaveChanges
        Task<bool> SaveChangesAsync();

        // ΜΕΘΟΔΟΣ ΓΙΑ ΤΟΝ ADMIN
        Task<IEnumerable<Notification>> GetAdminNotificationsAsync();
    }
}