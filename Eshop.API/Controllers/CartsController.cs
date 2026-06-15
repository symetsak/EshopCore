using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartsController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // Helper μέθοδος για να διαβάζει το CustomerId μέσα από το JWT Token
        private int GetCurrentCustomerId()
        {
            // Ψάχνει το Claim που κρατάει το ID (συνήθως NameIdentifier ή "customerId" ανάλογα πώς το έφτιαξες στο Login)
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id") ?? User.FindFirst("CustomerId");
            if (claim == null) return 0;

            return int.TryParse(claim.Value, out var id) ? id : 0;
        }

        // 1. GET: api/carts -> Το καλάθι του συνδεδεμένου χρήστη 
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0) return Unauthorized(new { message = "Μη έγκυρος χρήστης." });

            var cart = await _cartService.GetCartByCustomerAsync(customerId);
            return Ok(cart);
        }

        // 2. POST: api/carts/items -> Προσθήκη στο καλάθι του συνδεδεμένου χρήστη
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0) return Unauthorized(new { message = "Μη έγκυρος χρήστης." });

            if (dto.Quantity <= 0)
            {
                return BadRequest(new { message = "Η ποσότητα πρέπει να είναι μεγαλύτερη από μηδέν." });
            }

            await _cartService.AddOrUpdateItemAsync(customerId, dto);
            return Ok(new { message = "Το προϊόν προστέθηκε στο καλάθι επιτυχώς!" });
        }

        // 3. DELETE: api/carts/items/{productId}
        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0) return Unauthorized(new { message = "Μη έγκυρος χρήστης." });

            await _cartService.RemoveItemAsync(customerId, productId);
            return Ok(new { message = "Το προϊόν αφαιρέθηκε από το καλάθι." });
        }

        // 4. DELETE: api/carts
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0) return Unauthorized(new { message = "Μη έγκυρος χρήστης." });

            await _cartService.ClearCartAsync(customerId);
            return Ok(new { message = "Το καλάθι αδειάστηκε επιτυχώς." });
        }

        // 5. POST: api/carts/checkout -> Ολοκλήρωση αγοράς
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0) return Unauthorized(new { message = "Μη έγκυρος χρήστης." });

            try
            {
                var orderId = await _cartService.CheckoutAsync(customerId);
                return Ok(new
                {
                    message = "Η παραγγελία ολοκληρώθηκε με επιτυχία!",
                    orderId = orderId
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 6. POST: api/carts/coupon -> Εφαρμογή κουπονιού στο καλάθι
        [HttpPost("coupon")]
        public async Task<IActionResult> ApplyCoupon([FromQuery] string couponCode)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0) return Unauthorized(new { message = "Μη έγκυρος χρήστης." });

            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return BadRequest(new { message = "Ο κωδικός κουπονιού δεν μπορεί να είναι κενός." });
            }

            try
            {
                await _cartService.ApplyCouponAsync(customerId, couponCode);
                return Ok(new { message = $"Το κουπόνι '{couponCode}' καταχωρήθηκε στο καλάθι επιτυχώς!" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}