using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Eshop.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product) // Φέρνουμε και το Product για να ξέρουμε τιμή, όνομα κλπ.
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task AddOrUpdateItemAsync(int customerId, int productId, int quantity)
        {
            // 1. Βρίσκουμε ή δημιουργούμε το καλάθι του πελάτη
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                cart = new Cart { CustomerId = customerId };
                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync(); // Σώζουμε για να πάρει Id το Cart
            }

            // 2. Ελέγχουμε αν το προϊόν υπάρχει ήδη στο καλάθι
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (existingItem != null)
            {
                // Αν υπάρχει, αυξάνουμε την ποσότητα
                existingItem.Quantity += quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Αν δεν υπάρχει, φτιάχνουμε νέα γραμμή στο καλάθι
                var newItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.CartItems.AddAsync(newItem);
            }
        }

        public async Task RemoveItemAsync(int customerId, int productId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart != null)
            {
                var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (item != null)
                {
                    _context.CartItems.Remove(item);
                }
            }
        }

        public async Task ClearCartAsync(int customerId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart != null && cart.CartItems.Any())
            {
                _context.CartItems.RemoveRange(cart.CartItems);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}