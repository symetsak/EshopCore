using Eshop.Core.Interfaces;
using Stripe;
using Eshop.API.Hubs; 
using Microsoft.AspNetCore.SignalR; 


namespace Eshop.API.Extensions
{
    public static class StripeWebhookExtensions
    {
        public static void MapStripeWebhook(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/webhooks/stripe", async (
                HttpContext context,
                IConfiguration configuration,
                IServiceProvider serviceProvider) =>
            {
                // 1. Έλεγχος για το Signature της Stripe
                if (!context.Request.Headers.TryGetValue("Stripe-Signature", out var stripeSignature))
                {
                    return Results.BadRequest("Missing Stripe-Signature.");
                }

                // 2. Διάβασμα του Raw Body
                string json;
                using (var reader = new StreamReader(context.Request.Body))
                {
                    json = await reader.ReadToEndAsync();
                }

                // 3. Ανάγνωση του Configuration με τον απόλυτο τρόπο μέσω RequestServices
                var config = context.RequestServices.GetRequiredService<IConfiguration>();

                var eshopNotificationService = context.RequestServices.GetRequiredService<IEshopNotificationService>();

                var webhookSecret = config.GetValue<string>("PaymentProviders:Stripe:WebhookSecret");

                // ΑΝ ΚΑΙ ΠΑΛΙ ΓΙΑ ΚΑΠΟΙΟ ΜΥΣΤΗΡΙΩΔΗ ΛΟΓΟ ΒΓΑΛΕΙ NULL, 
                // βάλε εδώ το string καρφωτό για να ηρεμήσεις, και στο production το αλλάζουμε!
                if (string.IsNullOrEmpty(webhookSecret))
                {
                    webhookSecret = "whsec_16017989cad996d0327b4483934f94cb6749cb898c5598e1c5ca03e5443afb32";
                }

                try
                {
                    // 4. Επαλήθευση του Event
                    var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

                    // 5. Επεξεργασία μόνο του Checkout Session Completed
                    if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                    {
                        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

                        if (session != null && session.Metadata != null)
                        {
                            bool hasOrderId = session.Metadata.TryGetValue("OrderId", out var orderIdStr);
                            bool hasTenantId = session.Metadata.TryGetValue("TenantId", out var tenantId);

                            if (hasOrderId && hasTenantId && int.TryParse(orderIdStr, out int orderId))
                            {
                                using var scope = serviceProvider.CreateScope();

                                // 1. Φέρνουμε τον TenantProvider και το TenantRepository (που διαβάζει τη Master Βάση)
                                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                                var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();

                                // 2. Ψάχνουμε τον Tenant στη Master βάση για να βρούμε το Connection String του
                                var tenantInfo = await tenantRepo.GetByIdAsync(tenantId!);

                                if (tenantInfo != null && !string.IsNullOrEmpty(tenantInfo.ConnectionString))
                                {
                                    // ΕΔΩ ΓΙΝΕΤΑΙ Η ΜΑΓΕΙΑ: Κλειδώνουμε και τα δύο στον Provider!
                                    tenantProvider.TenantId = tenantId;
                                    tenantProvider.ConnectionString = tenantInfo.ConnectionString;

                                    // 3. Τώρα που ο Provider έχει το Connection String, το OrderRepository θα συνδεθεί στη ΣΩΣΤΗ βάση!
                                    var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                                    var order = await orderRepo.GetByIdAsync(orderId);
                                    if (order != null)
                                    {
                                        order.Status = "Paid";
                                        await orderRepo.SaveChangesAsync();
                                        Console.WriteLine($"[Stripe Webhook] Η παραγγελία {orderId} για τον Tenant {tenantId} έγινε PAID!");

                                        // Φτιάχνουμε το όνομα του Group βάσει του Tenant
                                        var tenantGroupName = $"Group_{tenantId!.ToLower().Trim()}";

                                        // Στέλνουμε το live καμπανάκι ΑΠΟΚΛΕΙΣΤΙΚΑ στο back-office (στους Admins) του Tenant!
                                        await eshopNotificationService.SendToAdminsAsync(
                                            tenantId,
                                            "Νέα Πληρωμή!",
                                            $"Η παραγγελία #{orderId} πληρώθηκε επιτυχώς μέσω Stripe.",
                                            new { orderId = orderId }
                                        );

                                        Console.WriteLine($"[SignalR] Εστάλη live ειδοποίηση στους Admins του Tenant {tenantId} για την παραγγελία #{orderId}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[Stripe Webhook] Η παραγγελία {orderId} δεν βρέθηκε στη βάση του Tenant {tenantId}!");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[Stripe Webhook] Δεν βρέθηκαν πληροφορίες σύνδεσης για τον Tenant {tenantId}!");
                                }
                            }
                        }
                    }

                    return Results.Ok();
                }
                catch (StripeException ex)
                {
                    Console.WriteLine($"[Stripe Webhook Error] Verification failed: {ex.Message}");
                    return Results.BadRequest();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Stripe Webhook Internal Error] {ex.Message}");
                    return Results.StatusCode(500);
                }
            })
            .AllowAnonymous()
            .ExcludeFromDescription(); // Κρύβει το webhook από το Swagger για να μην λερώνει το UI σου
        }
    }
}