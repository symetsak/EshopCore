using System.Collections.Generic;
using System.Threading.Tasks;
using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface IWishlistRepository
    {
        // Φέρνει όλα τα αντικείμενα της Wishlist για έναν συγκεκριμένο πελάτη (μαζί με τα Products)
        Task<IEnumerable<Wishlist>> GetByCustomerIdAsync(int customerId);

        // Eπιστρέφει ποιες wishlists έχουν το συγκεκριμένο προϊόν
        Task<IEnumerable<Wishlist>> GetByProductIdAsync(int productId);

        // Προσθέτει ένα προϊόν στα αγαπημένα
        Task AddAsync(Wishlist wishlist);

        // Αφαιρεί ένα προϊόν από τα αγαπημένα
        void Remove(Wishlist wishlist);

        // Έλεγχος αν το προϊόν υπάρχει ήδη στη Wishlist αυτού του πελάτη
        Task<Wishlist?> GetExistingAsync(int customerId, int productId);

        // SaveChanges
        Task<bool> SaveChangesAsync();
    }
}