using System.Security.Claims;
using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // POST: api/orders
        [HttpPost]
        [Authorize(Roles = "Customer")]
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

        // GET: api/orders/my-orders
        [HttpGet("my-orders")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized(new { message = "Μη έγκυρο αναγνωριστικό πελάτη στο token." });
            }

            var orders = await _orderService.GetCustomerOrdersAsync(customerId);
            return Ok(orders);
        }

        // GET: api/orders/admin/all
        [HttpGet("admin/all")]
        [Authorize(Roles = "Administrator")] // Μόνο ο Admin του καταστήματος προς το παρόν
        public async Task<IActionResult> GetAllTenantOrders()
        {
            var orders = await _orderService.GetAllTenantOrdersAsync();
            return Ok(orders);
        }

        // GET: api/Orders/admin/{id}
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetOrderDetailsForAdmin(int id)
        {
            var order = await _orderService.GetOrderDetailsForAdminAsync(id);

            if (order == null)
            {
                return NotFound(new { message = $"Η παραγγελία με ID {id} δεν βρέθηκε." });
            }

            return Ok(order);
        }

        // PUT: api/orders/admin/{id}/status
        [HttpPut("admin/{id}/status")]
        [Authorize(Roles = "Administrator")] // Μόνο ο Admin μπορεί να αλλάξει status
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatusUpdateDto dto)
        {
            if (string.IsNullOrEmpty(dto.Status))
            {
                return BadRequest(new { message = "Το πεδίο Status είναι υποχρεωτικό." });
            }

            var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, dto);

            if (updatedOrder == null)
            {
                return NotFound(new { message = $"Η παραγγελία με ID {id} δεν βρέθηκε." });
            }

            return Ok(updatedOrder);
        }

        // GET: api/orders/admin/dashboard
        [HttpGet("admin/dashboard")]
        [Authorize(Roles = "Administrator")] // Μόνο ο Admin βλέπει τζίρους!
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _orderService.GetAdminDashboardStatsAsync();
            return Ok(stats);
        }
    }
}