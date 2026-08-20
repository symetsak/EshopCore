using Eshop.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Core.Interfaces
{
    public interface IMasterDbContext
    {
        DbSet<SuperAdmin> SuperAdmins { get; set; }
        DbSet<Tenant> Tenants { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}