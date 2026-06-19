using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface IPaymentStrategy
    {
        // Δημιουργεί τη συνεδρία πληρωμής και επιστρέφει το URL (Stripe ή Viva)
        Task<string> CreateCheckoutSessionAsync(Order order, string tenantId);
    }
}