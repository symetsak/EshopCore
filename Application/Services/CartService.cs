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
        private readonly IPaymentStrategyFactory _paymentFactory;

        public CartService(ICartRepository cartRepo, IOrderRepository orderRepo, ICouponService couponService, IPaymentStrategyFactory paymentFactory)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _couponService = couponService;
            _paymentFactory = paymentFactory;
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
                    Price = ci.Product.CurrentPrice,             
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

        public async Task<CheckoutResultDto> CheckoutAsync(int customerId, string paymentProvider, string tenantId)
        {
            // 1. Φέρνουμε το καλάθι του πελάτη με τα items και τα προϊόντα
            var cart = await _cartRepo.GetByCustomerIdAsync(customerId);
            if (cart == null || !cart.CartItems.Any())
            {
                throw new InvalidOperationException("Δεν μπορεί να γίνει checkout σε άδειο καλάθι.");
            }

            // 2. Υπολογίζουμε το συνολικό ποσό (εδώ θα μπουν και τα κουπόνια αύριο!)
            decimal subTotal = cart.CartItems.Sum(ci => ci.Product.CurrentPrice * ci.Quantity);

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
                    UnitPrice = ci.Product.CurrentPrice // Κλειδώνουμε την τιμή αγοράς!
                }).ToList()
            };

            // 4. Αποθηκεύουμε την παραγγελία μέσω του OrderRepository
            await _orderRepo.AddAsync(order); // Αν το Repo σου έχει AddAsync
            await _orderRepo.SaveChangesAsync();

            // 5. ΠΡΩΤΑ μηδενίζουμε το κουπόνι πάνω στο cart object
            cart.AppliedCouponCode = null;

            // 6. Σώζουμε την αλλαγή του κουπονιού στη βάση
            await _cartRepo.SaveChangesAsync();

            // 7. Μετά αδειάζουμε τα προϊόντα του καλαθιού
            await _cartRepo.ClearCartAsync(customerId);
            await _cartRepo.SaveChangesAsync();

            var paymentStrategy = _paymentFactory.GetPaymentStrategy(paymentProvider);

            // 8. Παράγουμε το URL (Η Stripe θα πάρει το order entity και το tenantId)
            string paymentUrl = await paymentStrategy.CreateCheckoutSessionAsync(order, tenantId);

            // 9. Επιστρέφουμε το συνδυασμένο αποτέλεσμα
            return new CheckoutResultDto
            {
                OrderId = order.Id,
                Url = paymentUrl
            };
        }

        public Task<int> CheckoutAsync(int customerId)
        {
            throw new NotImplementedException();
        }
    }
}