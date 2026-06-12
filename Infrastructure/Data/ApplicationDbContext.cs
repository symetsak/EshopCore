using Microsoft.EntityFrameworkCore;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantProvider? _tenantProvider;

        // O overloaded constructor για τα migrations στο Program.cs
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
        {
            // Εδώ δεν χρειαζόμαστε τον provider, οπότε τον αφήνουμε null ή empty
        }

        // Αυτός ο constructor επιτρέπει να περνάμε δυναμικά το Connection String
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        // Αυτή η μέθοδος τρέχει αυτόματα πριν το EF συνδεθεί στη βάση
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Αν το optionsBuilder δεν έχει ρυθμιστεί ήδη (π.χ. από το DesignTime Factory)
            if (!optionsBuilder.IsConfigured)
            {
                var connString = _tenantProvider!.ConnectionString;

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

            // 1. SEEDING ΓΙΑ ΚΑΤΗΓΟΡΙΕΣ
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Λοιπά",
                    DisplayOrder = 1
                }
            );

            // 2. SEEDING ΓΙΑ ΤΟΝ ADMIN USER
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin", 
                    Email = "admin@eshop.com",
                    PasswordHash = "$2a$11$fX1Z7.h2bXQenQ/K3d0fbeU3Zp7Z7WkO8/j7YAnF.gXjbe5Q2WdmG", // Αντιπροσωπεύει το κρυπτογραφημένο Admin123!
                    FirstName = "System",
                    LastName = "Admin",
                    Role = "Administrator", 
                    IsFirstLogin = true, // Υποχρεώνει τον χρήστη για αλλαγή στο 1ο login
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // 1. Σχέση Category -> Products (1-to-Many)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Σχέση Customer -> Orders (1-to-Many)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
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

            // Σχέση User -> RefreshTokens (1-to-Many): Ένας χρήστης μπορεί να έχει πολλά Refresh Tokens από διαφορετικές συσκευές)
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany() // Αν θες, μπορείς να αφήσεις άδεια τη λίστα στο User
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Αν διαγραφεί ο χρήστης, σβήνονται αυτόματα και τα tokens του

            // Σχέσεις για το Καλάθι (Cart -> CartItems)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade); // Αν διαγραφεί το καλάθι, σβήνονται και τα items

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany() // Δεν χρειάζεται λίστα από CartItems μέσα στο Product
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Δεν αφήνουμε να διαγραφεί προϊόν αν είναι μέσα σε καλάθι
        }
    }
}