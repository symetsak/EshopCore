using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByEmailAsync(string email);
        Task<Customer?> GetByRefreshTokenAsync(string refreshToken);
        Task AddAsync(Customer customer);
        Task SaveChangesAsync();
    }
}