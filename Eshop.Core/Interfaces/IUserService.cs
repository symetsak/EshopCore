using Eshop.Core.DTOs;

namespace Eshop.Core.Interfaces
{
    public interface IUserService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto, string tenantId);
        Task<LoginResponseDto?> RefreshAsync(RefreshTokenRequestDto dto, string tenantId);
        Task<bool> LogoutAsync(string refreshToken);
        Task<bool> ChangePasswordAsync(ChangePasswordRequestDto dto, string tenantId);
        Task<bool> CreateUserAsync(CreateUserDto dto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<IEnumerable<UserNoteDto>> GetUserNotesAsync(int userId);
        Task<bool> AddUserNoteAsync(int userId, string content, string createdBy);
    }
}