using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using BCrypt.Net;
using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Eshop.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public CustomerService(ICustomerRepository customerRepo, IMapper mapper, IConfiguration configuration)
        {
            _customerRepo = customerRepo;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<CustomerAuthResponseDto> RegisterAsync(CustomerRegisterDto dto)
        {
            var existingCustomer = await _customerRepo.GetByEmailAsync(dto.Email);
            if (existingCustomer != null)
            {
                throw new InvalidOperationException("Το email χρησιμοποιείται ήδη.");
            }

            var customer = _mapper.Map<Customer>(dto);
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Παραγωγή Refresh Token για 24 ώρες
            customer.RefreshToken = GenerateRefreshTokenString();
            customer.RefreshTokenExpiry = DateTime.UtcNow.AddHours(24);

            await _customerRepo.AddAsync(customer);
            await _customerRepo.SaveChangesAsync();

            var token = GenerateCustomerToken(customer);
            var response = _mapper.Map<CustomerAuthResponseDto>(customer);
            response.Token = token;

            return response;
        }

        public async Task<CustomerAuthResponseDto?> LoginAsync(CustomerLoginRequestDto dto)
        {
            var customer = await _customerRepo.GetByEmailAsync(dto.Email);
            if (customer == null || !BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash))
            {
                return null;
            }

            // Ανανέωση του Refresh Token κατά το Login (24 ώρες)
            customer.RefreshToken = GenerateRefreshTokenString();
            customer.RefreshTokenExpiry = DateTime.UtcNow.AddHours(24);
            await _customerRepo.SaveChangesAsync();

            var token = GenerateCustomerToken(customer);
            var response = _mapper.Map<CustomerAuthResponseDto>(customer);
            response.Token = token;

            return response;
        }

        public async Task<CustomerAuthResponseDto?> RefreshTokenAsync(CustomerRefreshRequestDto dto)
        {
            // Εδώ χρειαζόμαστε μια μέθοδο στο Repo ή ένα query. Για multi-tenancy, επειδή ψάχνουμε 
            // μέσα στη βάση του συγκεκριμένου tenant, θα βρούμε τον customer με αυτό το Refresh Token.
            // (Θα προσθέσουμε τη μέθοδο στο Repository αμέσως μετά!)

            // Προσωρινά, ας υποθέσουμε ότι το Repo μας έχει τη μέθοδο GetByRefreshTokenAsync
            var customer = await _customerRepo.GetByRefreshTokenAsync(dto.RefreshToken);

            if (customer == null || customer.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return null; // Το token δεν υπάρχει ή έληξε!
            }

            // Αν όλα είναι οκ, παράγουμε νέα tokens (Rotation για ασφάλεια!)
            customer.RefreshToken = GenerateRefreshTokenString();
            customer.RefreshTokenExpiry = DateTime.UtcNow.AddHours(24);
            await _customerRepo.SaveChangesAsync();

            var newJwtToken = GenerateCustomerToken(customer);
            var response = _mapper.Map<CustomerAuthResponseDto>(customer);
            response.Token = newJwtToken;

            return response;
        }

        private string GenerateCustomerToken(Customer customer)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, customer.Email),
                new Claim(ClaimTypes.Role, "Customer"),
                new Claim("CustomerId", customer.Id.ToString())
            };

            var jwtSecret = _configuration["JwtSettings:Secret"] ?? "$uper$ecureL0ngKeyCh@ngeMe!WhyS0L0ngMu$tBeThi$Key";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[62];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}