using Eshop.Core.DTOs;

namespace Eshop.Core.Interfaces
{
    public interface IUserService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto, string tenantId);
        Task<LoginResponseDto?> RefreshAsync(RefreshTokenRequestDto dto, string tenantId);
        Task<bool> LogoutAsync(string refreshToken);
    }
}