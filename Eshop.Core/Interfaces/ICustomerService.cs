using Eshop.Core.DTOs;

namespace Eshop.Core.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerAuthResponseDto> RegisterAsync(CustomerRegisterDto dto);
        Task<CustomerAuthResponseDto?> LoginAsync(CustomerLoginRequestDto dto);
        Task<CustomerAuthResponseDto?> RefreshTokenAsync(CustomerRefreshRequestDto dto);
        Task<bool> LogoutAsync(string refreshToken);
    }
}