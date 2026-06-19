using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;

namespace Eshop.Application.Payments
{
    public class StripePaymentStrategy : IPaymentStrategy
    {
        private readonly IConfiguration _configuration;

        public StripePaymentStrategy(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> CreateCheckoutSessionAsync(Order order, string tenantId)
        {
            // 1. Ρυθμίζουμε το Secret Key που έχουμε στα User Secrets
            Stripe.StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            // 2. Μετατροπή του TotalAmount από decimal σε cents (long)
            // Παράδειγμα: 19.99 * 100 = 1999
            long amountInCents = Convert.ToInt64(order.TotalAmount * 100);

            // 3. Ορισμός των επιλογών για το Stripe Checkout Session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" }, // Μόνο κάρτα για αρχή
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amountInCents,
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Παραγγελία #{order.Id}",
                                Description = "Ολοκλήρωση αγοράς από το Eshop μας"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                // URLs επιστροφής
                SuccessUrl = _configuration["Stripe:SuccessUrl"],
                CancelUrl = _configuration["Stripe:CancelUrl"],

                // THE ENTERPRISE MAGIC: Κρύβουμε τα IDs στα Metadata για να τα βρούμε στο Webhook!
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", order.Id.ToString() },
                    { "TenantId", tenantId }
                }
            };

            var service = new SessionService();
            // Εκτελούμε το request στη Stripe
            Session session = await service.CreateAsync(options);

            // Επιστρέφουμε το URL στο οποίο πρέπει να ανακατευθυνθεί ο πελάτης
            return session.Url;
        }
    }
}