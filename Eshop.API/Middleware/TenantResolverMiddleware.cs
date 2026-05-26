using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
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
            // 1. Προσπαθούμε να διαβάσουμε το Tenant ID από το HTTP Header
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
            {
                var tId = tenantId.ToString().Trim().ToLower();

                // 2. Ψάχνουμε τον πελάτη στην κεντρική βάση (MasterDB)
                var tenant = await masterDbContext.Tenants
                    .FirstOrDefaultAsync(t => t.Id.ToLower() == tId);

                if (tenant != null)
                {
                    if (!tenant.IsActive)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync($"Ο πελάτης '{tId}' είναι απενεργοποιημένος.");
                        return;
                    }

                    // 3. Αν υπάρχει, "γεμίζουμε" τον TenantProvider για αυτό το request
                    tenantProvider.TenantId = tenant.Id;
                    tenantProvider.ConnectionString = tenant.ConnectionString;

                    // Βοηθητικό μήνυμα στην κονσόλα του Visual Studio
                    Console.WriteLine($"[Tenancy] Επιτυχής σύνδεση στο Tenant: {tenant.Id}");
                }
                else
                {
                    // Αντί για 500 Error, επιστρέφουμε ένα καθαρό 404 στον χρήστη
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync($"Ο πελάτης (Tenant) με ID '{tId}' δεν βρέθηκε.");
                    return;
                }
            }

            // Συνεχίζει το Request κανονικά προς τον Controller
            await _next(context);
        }
    }
}