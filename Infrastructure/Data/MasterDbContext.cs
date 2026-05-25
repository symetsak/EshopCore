using Microsoft.EntityFrameworkCore;
using Eshop.Core.Entities; // Φέρνουμε το Tenant Entity από το Core

namespace Eshop.Infrastructure.Data
{
    public class MasterDbContext : DbContext
    {
        public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ρυθμίζουμε τον πίνακα Tenants με Fluent API
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("Tenants"); // Όνομα πίνακα στην Postgres
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ConnectionString).IsRequired();
            });
        }
    }
}