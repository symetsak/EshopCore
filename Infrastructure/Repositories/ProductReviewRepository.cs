using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Repositories
{
    public class ProductReviewRepository : IProductReviewRepository
    {
        private readonly ApplicationDbContext _context;

        // Κάνουμε Inject το ApplicationDbContext του Tenant
        public ProductReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Φέρνει τις κριτικές ταξινομημένες από τις πιο πρόσφατες
        public async Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId)
        {
            return await _context.ProductReviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // Προσθήκη κριτικής
        public async Task AddAsync(ProductReview review)
        {
            await _context.ProductReviews.AddAsync(review);
        }

        // Ο κρίσιμος έλεγχος για Verified Buyer!
        public async Task<bool> HasCustomerPurchasedProductAsync(int customerId, int productId)
        {
            // Ψάχνουμε αν υπάρχει ΕΣΤΩ ΚΑΙ ΜΙΑ παραγγελία του συγκεκριμένου Customer,
            // η οποία είναι πληρωμένη ("Paid"), και περιέχει το ProductId στα είδη της (OrderItems).
            return await _context.Orders
                .AnyAsync(o => o.CustomerId == customerId
                            && o.Status == "Paid"
                            && o.OrderItems.Any(item => item.ProductId == productId));
        }

        // Δ) Αποθήκευση
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}