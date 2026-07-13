using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username) => await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        public async Task<User?> GetByIdAsync(int id) => await _context.Users.FindAsync(id);

        public async Task<RefreshToken?> GetRefreshTokenWithUserAsync(string refreshToken) => await _context.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        public async Task AddRefreshTokenAsync(RefreshToken token) => await _context.RefreshTokens.AddAsync(token);

        public void RemoveRefreshToken(RefreshToken token) => _context.RefreshTokens.Remove(token);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task AddUserAsync(User user) => await _context.Users.AddAsync(user);
        public void UpdateUser(User user) => _context.Users.Update(user);
        public async Task<IEnumerable<User>> GetAllUsersAsync() => await _context.Users.ToListAsync();

        public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword, string tenantId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null) return false;

            // Έλεγχος ΜΟΝΟ με BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);

            if (!isPasswordValid) return false;

            // Αποθήκευση του νέου Hash
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Σβήνουμε το flag του πρώτου login!
            user.IsFirstLogin = false;

            return true;
        }
    }
}