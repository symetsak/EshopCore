using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Stripe;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Eshop.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly ICouponService _couponService;
        private readonly IPaymentStrategyFactory _paymentFactory;
        private readonly IOrderService _orderService;

        public CartService(ICartRepository cartRepo, IOrderRepository orderRepo, ICouponService couponService, IPaymentStrategyFactory paymentFactory, IOrderService orderService)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _couponService = couponService;
            _paymentFactory = paymentFactory;
            _orderService = orderService;
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

        public async Task<CheckoutResultDto> CheckoutAsync(int customerId, string paymentProvider, string tenantId, string paymentMethod)
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

            // 3. Προετοιμασία του DTO για το OrderService
            var orderCreateDto = new OrderCreateDto
            {
                PaymentMethod = paymentMethod,
                OverrideTotalAmount = total, // Περνάμε το τελικό ποσό (με την έκπτωση) στο OrderService
                OrderItems = cart.CartItems.Select(ci => new OrderItemCreateDto
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity
                }).ToList()
            };

            // 4. ΟΛΗ Η ΔΟΥΛΕΙΑ (Stock, Statuses, Base Order Entity, Save, Notifications) γίνεται πλέον ΕΔΩ!
            var createdOrderDto = await _orderService.CreateOrderAsync(customerId, orderCreateDto);

            // 5. Καθαρισμός κουπονιού και καλαθιού
            cart.AppliedCouponCode = null;
            await _cartRepo.SaveChangesAsync();
            await _cartRepo.ClearCartAsync(customerId);
            await _cartRepo.SaveChangesAsync();

            // 6. Financial Logic για το Stripe Link
            string paymentUrl = string.Empty;

            if (paymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(paymentProvider))
                {
                    throw new InvalidOperationException("Για πληρωμή με κάρτα, απαιτείται η επιλογή παρόχου πληρωμών (π.χ. Stripe).");
                }

                // Επειδή η στρατηγική του Stripe (paymentStrategy) ενδέχεται να ζητάει το Order Entity από τη βάση, το τραβάμε μέσω του Repo
                var orderEntity = await _orderRepo.GetByIdAsync(createdOrderDto.Id);
                if (orderEntity != null)
                {
                    var paymentStrategy = _paymentFactory.GetPaymentStrategy(paymentProvider);
                    paymentUrl = await paymentStrategy.CreateCheckoutSessionAsync(orderEntity, tenantId);
                }
            }

            // 7. Επιστροφή αποτελέσματος
            return new CheckoutResultDto
            {
                OrderId = createdOrderDto.Id,
                Url = paymentUrl
            };
        }

        public Task<int> CheckoutAsync(int customerId)
        {
            throw new NotImplementedException();
        }
    }
}