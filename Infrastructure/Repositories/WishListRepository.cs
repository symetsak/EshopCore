using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly ApplicationDbContext _context;

        // Inject τον DB Context του Tenant
        public WishlistRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Λήψη Wishlist με .Include(w => w.Product) για να φέρουμε και τα δεδομένα του προϊόντος!
        public async Task<IEnumerable<Wishlist>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Wishlists
                .Include(w => w.Product) // Κρίσιμο: Φέρνει το όνομα, τιμή, εικόνα από τον πίνακα Products
                .Where(w => w.CustomerId == customerId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }

        // Προσθήκη
        public async Task AddAsync(Wishlist wishlist)
        {
            await _context.Wishlists.AddAsync(wishlist);
        }

        // Αφαίρεση (Σύγχρονη μέθοδος, δεν χρειάζεται async για το Remove, το SaveChanges κάνει τη δουλειά)
        public void Remove(Wishlist wishlist)
        {
            _context.Wishlists.Remove(wishlist);
        }

        // Έλεγχος για διπλοεγγραφές ή για να βρούμε την εγγραφή προς διαγραφή
        public async Task<Wishlist?> GetExistingAsync(int customerId, int productId)
        {
            return await _context.Wishlists
                .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.ProductId == productId);
        }

        // Save
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Wishlist>> GetByProductIdAsync(int productId)
        {
            return await _context.Wishlists
                .Where(w => w.ProductId == productId)
                .ToListAsync();
        }
    }
}