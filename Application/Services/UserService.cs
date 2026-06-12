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
            // 1. Αναζήτηση του χρήστη βάσει Username μέσω του Repository
            var user = await _userRepo.GetByUsernameAsync(dto.Username);
            if (user == null) return null;

            // 2. Έλεγχος Password (με fallback για τον System Admin)
            bool isPasswordValid = false;
            if (user.Username.ToLower() == "admin" && dto.Password == "Admin123!")
            {
                isPasswordValid = true;
            }
            else
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            }

            if (!isPasswordValid) return null;

            // 3. Παραγωγή του JWT Token για τον Admin
            var accessToken = GenerateAccessToken(user, tenantId);

            // 4. Παραγωγή & Αποθήκευση Refresh Token (72 Ώρες)
            var refreshTokenEntity = GenerateRefreshTokenEntity(user.Id);
            await _userRepo.AddRefreshTokenAsync(refreshTokenEntity);
            await _userRepo.SaveChangesAsync();

            // 5. Επιστροφή Response
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
                ExpiresAt = DateTime.UtcNow.AddHours(72),
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
    }
}