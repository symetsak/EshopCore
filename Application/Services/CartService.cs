using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Eshop.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;
        private readonly IOrderRepository _orderRepo;

        public CartService(ICartRepository cartRepo, IOrderRepository orderRepo)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
        }

        public async Task<CartResponseDto> GetCartByCustomerAsync(int customerId)
        {
            var cart = await _cartRepo.GetByCustomerIdAsync(customerId);

            // Αν ο πελάτης δεν έχει καθόλου καλάθι στη βάση, του επιστρέφουμε ένα άδειο DTO
            if (cart == null)
            {
                return new CartResponseDto { CustomerId = customerId };
            }

            // Χτίζουμε το Response DTO
            var response = new CartResponseDto
            {
                Id = cart.Id,
                CustomerId = cart.CustomerId,
                Items = cart.CartItems.Select(ci => new CartItemResponseDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    ProductImageUrl = ci.Product.ImageUrl ?? string.Empty, // Αν έχεις ImageUrl στο Product entity
                    Price = ci.Product.Price,             // ΕΔΩ ΑΥΡΙΟ ΘΑ ΜΠΑΙΝΕΙ Η ΤΙΜΗ ΤΗΣ ΠΡΟΣΦΟΡΑΣ (Offer Price)!
                    Quantity = ci.Quantity
                }).ToList()
            };

            // ΕΔΩ ΑΥΡΙΟ ΘΑ ΚΑΛΟΥΜΕ ΤΗ ΛΟΓΙΚΗ ΤΩΝ ΚΟΥΠΟΝΙΩΝ!
            // π.χ. response.Discount = await _couponService.CalculateDiscountAsync(cart);

            return response;
        }

        public async Task AddOrUpdateItemAsync(int customerId, AddToCartDto dto)
        {
            // Κλήση του Repo για προσθήκη ή αύξηση ποσότητας
            await _cartRepo.AddOrUpdateItemAsync(customerId, dto.ProductId, dto.Quantity);
            await _cartRepo.SaveChangesAsync();
        }

        public async Task RemoveItemAsync(int customerId, int productId)
        {
            await _cartRepo.RemoveItemAsync(customerId, productId);
            await _cartRepo.SaveChangesAsync();
        }

        public async Task ClearCartAsync(int customerId)
        {
            await _cartRepo.ClearCartAsync(customerId);
            await _cartRepo.SaveChangesAsync();
        }

        public async Task<int> CheckoutAsync(int customerId)
        {
            // 1. Φέρνουμε το καλάθι του πελάτη με τα items και τα προϊόντα
            var cart = await _cartRepo.GetByCustomerIdAsync(customerId);
            if (cart == null || !cart.CartItems.Any())
            {
                throw new InvalidOperationException("Δεν μπορεί να γίνει checkout σε άδειο καλάθι.");
            }

            // 2. Υπολογίζουμε το συνολικό ποσό (εδώ θα μπουν και τα κουπόνια αύριο!)
            decimal subTotal = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);
            decimal discount = 0; // Placeholder για κουπόνια
            decimal total = subTotal - discount;

            // 3. Δημιουργούμε το Order Entity
            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                Status = "Pending", // Ή OrderStatus.Pending αν έχεις Enum

                // Μετατρέπουμε τα CartItems σε OrderItems
                OrderItems = cart.CartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price // Κλειδώνουμε την τιμή αγοράς!
                }).ToList()
            };

            // 4. Αποθηκεύουμε την παραγγελία μέσω του OrderRepository
            await _orderRepo.AddAsync(order); // Αν το Repo σου έχει AddAsync
            await _orderRepo.SaveChangesAsync();

            // 5. ΑΔΕΙΑΖΟΥΜΕ το καλάθι του χρήστη, αφού ολοκληρώθηκε η αγορά!
            await _cartRepo.ClearCartAsync(customerId);
            await _cartRepo.SaveChangesAsync();

            return order.Id; // Επιστρέφουμε το ID της νέας παραγγελίας
        }
    }
}