using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Eshop.API.Middleware
{
    public class TenantResolverMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolverMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, MasterDbContext masterDbContext)
        {
            // 1. Καθαρισμός και προετοιμασία του Path και της Μεθόδου του Request
            var path = context.Request.Path.Value?.Trim().ToLower() ?? string.Empty;
            var method = context.Request.Method.ToUpper();

            // 2. ΕΞΑΙΡΕΣΕΙΣ (Skip): Προσπερνάμε το Middleware για endpoints που δεν απαιτούν Tenant Connection String.
            //    - Το Swagger UI και τα assets του.
            //    - Το Root endpoint (/).
            //    - Το POST /api/Tenants (Δημιουργία νέου Tenant, καθώς η βάση του δεν υπάρχει ακόμα).
            //    - ΣΗΜΑΝΤΙΚΟ: Αφαιρέσαμε το "users/login" από εδώ, γιατί το Login ΧΡΕΙΑΖΕΤΑΙ το Connection String 
            //      για να ελέγξει τον Admin στον πίνακα Users του εκάστοτε Tenant!
            if (path == "/" ||
                path.Contains("swagger") ||
                path.Contains("favicon") ||
                (path.Contains("tenants") && method == "POST"))
            {
                await _next(context);
                return;
            }

            // 3. ΕΛΕΓΧΟΣ HEADER: Προσπαθούμε να διαβάσουμε το Tenant ID από το HTTP Header
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
            {
                var tId = tenantId.ToString().Trim().ToLower();

                // 4. ΑΝΑΖΗΤΗΣΗ ΣΤΗ MASTER DB: Ψάχνουμε τον πελάτη στην κεντρική βάση δεδομένων
                var tenant = await masterDbContext.Tenants.FindAsync(tId);

                if (tenant != null)
                {
                    // Α) Έλεγχος αν ο Tenant είναι ενεργός
                    if (!tenant.IsActive)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "text/plain; charset=utf-8";
                        await context.Response.WriteAsync($"Ο πελάτης '{tId}' είναι απενεργοποιημένος.");
                        return;
                    }

                    // Β) Δυναμική ανάθεση τιμών απευθείας στις έτοιμες ιδιότητες του TenantProvider
                    tenantProvider.TenantId = tenant.Id;
                    tenantProvider.ConnectionString = tenant.ConnectionString;

                    // Διαγνωστικό μήνυμα στην κονσόλα του Visual Studio
                    Console.WriteLine($"[Multi-Tenancy] Επιτυχής σύνδεση στο Tenant: {tenant.Id}");

                    // Αυτόματο migrate στην βάση κατά την κλήση request (προαιρετικό, αλλά χρήσιμο για ανάπτυξη)
                    var dbContext = context.RequestServices.GetRequiredService<Eshop.Infrastructure.Data.ApplicationDbContext>();
                    await dbContext.Database.MigrateAsync();
                }
                else
                {
                    // Γ) Αν το ID δεν αντιστοιχεί σε κανέναν Tenant στη MasterDB, επιστρέφουμε 404
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync($"Ο πελάτης (Tenant) με ID '{tId}' δεν βρέθηκε στην κεντρική βάση.");
                    return;
                }
            }
            else
            {
                // 5. ΑΜΥΝΤΙΚΟΣ ΠΡΟΓΡΑΜΜΑΤΙΣΜΟΣ: Αν ο χρήστης κάλεσε προστατευμένο endpoint (π.χ. Login, Products) 
                //    χωρίς να στείλει το Header, τον μπλοκάρουμε με 400 Bad Request αντί να αφήσουμε να σκάσει 500άρι.
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("Λείπει το απαιτούμενο HTTP Header 'X-Tenant-Id'.");
                return;
            }

            // 6. Όλα πήγαν καλά, ο Provider ενημερώθηκε, προχωράμε στον Controller
            await _next(context);
        }
    }
}