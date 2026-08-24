using Eshop.Application.DTOs;
using Eshop.Core.DTOs; // ΠΡΟΣΘΗΚΗ: Για το PagedResultDto και OrderReturnFilterDto

namespace Eshop.Application.Services
{
    public interface IOrderReturnService
    {
        // Για τον πελάτη
        Task<OrderReturnResponseDto> CreateReturnRequestAsync(int customerId, OrderReturnRequestDto dto);
        Task<IEnumerable<OrderReturnResponseDto>> GetCustomerReturnsAsync(int customerId);

        // Για τον Admin
        Task<PagedResultDto<OrderReturnResponseDto>> GetFilteredReturnsAsync(OrderReturnFilterDto filter); // REFACTOR: Αντικατέστησε το GetAllReturnsAsync
        Task<OrderReturnResponseDto?> GetReturnByIdAsync(int id);
        Task<OrderReturnResponseDto?> UpdateReturnStatusAsync(int returnId, OrderReturnStatusUpdateDto dto);
    }
}