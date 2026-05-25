using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Eshop.Infrastructure.Data
{
    // Αυτή η κλάση τρέχει ΜΟΝΟ την ώρα που γράφεις Add-Migration στην κονσόλα
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Του δίνουμε ένα εικονικό/προσωρινό connection string απλώς για να πάρει μπρος το εργαλείο.
            // Δεν μας νοιάζει αν είναι αληθινό, γιατί δεν θα τρέξει στη βάση τώρα!
            optionsBuilder.UseNpgsql("Host=localhost;Database=DummyTenantDb;Username=postgres;Password=dummy");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}