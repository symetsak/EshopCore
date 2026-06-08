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
    }
}