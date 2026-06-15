using Eshop.Core.Entities;
using System.Threading.Tasks;

namespace Eshop.Core.Interfaces
{
    public interface ICouponRepository
    {
        Task<Coupon?> GetByCodeAsync(string code);
        Task AddAsync(Coupon coupon); // Χρήσιμο για να μπορούμε να βάζουμε κουπόνια
        Task SaveChangesAsync();
    }
}