using System.Security.Claims;
using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")] // Μόνο συνδεδεμένοι πελάτες μπορούν να ψωνίσουν!
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // POST: api/orders
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            // 1. Τραβάμε το CustomerId μέσα από τα Claims του JWT Token
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized(new { message = "Μη έγκυρο αναγνωριστικό πελάτη στο token." });
            }

            try
            {
                // 2. Εκτέλεση της παραγγελίας
                var response = await _orderService.CreateOrderAsync(customerId, dto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // Αν κοπεί λόγω Stock ή άδειου καλαθιού
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}