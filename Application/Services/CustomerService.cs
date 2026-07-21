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
        private readonly ITenantProvider _tenantProvider;

        public CustomerService(ICustomerRepository customerRepo, IMapper mapper, IConfiguration configuration, ITenantProvider tenantProvider)
        {
            _customerRepo = customerRepo;
            _mapper = mapper;
            _configuration = configuration;
            _tenantProvider = tenantProvider;
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
            response.RefreshToken = customer.RefreshToken;

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
            response.RefreshToken = customer.RefreshToken;

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
                new Claim("CustomerId", customer.Id.ToString()),
                new Claim("TenantId", _tenantProvider.TenantId!)
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

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            // 1. Αναζήτηση του Customer βάσει του Refresh Token
            var customer = await _customerRepo.GetByRefreshTokenAsync(refreshToken);

            // Αν δεν βρεθεί ο customer (π.χ. άκυρο ή ήδη σβησμένο token), επιστρέφουμε false
            if (customer == null) return false;

            // 2. Μηδενισμός των πεδίων του token για την ακύρωσή του (Revoke)
            customer.RefreshToken = string.Empty;
            customer.RefreshTokenExpiry = DateTime.MinValue; 

            // 3. Αποθήκευση των αλλαγών στη βάση
            await _customerRepo.SaveChangesAsync();

            return true;
        }

        public async Task<CustomerProfileDto?> GetProfileAsync(int customerId)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            return customer == null ? null : _mapper.Map<CustomerProfileDto>(customer);
        }

        public async Task<CustomerProfileDto?> UpdateProfileAsync(int customerId, CustomerUpdateProfileDto dto)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null) return null;

            // Ενημέρωση των πεδίων
            customer.FirstName = dto.FirstName;
            customer.LastName = dto.LastName;
            customer.Phone = dto.Phone;
            customer.Street = dto.Street;
            customer.StreetNumber = dto.StreetNumber;
            customer.City = dto.City;
            customer.ZipCode = dto.ZipCode;

            await _customerRepo.SaveChangesAsync();

            return _mapper.Map<CustomerProfileDto>(customer);
        }
    }
}