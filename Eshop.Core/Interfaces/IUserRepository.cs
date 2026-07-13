using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(int id);
        Task<RefreshToken?> GetRefreshTokenWithUserAsync(string refreshToken);
        Task AddRefreshTokenAsync(RefreshToken token);
        void RemoveRefreshToken(RefreshToken token);
        Task SaveChangesAsync();
        Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword, string tenantId);
        Task AddUserAsync(User user);
        void UpdateUser(User user);
        Task<IEnumerable<User>> GetAllUsersAsync();
    }
}