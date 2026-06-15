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
        private readonly ICouponService _couponService;

        public CartService(ICartRepository cartRepo, IOrderRepository orderRepo, ICouponService couponService)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _couponService = couponService;
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
                    ProductImageUrl = ci.Product.ImageUrl ?? string.Empty, 
                    Price = ci.Product.Price,             
                    Quantity = ci.Quantity
                }).ToList()
            };

            // ΕΔΩ ΓΙΝΕΤΑΙ Η ΜΑΓΕΙΑ ΤΩΝ ΚΟΥΠΟΝΙΩΝ!
            if (!string.IsNullOrEmpty(cart.AppliedCouponCode))
            {
                // Καλούμε το CouponService να μας πει πόση έκπτωση δικαιούται ο πελάτης
                response.Discount = await _couponService.CalculateDiscountAsync(cart.AppliedCouponCode, response.SubTotal);

                // Αν για κάποιο λόγο το κουπόνι έληξε ή δεν πιάνει πια το όριο (π.χ. ο χρήστης αφαίρεσε προϊόντα),
                // το discount θα επιστρέψει 0, οπότε το frontend θα ξέρει ότι δεν ισχύει.
            }

            return response;
        }

        public async Task ApplyCouponAsync(int customerId, string couponCode)
        {
            var cart = await _cartRepo.GetByCustomerIdAsync(customerId);
            if (cart == null)
            {
                throw new InvalidOperationException("Δεν βρέθηκε ενεργό καλάθι για αυτόν τον πελάτη.");
            }

            // Αποθηκεύουμε τον κωδικό του κουπονιού στο καλάθι
            cart.AppliedCouponCode = couponCode;

            await _cartRepo.SaveChangesAsync();
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

            decimal discount = 0; 

            if (!string.IsNullOrEmpty(cart.AppliedCouponCode))
            {
                discount = await _couponService.CalculateDiscountAsync(cart.AppliedCouponCode, subTotal);
            }
            decimal total = subTotal - discount;

            // 3. Δημιουργούμε το Order Entity
            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                Status = "Pending", 

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
            
            cart.AppliedCouponCode = null; // Προσοχή: Όταν αδειάζει το καλάθι, μηδενίζουμε και το εφαρμοσμένο κουπόνι για την επόμενη αγορά

            await _cartRepo.SaveChangesAsync();

            return order.Id; // Επιστρέφουμε το ID της νέας παραγγελίας
        }
    }
}