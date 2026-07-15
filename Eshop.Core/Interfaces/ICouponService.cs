using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using System.Threading.Tasks;

namespace Eshop.Core.Interfaces
{
    public interface ICouponService
    {
        // Ελέγχει αν το κουπόνι ισχύει και επιστρέφει το ποσό της έκπτωσης που αναλογεί
        Task<decimal> CalculateDiscountAsync(string code, decimal currentSubTotal);

        Task<IEnumerable<Coupon>> GetAllCouponsAsync();
        Task<Coupon?> GetCouponByCodeAsync(string code);
        Task<Coupon> CreateCouponAsync(CreateCouponDto dto);
        Task<Coupon> UpdateCouponAsync(int id, CreateCouponDto dto);
        Task DeleteCouponAsync(int id);
    }
}