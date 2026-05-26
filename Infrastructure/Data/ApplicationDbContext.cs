using Microsoft.EntityFrameworkCore;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        // Αυτός ο constructor επιτρέπει να περνάμε δυναμικά το Connection String
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Αυτή η μέθοδος τρέχει αυτόματα πριν το EF συνδεθεί στη βάση
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Αν το optionsBuilder δεν έχει ρυθμιστεί ήδη (π.χ. από το DesignTime Factory)
            if (!optionsBuilder.IsConfigured)
            {
                var connString = _tenantProvider.ConnectionString;

                if (string.IsNullOrEmpty(connString))
                {
                    throw new InvalidOperationException("Δεν βρέθηκε έγκυρο Connection String για τον συγκεκριμένο πελάτη.");
                }

                optionsBuilder.UseNpgsql(connString);
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Σχέση Category -> Products (1-to-Many)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Σχέση User -> Orders (1-to-Many)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Σχέσεις για τον πίνακα-γέφυρα OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Αν σβηστεί η παραγγελία, σβήνονται και τα items της

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany() // Δεν χρειάζεται λίστα από OrderItems μέσα στο Product κλάση
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}