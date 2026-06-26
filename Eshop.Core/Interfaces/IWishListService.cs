using System.Collections.Generic;
using System.Threading.Tasks;
using Eshop.Application.DTOs;

namespace Eshop.Application.Services
{
    public interface IWishlistService
    {
        // Επιστρέφει όλα τα αγαπημένα προϊόντα του πελάτη
        Task<IEnumerable<WishlistResponseDto>> GetCustomerWishlistAsync(int customerId);

        // Η "Toggle" μέθοδος: Επιστρέφει string που λέει "Added" ή "Removed" για να ξέρει το frontend τι έγινε
        Task<string> ToggleWishlistAsync(int customerId, int productId);
    }
}