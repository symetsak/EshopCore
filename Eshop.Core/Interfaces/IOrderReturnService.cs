using Eshop.Application.DTOs;

namespace Eshop.Application.Services
{
    public interface IOrderReturnService
    {
        // Για τον πελάτη
        Task<OrderReturnResponseDto> CreateReturnRequestAsync(int customerId, OrderReturnRequestDto dto);
        Task<IEnumerable<OrderReturnResponseDto>> GetCustomerReturnsAsync(int customerId);

        // Για τον Admin
        Task<IEnumerable<OrderReturnResponseDto>> GetAllReturnsAsync();
        Task<OrderReturnResponseDto?> GetReturnByIdAsync(int id);
        Task<OrderReturnResponseDto?> UpdateReturnStatusAsync(int returnId, OrderReturnStatusUpdateDto dto);
    }
}