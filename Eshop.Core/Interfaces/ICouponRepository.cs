using Eshop.Core.Entities;
using System.Threading.Tasks;

namespace Eshop.Core.Interfaces
{
    public interface ICouponRepository
    {
        Task<Coupon?> GetByCodeAsync(string code);
        Task<Coupon?> GetByIdAsync(int id);
        Task<IEnumerable<Coupon>> GetAllAsync();
        Task AddAsync(Coupon coupon); // Χρήσιμο για να μπορούμε να βάζουμε κουπόνια
        Task UpdateAsync(Coupon coupon);
        Task DeleteAsync(Coupon coupon);
        Task SaveChangesAsync();
    }
}