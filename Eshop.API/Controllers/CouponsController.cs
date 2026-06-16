using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponRepository _couponRepo;

        public CouponsController(ICouponRepository couponRepo)
        {
            _couponRepo = couponRepo;
        }

        // 1. POST: api/coupons
        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new { message = "Ο κωδικός του κουπονιού είναι υποχρεωτικός." });
            }

            // Χειροκίνητο, αυστηρό Parsing των ημερομηνιών (Format: YYYY-MM-DD)
            if (!DateTime.TryParseExact(dto.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStartDate) ||
                !DateTime.TryParseExact(dto.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEndDate))
            {
                return BadRequest(new { message = "Μη έγκυρη μορφή ημερομηνίας. Χρησιμοποιήστε αυστηρά το format YYYY-MM-DD (π.χ. 2026-06-15)." });
            }

            if (parsedStartDate >= parsedEndDate)
            {
                return BadRequest(new { message = "Η ημερομηνία έναρξης πρέπει να είναι προγενέστερη της ημερομηνίας λήξης." });
            }

            var existing = await _couponRepo.GetByCodeAsync(dto.Code);
            if (existing != null)
            {
                return BadRequest(new { message = $"Υπάρχει ήδη καταχωρημένο κουπόνι με τον κωδικό '{dto.Code}'." });
            }

            // Φτιάχνουμε το Entity από το DTO
            var coupon = new Coupon
            {
                Code = dto.Code.ToUpper(),
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinimumSubTotalRequired = dto.MinimumSubTotalRequired,
                StartDate = DateTime.SpecifyKind(parsedStartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(parsedEndDate, DateTimeKind.Utc),
                IsActive = dto.IsActive
            };

            await _couponRepo.AddAsync(coupon);
            await _couponRepo.SaveChangesAsync();

            return Ok(new { message = "Το κουπόνι δημιουργήθηκε με επιτυχία!", coupon });
        }

        // 2. GET: api/coupons/{code} -> Έλεγχος/Εμφάνιση ενός κουπονιού βάσει κωδικού
        [HttpGet("{code}")]
        public async Task<IActionResult> GetCouponByCode(string code)
        {
            var coupon = await _couponRepo.GetByCodeAsync(code);
            if (coupon == null)
            {
                return NotFound(new { message = $"Το κουπόνι '{code}' δεν βρέθηκε." });
            }

            return Ok(coupon);
        }
    }
}