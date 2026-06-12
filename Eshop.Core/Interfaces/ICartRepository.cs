using Eshop.Core.Entities;
using System.Threading.Tasks;

namespace Eshop.Core.Interfaces
{
    public interface ICartRepository
    {
        // Φέρνει το καλάθι του πελάτη μαζί με όλα τα CartItems και τα Products τους
        Task<Cart?> GetByCustomerIdAsync(int customerId);

        // Προσθέτει ή ενημερώνει ένα Item μέσα στο καλάθι
        Task AddOrUpdateItemAsync(int customerId, int productId, int quantity);

        // Αφαιρεί τελείως ένα προϊόν από το καλάθι
        Task RemoveItemAsync(int customerId, int productId);

        // Αδειάζει τελείως το καλάθι (π.χ. μετά από επιτυχημένο checkout)
        Task ClearCartAsync(int customerId);

        // Αποθήκευση αλλαγών
        Task SaveChangesAsync();
    }
}