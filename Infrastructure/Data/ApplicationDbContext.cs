using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
        public DbSet<UserNote> UserNotes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OrderReturn> OrderReturns { get; set; }
        public DbSet<OrderReturnItem> OrderReturnItems { get; set; }

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

            optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
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

            // 2. Σχέση Customer -> Orders (1-to-Many) & Ρυθμίσεις Παραγγελίας
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasOne(o => o.Customer)
                      .WithMany(c => c.Orders)
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Ρύθμιση για τον Τρόπο Πληρωμής
                entity.Property(o => o.PaymentMethod)
                      .HasMaxLength(30)
                      .HasDefaultValue("CashOnDelivery")
                      .IsRequired();
            });

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

            modelBuilder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();

            // Ρυθμίσεις για τον πίνακα των κριτικών
            modelBuilder.Entity<ProductReview>(entity =>
            {
                // Ο τίτλος να είναι μέχρι 100 χαρακτήρες
                entity.Property(r => r.Title).HasMaxLength(100);

                // Το σχόλιο είναι προαιρετικό και μέχρι 1000 χαρακτήρες
                entity.Property(r => r.Comment).IsRequired(false).HasMaxLength(1000);

                // Περιορισμός: Το Rating πρέπει να είναι μεταξύ 1 και 5 στη βάση
                entity.ToTable(t => t.HasCheckConstraint("CK_ProductReview_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5"));
            });

            modelBuilder.Entity<Wishlist>(entity =>
            {
                // 1. Ορίζουμε Unique Index στο συνδυασμό CustomerId και ProductId
                entity.HasIndex(w => new { w.CustomerId, w.ProductId })
                      .IsUnique();

                // 2. Ρύθμιση Σχέσης: Αν διαγραφεί ένα προϊόν, σβήνεται αυτόματα και από τις Wishlists (Cascade Delete)
                entity.HasOne(w => w.Product)
                      .WithMany() // Ένα προϊόν μπορεί να είναι σε πολλές wishlists, αλλά δεν χρειαζόμαστε List<Wishlist> μέσα στο Product Entity
                      .HasForeignKey(w => w.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                // Δημιουργία Index για γρήγορο search ανά CustomerId
                entity.HasIndex(n => n.CustomerId);

                // Περιορισμοί μεγέθους
                entity.Property(n => n.Title).HasMaxLength(150).IsRequired();
                entity.Property(n => n.Message).HasMaxLength(500).IsRequired();
                entity.Property(n => n.Type).HasMaxLength(50);
            });

            modelBuilder.Entity<OrderReturn>(entity =>
            {
                // 1. Ορισμός Primary Key
                entity.HasKey(r => r.Id);

                // 2. Περιορισμοί στα κείμενα 
                entity.Property(r => r.Title).HasMaxLength(150).IsRequired();
                entity.Property(r => r.Reason).HasMaxLength(1000).IsRequired();
                entity.Property(r => r.ReturnType).HasMaxLength(20).IsRequired(); // Total ή Partial
                entity.Property(r => r.Status).HasMaxLength(30).IsRequired();     // Requested, Approved κλπ

                // 3. Σωστό mapping για το decimal 
                entity.Property(r => r.RefundAmount).HasColumnType("decimal(18,2)");

                // 4. Σχέση: Μια Παραγγελία μπορεί να έχει πολλές επιστροφές (αν γίνουν τμηματικά)
                entity.HasOne(r => r.Order)
                      .WithMany()
                      .HasForeignKey(r => r.OrderId)
                      .OnDelete(DeleteBehavior.Restrict); // Αν διαγραφεί μια παραγγελία (σπάνιο), να μην σβήσει αυτόματα η επιστροφή για λογιστικούς λόγους

                entity.Property(r => r.Iban).HasMaxLength(34).IsRequired(false); // Nullable στη βάση
            });

            modelBuilder.Entity<OrderReturnItem>(entity =>
            {
                entity.HasKey(ri => ri.Id);

                // Σωστό mapping για την τιμή μονάδας
                entity.Property(ri => ri.UnitPrice).HasColumnType("decimal(18,2)");

                // Σχέση: Αν σβηστεί το "κεφάλι" της επιστροφής (OrderReturn), σβήνονται αυτόματα και οι γραμμές της (Items)
                entity.HasOne(ri => ri.OrderReturn)
                      .WithMany(r => r.ReturnItems)
                      .HasForeignKey(ri => ri.OrderReturnId)
                      .OnDelete(DeleteBehavior.Cascade); // Εδώ θέλουμε Cascade!

                // Σύνδεση με το Προϊόν
                entity.HasOne(ri => ri.Product)
                      .WithMany()
                      .HasForeignKey(ri => ri.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}