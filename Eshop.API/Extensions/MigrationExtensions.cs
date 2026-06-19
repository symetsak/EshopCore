using Eshop.Infrastructure.Data;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Eshop.API.Extensions
{
    public static class MigrationExtensions
    {
        public static async Task ApplyTenantMigrationsAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Έναρξη αυτόματων migrations για τη Master Βάση...");
                var masterContext = services.GetRequiredService<MasterDbContext>();
                await masterContext.Database.MigrateAsync(); // Ενημερώνει τη Master βάση αν έχει εκκρεμότητες

                var tenantRepo = services.GetRequiredService<ITenantRepository>();
                var tenants = await tenantRepo.GetAllAsync(); // Παίρνει όλους τους tenants από τη Master

                logger.LogInformation("Βρέθηκαν {Count} Tenants. Έναρξη migrations για τις βάσεις τους...", tenants.Count());

                foreach (var tenant in tenants)
                {
                    if (string.IsNullOrEmpty(tenant.ConnectionString)) continue;

                    logger.LogInformation("Εκτέλεση Migration για τον Tenant: {TenantId}...", tenant.Id);

                    // Δημιουργούμε ένα dynamic instance του ApplicationDbContext ειδικά γι' αυτό το connection string
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder.UseNpgsql(tenant.ConnectionString);

                    using (var tenantContext = new ApplicationDbContext(optionsBuilder.Options))
                    {
                        await tenantContext.Database.MigrateAsync(); // ΤΡΕΧΕΙ ΤΟ MIGRATION ΣΤΗ ΒΑΣΗ ΤΟΥ TENANT!
                    }
                }

                logger.LogInformation("Όλα τα migrations εκτελέστηκαν επιτυχώς!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Προέκυψε σοβαρό σφάλμα κατά την εκτέλεση των αυτόματων migrations!");
            }
        }
    }
}