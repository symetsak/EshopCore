using Microsoft.EntityFrameworkCore;
using Eshop.Core.Entities; // Φέρνουμε το Tenant Entity από το Core
using Eshop.Core.Interfaces;

namespace Eshop.Infrastructure.Data
{
    public class MasterDbContext : DbContext , IMasterDbContext
    {
        public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<SuperAdmin> SuperAdmins { get; set; }
        public DbSet<TenantTransaction> TenantTransactions { get; set; }

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
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.Mobile).HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(1000);

                // Ορίζουμε ότι το Balance είναι δεκαδικός (χρήματα)
                entity.Property(e => e.Balance).HasColumnType("decimal(18,2)");

                // Σχέση 1-προς-πολλά (Ένας Tenant έχει πολλές Συναλλαγές)
                entity.HasMany(e => e.Transactions)
                      .WithOne(t => t.Tenant)
                      .HasForeignKey(t => t.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TenantTransaction>(entity =>
            {
                entity.ToTable("TenantTransactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<SuperAdmin>().HasData(
                new SuperAdmin
                {
                    Id = 1,
                    Username = "systemadmin",
                    Email = "admin@myeshopsaas.com",
                    PasswordHash = "$2a$11$DOIlSaF/XeooHYE/IJ5RruiOUV2vKDx08sCLJt10CcyjpeJqED8RS",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}