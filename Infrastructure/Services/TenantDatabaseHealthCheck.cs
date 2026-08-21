using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Eshop.Infrastructure.Data;
using Npgsql; // Χρησιμοποιούμε τον native driver για αστραπιαίο Ping

namespace Eshop.Infrastructure.Services
{
    public class TenantDatabasesHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Χρησιμοποιούμε IServiceScopeFactory γιατί το Health Check τρέχει ως Singleton
        // και δεν μπορούμε να ζητήσουμε το MasterDbContext (που είναι Scoped) απευθείας.
        public TenantDatabasesHealthCheck(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var masterContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

            // Βρίσκουμε όλους τους πελάτες. 
            // (Προσάρμοσε το ".Tenants" αν ο πίνακάς σου λέγεται αλλιώς, π.χ. ".Stores")
            var tenants = await masterContext.Tenants.ToListAsync(cancellationToken);

            var unhealthyTenants = new List<string>();

            foreach (var tenant in tenants)
            {
                try
                {
                    // Κάνουμε ένα αστραπιαίο και "ελαφρύ" Ping στη βάση του πελάτη
                    using var connection = new NpgsqlConnection(tenant.ConnectionString);
                    await connection.OpenAsync(cancellationToken);
                }
                catch (Exception)
                {
                    // Αν σκάσει, καταγράφουμε το Id ή το Όνομα του Tenant
                    unhealthyTenants.Add(tenant.Id.ToString());
                }
            }

            // Αν βρήκαμε έστω και μία πεσμένη βάση, ρίχνουμε το status σε "Degraded" (Υποβαθμισμένο)
            if (unhealthyTenants.Any())
            {
                return HealthCheckResult.Degraded(
                    description: $"Οι εξής Tenants έχουν πέσει: {string.Join(", ", unhealthyTenants)}");
            }

            // Όλα πράσινα!
            return HealthCheckResult.Healthy("Όλες οι βάσεις των Tenants λειτουργούν άψογα.");
        }
    }
}