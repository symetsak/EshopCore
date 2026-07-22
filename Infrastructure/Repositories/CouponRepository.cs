using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Repositories
{
    public class CouponRepository : ICouponRepository
    {
        private readonly ApplicationDbContext _context;

        public CouponRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            // Μετατρέπουμε σε Lowercase για να μην έχει θέμα αν ο χρήστης γράψει "summer20" αντί για "SUMMER20"
            return await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
        }

        public async Task AddAsync(Coupon coupon)
        {
            await _context.Coupons.AddAsync(coupon);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Coupon?> GetByIdAsync(int id)
        {
            return await _context.Coupons.FindAsync(id);
        }

        public async Task<IEnumerable<Coupon>> GetAllAsync()
        {
            return await _context.Coupons.ToListAsync();
        }

        public Task DeleteAsync(Coupon coupon)
        {
            _context.Coupons.Remove(coupon);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Coupon coupon)
        {
            _context.Coupons.Update(coupon);
            return Task.CompletedTask;
        }
    }
}