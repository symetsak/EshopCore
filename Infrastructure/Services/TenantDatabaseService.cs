using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Services
{
    public class TenantDatabaseService : ITenantDatabaseService
    {
        public async Task CreateTenantDatabaseAsync(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            // Φτιάχνουμε έναν προσωρινό provider ειδικά για τη δημιουργία αυτής της βάσης
            var temporaryProvider = new InitialTenantProvider { ConnectionString = connectionString };

            using (var tenantContext = new ApplicationDbContext(optionsBuilder.Options, temporaryProvider))
            {
                await tenantContext.Database.MigrateAsync();
            }
        }
    }

    // Μια μικρή βοηθητική κλάση για τη ροή του initialization
    public class InitialTenantProvider : ITenantProvider
    {
        public string? TenantId { get; set; }
        public string? ConnectionString { get; set; }
    }
}