using Eshop.Application.DTOs;
using Eshop.Core.DTOs; 

namespace Eshop.Application.Services
{
    public interface IOrderReturnService
    {
        // Για τον πελάτη
        Task<OrderReturnResponseDto> CreateReturnRequestAsync(int customerId, OrderReturnRequestDto dto);
        Task<IEnumerable<OrderReturnResponseDto>> GetCustomerReturnsAsync(int customerId);

        // Για τον Admin
        Task<PagedResultDto<OrderReturnResponseDto>> GetFilteredReturnsAsync(OrderReturnFilterDto filter); 
        Task<OrderReturnResponseDto?> GetReturnByIdAsync(int id);
        Task<OrderReturnResponseDto?> UpdateReturnStatusAsync(int returnId, OrderReturnStatusUpdateDto dto);
        Task<IEnumerable<OrderReturnResponseDto>> GetReturnsForExportAsync(OrderReturnFilterDto filter);
    }
}