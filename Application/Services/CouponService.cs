using Eshop.Core.Interfaces;
using Eshop.Core.Entities;
using Eshop.Core.DTOs;
using System.Globalization;


namespace Eshop.Application.Services
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepo;

        public CouponService(ICouponRepository couponRepo)
        {
            _couponRepo = couponRepo;
        }

        // Η υπάρχουσα μέθοδος σου παραμένει ίδια:
        public async Task<decimal> CalculateDiscountAsync(string code, decimal currentSubTotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return 0;
            var coupon = await _couponRepo.GetByCodeAsync(code);
            if (coupon == null || !coupon.IsActive) return 0;
            var now = DateTime.UtcNow;
            if (now < coupon.StartDate || now > coupon.EndDate) return 0;
            if (currentSubTotal < coupon.MinimumSubTotalRequired) return 0;

            if (coupon.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
                return currentSubTotal * (coupon.DiscountValue / 100);
            else if (coupon.DiscountType.Equals("FixedAmount", StringComparison.OrdinalIgnoreCase))
                return Math.Min(coupon.DiscountValue, currentSubTotal);

            return 0;
        }

        public async Task<IEnumerable<Coupon>> GetAllCouponsAsync()
        {
            return await _couponRepo.GetAllAsync();
        }

        public async Task<Coupon?> GetCouponByCodeAsync(string code)
        {
            return await _couponRepo.GetByCodeAsync(code);
        }

        public async Task<Coupon> CreateCouponAsync(CreateCouponDto dto)
        {
            // Business Logic Validations
            if (!DateTime.TryParseExact(dto.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStartDate) ||
                !DateTime.TryParseExact(dto.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEndDate))
            {
                throw new ArgumentException("Μη έγκυρη μορφή ημερομηνίας. Χρησιμοποιήστε αυστηρά το format YYYY-MM-DD.");
            }

            if (parsedStartDate >= parsedEndDate)
            {
                throw new ArgumentException("Η ημερομηνία έναρξης πρέπει να είναι προγενέστερη της ημερομηνίας λήξης.");
            }

            var existing = await _couponRepo.GetByCodeAsync(dto.Code);
            if (existing != null)
            {
                throw new InvalidOperationException($"Υπάρχει ήδη καταχωρημένο κουπόνι με τον κωδικό '{dto.Code}'.");
            }

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

            return coupon;
        }

        public async Task<Coupon> UpdateCouponAsync(int id, CreateCouponDto dto)
        {
            var coupon = await _couponRepo.GetByIdAsync(id);
            if (coupon == null)
            {
                throw new KeyNotFoundException("Το κουπόνι δεν βρέθηκε.");
            }

            if (!DateTime.TryParseExact(dto.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStartDate) ||
                !DateTime.TryParseExact(dto.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEndDate))
            {
                throw new ArgumentException("Μη έγκυρη μορφή ημερομηνίας. Χρησιμοποιήστε αυστηρά το format YYYY-MM-DD.");
            }

            if (parsedStartDate >= parsedEndDate)
            {
                throw new ArgumentException("Η ημερομηνία έναρξης πρέπει να είναι προγενέστερη της ημερομηνίας λήξης.");
            }

            // Αν ο Admin άλλαξε τον κωδικό, ελέγχουμε μήπως υπάρχει ήδη άλλος με το νέο όνομα
            if (!string.Equals(coupon.Code, dto.Code, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _couponRepo.GetByCodeAsync(dto.Code);
                if (existing != null)
                {
                    throw new InvalidOperationException($"Υπάρχει ήδη καταχωρημένο κουπόνι με τον κωδικό '{dto.Code}'.");
                }
            }

            coupon.Code = dto.Code.ToUpper();
            coupon.DiscountType = dto.DiscountType;
            coupon.DiscountValue = dto.DiscountValue;
            coupon.MinimumSubTotalRequired = dto.MinimumSubTotalRequired;
            coupon.StartDate = DateTime.SpecifyKind(parsedStartDate, DateTimeKind.Utc);
            coupon.EndDate = DateTime.SpecifyKind(parsedEndDate, DateTimeKind.Utc);
            coupon.IsActive = dto.IsActive;

            await _couponRepo.UpdateAsync(coupon);
            await _couponRepo.SaveChangesAsync();

            return coupon;
        }

        public async Task DeleteCouponAsync(int id)
        {
            var coupon = await _couponRepo.GetByIdAsync(id);
            if (coupon == null)
            {
                throw new KeyNotFoundException("Το κουπόνι δεν βρέθηκε.");
            }

            await _couponRepo.DeleteAsync(coupon);
            await _couponRepo.SaveChangesAsync();
        }
    }
}