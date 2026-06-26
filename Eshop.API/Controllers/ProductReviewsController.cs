using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Eshop.Application.DTOs;
using Eshop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/reviews")]
    public class ProductReviewsController : ControllerBase
    {
        private readonly IProductReviewService _reviewService;

        // Κάνουμε Inject το Service που φτιάξαμε στο Βήμα 6
        public ProductReviewsController(IProductReviewService reviewService)
        {
            _reviewService = reviewService;
        }
        
        // Λήψη όλων των κριτικών για ένα προϊόν μαζί με τα στατιστικά (Μέσο Όρο & Πλήθος).
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductReviewContainerDto))]
        public async Task<IActionResult> GetReviews(int productId)
        {
            var result = await _reviewService.GetProductReviewsAsync(productId);
            return Ok(result);
        }

        // Υποβολή νέας κριτικής από συνδεδεμένο χρήστη (Verified Buyer).
        [HttpPost]
        [Authorize] // Κλειδώνει το endpoint: Μόνο για συνδεδεμένους χρήστες!
        public async Task<IActionResult> CreateReview(int productId, [FromBody] CreateReviewDto dto)
        {
            // 1. Διαβάζουμε το CustomerId με ασφάλεια μέσα από τα Claims του JWT Token
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;

            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            try
            {
                // 2. Καλούμε το Service για να κάνει τον έλεγχο αγοράς και την αποθήκευση
                var isSaved = await _reviewService.AddReviewAsync(productId, customerId, dto);

                if (isSaved)
                {
                    return Ok(new { message = "Η κριτική σας υποβλήθηκε με επιτυχία!" });
                }

                return BadRequest("Κάτι πήγε λάθος κατά την αποθήκευση της κριτικής.");
            }
            catch (InvalidOperationException ex)
            {
                // Πιάνουμε το exception αν ο χρήστης ΔΕΝ έχει αγοράσει το προϊόν
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}