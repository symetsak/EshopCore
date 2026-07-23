using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator, Employee")]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;

        // Κάνουμε inject πλέον το Service, ΟΧΙ το Repository!
        public CouponsController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new { message = "Ο κωδικός του κουπονιού είναι υποχρεωτικός." });
            }

            try
            {
                var coupon = await _couponService.CreateCouponAsync(dto);
                return Ok(new { message = "Το κουπόνι δημιουργήθηκε με επιτυχία!", coupon });
            }
            catch (ArgumentException ex) // Πιάνει τα λάθη ημερομηνιών
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) // Πιάνει τα διπλότυπα κουπόνια
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetCouponByCode(string code)
        {
            var coupon = await _couponService.GetCouponByCodeAsync(code);
            if (coupon == null)
            {
                return NotFound(new { message = $"Το κουπόνι '{code}' δεν βρέθηκε." });
            }

            return Ok(coupon);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCoupons()
        {
            var coupons = await _couponService.GetAllCouponsAsync();
            return Ok(coupons);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] CreateCouponDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new { message = "Ο κωδικός του κουπονιού είναι υποχρεωτικός." });
            }

            try
            {
                var coupon = await _couponService.UpdateCouponAsync(id, dto);
                return Ok(new { message = "Το κουπόνι ενημερώθηκε με επιτυχία!", coupon });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            try
            {
                await _couponService.DeleteCouponAsync(id);
                return Ok(new { message = "Το κουπόνι διαγράφηκε με επιτυχία." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}