using Eshop.Core.DTOs;
using Eshop.Core.Entities;
namespace Eshop.Core.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Order>> GetAllOrdersAsync(); 
        Task AddAsync(Order order);
        Task SaveChangesAsync();
        Task<Order?> GetByIdWithItemsAsync(int id);
        Task<PagedResultDto<Order>> GetPagedOrdersAsync(OrderFilterDto filter);
    }
}