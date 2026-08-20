using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        private readonly MasterDbContext _context;

        // Κάνουμε inject το DbContext της Infrastructure
        public TenantRepository(MasterDbContext context)
        {
            _context = context;
        }

        public async Task<Tenant?> GetByIdAsync(string id)
        {
            return await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Tenant>> GetAllAsync()
        {
            return await _context.Tenants.ToListAsync();
        }

        public async Task AddAsync(Tenant tenant)
        {
            await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.Tenants.AnyAsync(t => t.Id == id);
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<IEnumerable<TenantTransaction>> GetTransactionsByTenantIdAsync(string tenantId)
        {
            return await _context.TenantTransactions
                .Where(t => t.TenantId == tenantId)
                .OrderByDescending(t => t.CreatedAt) 
                .ToListAsync();
        }

        public async Task AddTransactionAsync(TenantTransaction transaction)
        {
            await _context.TenantTransactions.AddAsync(transaction);
        }

        public async Task<TenantTransaction?> GetTransactionByIdAsync(int id)
        {
            return await _context.TenantTransactions.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task DeleteTransactionAsync(TenantTransaction transaction)
        {
            _context.TenantTransactions.Remove(transaction);
        }
    }
}