using Eshop.Core.DTOs;
using System.Threading.Tasks;

namespace Eshop.Core.Interfaces
{
    public interface ICartService
    {
        Task<CartResponseDto> GetCartByCustomerAsync(int customerId);
        Task AddOrUpdateItemAsync(int customerId, AddToCartDto dto);
        Task RemoveItemAsync(int customerId, int productId);
        Task ClearCartAsync(int customerId);
        Task<int> CheckoutAsync(int customerId);
        Task ApplyCouponAsync(int customerId, string couponCode);
        Task<CheckoutResultDto> CheckoutAsync(int customerId, string paymentProvider, string tenantId);
    }
}