using Eshop.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Eshop.Application.Payments
{
    public class PaymentStrategyFactory : IPaymentStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PaymentStrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPaymentStrategy GetPaymentStrategy(string providerName)
        {
            // Μετατρέπουμε σε πεζά για να μην έχουμε θέματα με κεφαλαία/μικρά
            return providerName.ToLower() switch
            {
                "stripe" => _serviceProvider.GetRequiredService<StripePaymentStrategy>(),

                // ΑΥΡΙΟ: Εδώ θα προσθέσουμε απλά τη Viva:
                // "viva" => _serviceProvider.GetRequiredService<VivaPaymentStrategy>(),

                _ => throw new NotImplementedException($"Ο πάροχος πληρωμών '{providerName}' δεν υποστηρίζεται ακόμα.")
            };
        }
    }
}