namespace Eshop.Core.Interfaces
{
    public interface IEshopNotificationService
    {
        // Για να στέλνουμε ειδοποιήσεις στους Admins του Tenant
        Task SendToAdminsAsync(string tenantId, string title, string message, object? data = null);

        // Για να στέλνουμε ειδοποίηση αποκλειστικά σε ΕΝΑΝ Customer
        Task SendToCustomerAsync(string tenantId, int customerId, string title, string message, object? data = null);

        // Για να ενημερώνουμε live τα tabs ενός Customer όταν αλλάζει το unread count (π.χ. μετά από Read)
        Task SyncCustomerUnreadCountAsync(string tenantId, int customerId);
    }
}