using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface ITenantRepository
    {
        Task<Tenant?> GetByIdAsync(string id);
        Task<IEnumerable<Tenant>> GetAllAsync();
        Task AddAsync(Tenant tenant);
        Task<bool> ExistsAsync(string id);
        Task SaveChangesAsync();
        Task<IEnumerable<TenantTransaction>> GetTransactionsByTenantIdAsync(string tenantId);
        Task AddTransactionAsync(TenantTransaction transaction);
        Task<TenantTransaction?> GetTransactionByIdAsync(int id);
        Task DeleteTransactionAsync(TenantTransaction transaction);
    }
}