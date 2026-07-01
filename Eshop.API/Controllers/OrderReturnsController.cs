using Eshop.Application.DTOs;
using Eshop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Authorize] // Όλος ο controller χρειάζεται login!
    public class OrderReturnsController : ControllerBase
    {
        private readonly IOrderReturnService _returnService;

        public OrderReturnsController(IOrderReturnService returnService)
        {
            _returnService = returnService;
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

        // ENDPOINTS ΓΙΑ ΤΟΝ ΔΙΑΧΕΙΡΙΣΤΗ (Admin)
        [HttpGet("api/admin/returns")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAllReturns()
        {
            var result = await _returnService.GetAllReturnsAsync();
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
        [Authorize(Roles = "Administrator")] 
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