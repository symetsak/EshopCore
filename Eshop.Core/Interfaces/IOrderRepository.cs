using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Order>> GetAllOrdersAsync(); // Για τον Admin του Tenant
        Task AddAsync(Order order);
        Task SaveChangesAsync();
    }
}