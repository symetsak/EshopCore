using Eshop.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Eshop.Application.Services
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepo;

        public CouponService(ICouponRepository couponRepo)
        {
            _couponRepo = couponRepo;
        }

        public async Task<decimal> CalculateDiscountAsync(string code, decimal currentSubTotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return 0;

            var coupon = await _couponRepo.GetByCodeAsync(code);

            // 1. Έλεγχος ύπαρξης και ενεργοποίησης
            if (coupon == null || !coupon.IsActive) return 0;

            // 2. Έλεγχος ημερομηνιών λήξης
            var now = DateTime.UtcNow;
            if (now < coupon.StartDate || now > coupon.EndDate) return 0;

            // 3. Έλεγχος ελάχιστου ορίου αγορών
            if (currentSubTotal < coupon.MinimumSubTotalRequired) return 0;

            // 4. Υπολογισμός Έκπτωσης βάσει τύπου
            if (coupon.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
            {
                // Ποσοστιαία έκπτωση (π.χ. SubTotal = 100€, Value = 20 -> Έκπτωση 20€)
                return currentSubTotal * (coupon.DiscountValue / 100);
            }
            else if (coupon.DiscountType.Equals("FixedAmount", StringComparison.OrdinalIgnoreCase))
            {
                // Σταθερό ποσό έκπτωσης (π.χ. Value = 10€). 
                // Προστασία: Η έκπτωση δεν μπορεί να είναι μεγαλύτερη από το ίδιο το subtotal!
                return Math.Min(coupon.DiscountValue, currentSubTotal);
            }

            return 0;
        }
    }
}