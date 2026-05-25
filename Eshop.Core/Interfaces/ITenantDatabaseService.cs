namespace Eshop.Core.Interfaces
{
    public interface ITenantDatabaseService
    {
        Task CreateTenantDatabaseAsync(string connectionString);
    }
}