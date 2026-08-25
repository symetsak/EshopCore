using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eshop.Core.Interfaces
{
    public interface IOrderReturnRepository
    {
        Task<OrderReturn?> GetByIdWithItemsAsync(int id);
        Task<IEnumerable<OrderReturn>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<OrderReturn>> GetAllReturnsAsync();
        Task AddAsync(OrderReturn orderReturn);
        void Update(OrderReturn orderReturn);
        Task SaveChangesAsync();
        Task<PagedResultDto<OrderReturn>> GetPagedReturnsAsync(OrderReturnFilterDto filter);
        Task<List<OrderReturn>> GetReturnsForExportAsync(OrderReturnFilterDto filter);
    }
}