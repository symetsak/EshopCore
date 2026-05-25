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

            using (var tenantContext = new ApplicationDbContext(optionsBuilder.Options))
            {
                await tenantContext.Database.MigrateAsync();
            }
        }
    }
}