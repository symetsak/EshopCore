namespace Eshop.Core.Interfaces
{
    public interface IPaymentStrategyFactory
    {
        // Επιστρέφει τη σωστή υλοποίηση (Stripe/Viva) βάσει string
        IPaymentStrategy GetPaymentStrategy(string providerName);
    }
}