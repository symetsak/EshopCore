using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Eshop.Application.Services
{
    public class SystemAuthService : ISystemAuthService
    {
        // Αλλάξαμε το MasterDbContext σε IMasterDbContext
        private readonly IMasterDbContext _masterDb;
        private readonly IConfiguration _configuration;

        // Και εδώ στον constructor το ίδιο
        public SystemAuthService(IMasterDbContext masterDb, IConfiguration configuration)
        {
            _masterDb = masterDb;
            _configuration = configuration;
        }

        public string? Login(LoginRequestDto request)
        {
            var admin = _masterDb.SuperAdmins.SingleOrDefault(a => a.Username == request.Username);
            if (admin == null) return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash);
            if (!isPasswordValid) return null;

            return GenerateAccessToken(admin);
        }

        private string GenerateAccessToken(Eshop.Core.Entities.SuperAdmin admin)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, "SuperAdmin"),
                new Claim("UserId", admin.Id.ToString())
            };

            var jwtSecret = _configuration["JwtSettings:Secret"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}