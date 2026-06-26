using System;
using System.Threading.Tasks;
using Eshop.Application.DTOs;
using Eshop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/wishlist")]
    [Authorize] // Όλος ο Controller είναι κλειδωμένος! Μόνο για συνδεδεμένους πελάτες.
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        // Inject το WishlistService που φτιάξαμε στο Βήμα 6
        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            // Διαβάζουμε το CustomerId από το custom JWT Claim που ελέγξαμε πριν
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;

            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            var result = await _wishlistService.GetCustomerWishlistAsync(customerId);
            return Ok(result);
        }

        [HttpPost("products/{productId}/toggle")]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            // Διαβάζουμε το CustomerId από το custom JWT Claim
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;

            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            // Καλούμε το Service για να κάνει το Add ή το Remove αυτόματα
            var action = await _wishlistService.ToggleWishlistAsync(customerId, productId);

            if (action == "Added")
            {
                return Ok(new { status = "Added", message = "Το προϊόν προστέθηκε στα αγαπημένα σας!" });
            }
            else
            {
                return Ok(new { status = "Removed", message = "Το προϊόν αφαιρέθηκε από τα αγαπημένα σας!" });
            }
        }
    }
}