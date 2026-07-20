using AutoMapper;
using BCrypt.Net;
using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Eshop.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepo, IMapper mapper, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto, string tenantId)
        {
            // 1. Αναζήτηση του χρήστη βάσει Username
            var user = await _userRepo.GetByUsernameAsync(dto.Username);
            if (user == null) return null;

            // 2. ΕΛΕΓΧΟΣ PASSWORD ΑΠΟΚΛΕΙΣΤΙΚΑ ΜΕ BCRYPT (Τέρμα τα hardcoded passwords!)
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!isPasswordValid) return null;

            // 3. Παραγωγή του JWT Token
            var accessToken = GenerateAccessToken(user, tenantId);

            // 4. Παραγωγή & Αποθήκευση Refresh Token
            var refreshTokenEntity = GenerateRefreshTokenEntity(user.Id);
            await _userRepo.AddRefreshTokenAsync(refreshTokenEntity);
            await _userRepo.SaveChangesAsync();

            // 5. Response
            var response = _mapper.Map<LoginResponseDto>(user);
            response.Token = accessToken;
            response.RefreshToken = refreshTokenEntity.Token;

            return response;
        }

        public async Task<LoginResponseDto?> RefreshAsync(RefreshTokenRequestDto dto, string tenantId)
        {
            // 1. Αναζήτηση του Refresh Token μαζί με τον User
            var storedToken = await _userRepo.GetRefreshTokenWithUserAsync(dto.RefreshToken);
            if (storedToken == null) return null;

            // 2. Έλεγχος αν έληξε
            if (storedToken.IsExpired)
            {
                _userRepo.RemoveRefreshToken(storedToken);
                await _userRepo.SaveChangesAsync();
                throw new InvalidOperationException("Το Refresh Token έχει λήξει. Παρακαλώ συνδεθείτε ξανά.");
            }

            var user = storedToken.User;
            if (user == null) return null;

            // 3. Δημιουργία νέου Access Token
            var newAccessToken = GenerateAccessToken(user, tenantId);

            // 4. Refresh Token Rotation
            _userRepo.RemoveRefreshToken(storedToken);
            var newRefreshTokenEntity = GenerateRefreshTokenEntity(user.Id);
            await _userRepo.AddRefreshTokenAsync(newRefreshTokenEntity);
            await _userRepo.SaveChangesAsync();

            // 5. Response
            var response = _mapper.Map<LoginResponseDto>(user);
            response.Token = newAccessToken;
            response.RefreshToken = newRefreshTokenEntity.Token;

            return response;
        }

        private string GenerateAccessToken(User user, string tenantId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("TenantId", tenantId),
                new Claim("UserId", user.Id.ToString())
            };

            var jwtSecret = _configuration["JwtSettings:Secret"] ?? "$uper$ecureL0ngKeyCh@ngeMe!WhyS0L0ngMu$tBeThi$Key";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(20),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshTokenEntity(int userId)
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            string refreshTokenString = Convert.ToBase64String(randomNumber);

            return new RefreshToken
            {
                Token = refreshTokenString,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddHours(12),
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            // 1. Αναζήτηση του Refresh Token στη βάση μέσω του έτοιμου Repository σου
            var storedToken = await _userRepo.GetRefreshTokenWithUserAsync(refreshToken);

            // Αν δεν βρεθεί (π.χ. έχει ήδη διαγραφεί ή είναι άκυρο), επιστρέφουμε false
            if (storedToken == null) return false;

            // 2. Διαγραφή του token από τη βάση
            _userRepo.RemoveRefreshToken(storedToken);
            await _userRepo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequestDto dto, string tenantId)
        {
            // 1. Καλούμε το Repository για να κάνει τη βαριά δουλειά στη μνήμη
            // (Έλεγχος χρήστη, Verify Password με BCrypt, και Hash-άρισμα του νέου κωδικού)
            bool isPrepared = await _userRepo.ChangePasswordAsync(dto.Username, dto.CurrentPassword, dto.NewPassword, tenantId);

            if (!isPrepared) return false;

            // 2. Αφού το Repository έκανε σωστά το update στο User Entity, 
            // σώζουμε τις αλλαγές στη βάση δεδομένων του συγκεκριμένου Tenant!
            await _userRepo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CreateUserAsync(CreateUserDto dto)
        {
            // Έλεγχος αν υπάρχει ήδη το username
            var existingUser = await _userRepo.GetByUsernameAsync(dto.Username);
            if (existingUser != null) return false;

            var newUser = new User
            {
                Username = dto.Username,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role,
                IsFirstLogin = true, // Οι χρήστες που φτιάχνει ο admin δεν χρειάζονται force reset
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Welcome123!"), // Κρυπτογράφηση
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddUserAsync(newUser);
            await _userRepo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return false;

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrEmpty(dto.Role)) user.Role = dto.Role;

            _userRepo.UpdateUser(user);
            await _userRepo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return false;

            _userRepo.DeleteUser(user);
            await _userRepo.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepo.GetAllUsersWithNotesAsync();
            var dtoList = new List<UserResponseDto>();

            foreach (var u in users)
            {
                var dto = _mapper.Map<UserResponseDto>(u);

                // Βρίσκουμε την πιο πρόσφατη σημείωση (αν υπάρχει)
                var latestNote = u.Notes.OrderByDescending(n => n.CreatedAt).FirstOrDefault();

                if (latestNote != null)
                {
                    dto.LatestNote = latestNote.Content;
                    dto.LatestNoteCreatedAt = latestNote.CreatedAt; 
                }

                dtoList.Add(dto);
            }

            return dtoList;
        }

        public async Task<IEnumerable<UserNoteDto>> GetUserNotesAsync(int userId)
        {
            var notes = await _userRepo.GetUserNotesByUserIdAsync(userId);

            // Χειροκίνητο mapping (ή μπορείς να βάλεις το AutoMapper αν προτιμάς)
            return notes.Select(n => new UserNoteDto
            {
                Id = n.Id,
                Content = n.Content,
                CreatedAt = n.CreatedAt,
                CreatedBy = n.CreatedBy
            }).ToList();
        }

        public async Task<bool> AddUserNoteAsync(int userId, string content, string createdBy)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            var note = new UserNote
            {
                UserId = userId,
                Content = content,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddUserNoteAsync(note);
            await _userRepo.SaveChangesAsync();

            return true;
        }
    }
}