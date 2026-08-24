using Eshop.Application.DTOs;
using Eshop.Application.Services;
using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Authorize]
    public class OrderReturnsController : ControllerBase
    {
        private readonly IOrderReturnService _returnService;
        private readonly IOrderService _orderService;

        public OrderReturnsController(IOrderReturnService returnService, IOrderService orderService)
        {
            _returnService = returnService;
            _orderService = orderService;
        }

        // ENDPOINTS ΓΙΑ ΤΟΝ ΠΕΛΑΤΗ (Customer)
        [HttpPost("api/returns")]
        public async Task<IActionResult> CreateReturnRequest([FromBody] OrderReturnRequestDto dto)
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            try
            {
                var result = await _returnService.CreateReturnRequestAsync(customerId, dto);
                return CreatedAtAction(nameof(GetReturnById), new { id = result.Id }, result);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("api/returns/my-returns")]
        public async Task<IActionResult> GetCustomerReturns()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            var result = await _returnService.GetCustomerReturnsAsync(customerId);
            return Ok(result);
        }


        // Υποβολή αιτήματος ακύρωσης από τον πελάτη (Μόνο για παραγγελίες Pending ή Paid).   
        [HttpPut("api/returns/orders/{orderId}/request-cancel")]
        public async Task<IActionResult> RequestOrderCancellation(int orderId)
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized("Μη έγκυρος χρήστης.");
            }

            // 1. Φέρνουμε την παραγγελία για να δούμε αν υπάρχει και αν ανήκει στον πελάτη
            var orderDto = await _orderService.GetOrderByIdAsync(orderId);
            if (orderDto == null || orderDto.CustomerId != customerId)
            {
                return NotFound("Η παραγγελία δεν βρέθηκε ή δεν σας ανήκει.");
            }

            // 2. Έλεγχος: Ο πελάτης μπορεί να ζητήσει ακύρωση ΜΟΝΟ αν είναι Pending ή Paid
            if (!orderDto.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) && !orderDto.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = $"Δεν μπορείτε να ακυρώσετε την παραγγελία, καθώς βρίσκεται ήδη σε κατάσταση '{orderDto.Status}' (έχει δρομολογηθεί). Παρακαλούμε περιμένετε να παραλάβετε και ξεκινήστε διαδικασία επιστροφής." });
            }

            // 3. Θέτουμε την κατάσταση σε 'CancellationRequested' μέσω του Service
            var updateDto = new OrderStatusUpdateDto { Status = "CancellationRequested" };
            var result = await _orderService.UpdateOrderStatusAsync(orderId, updateDto);

            return Ok(result);
        }

        // ENDPOINTS ΓΙΑ ΤΟΝ ΔΙΑΧΕΙΡΙΣΤΗ (Admin)
        [HttpGet("api/admin/returns")]
        [Authorize(Roles = "Administrator, Employee")]
        public async Task<IActionResult> GetAllReturns([FromQuery] OrderReturnFilterDto filter) // REFACTOR: Προστέθηκαν τα φίλτρα
        {
            var result = await _returnService.GetFilteredReturnsAsync(filter);
            return Ok(result);
        }

        [HttpGet("api/admin/returns/{id}")]
        public async Task<IActionResult> GetReturnById(int id)
        {
            var result = await _returnService.GetReturnByIdAsync(id);
            if (result == null) return NotFound("Η αίτηση επιστροφής δεν βρέθηκε.");
            return Ok(result);
        }


        [HttpPut("api/admin/returns/{id}/status")]
        [Authorize(Roles = "Administrator, Employee")]
        public async Task<IActionResult> UpdateReturnStatus(int id, [FromBody] OrderReturnStatusUpdateDto dto)
        {
            try
            {
                var result = await _returnService.UpdateReturnStatusAsync(id, dto);
                if (result == null) return NotFound("Η αίτηση επιστροφής δεν βρέθηκε.");
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}