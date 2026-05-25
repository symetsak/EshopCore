using Microsoft.EntityFrameworkCore;
using Eshop.Core.Entities;

namespace Eshop.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Αυτός ο constructor επιτρέπει να περνάμε δυναμικά το Connection String
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ρύθμιση σχέσης 1-προς-Πολλά (Category -> Products)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}