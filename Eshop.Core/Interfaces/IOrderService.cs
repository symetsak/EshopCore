using Eshop.Core.DTOs;

namespace Eshop.Core.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(int customerId, OrderCreateDto dto);
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<IEnumerable<OrderResponseDto>> GetCustomerOrdersAsync(int customerId);
        Task<IEnumerable<OrderResponseDto>> GetAllTenantOrdersAsync();
        Task<OrderResponseDto?> UpdateOrderStatusAsync(int orderId, OrderStatusUpdateDto dto);
        Task<AdminDashboardDto> GetAdminDashboardStatsAsync();
    }
}
