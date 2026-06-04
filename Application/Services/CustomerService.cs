using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
            // 1. Έλεγχος αν υπάρχει ήδη το email
            var existingCustomer = await _customerRepo.GetByEmailAsync(dto.Email);
            if (existingCustomer != null)
            {
                throw new InvalidOperationException("Το email χρησιμοποιείται ήδη.");
            }

            // 2. Mapping και Hash του κωδικού
            var customer = _mapper.Map<Customer>(dto);
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. Αποθήκευση στη βάση
            await _customerRepo.AddAsync(customer);
            await _customerRepo.SaveChangesAsync();

            // 4. Παραγωγή Token για αυτόματη σύνδεση μετά το register
            var token = GenerateCustomerToken(customer);

            var response = _mapper.Map<CustomerAuthResponseDto>(customer);
            response.Token = token;

            return response;
        }

        public async Task<CustomerAuthResponseDto?> LoginAsync(CustomerLoginRequestDto dto)
        {
            // 1. Εύρεση πελάτη με βάση το email
            var customer = await _customerRepo.GetByEmailAsync(dto.Email);
            if (customer == null) return null;

            // 2. Έλεγχος κωδικού
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash))
            {
                return null;
            }

            // 3. Παραγωγή Token
            var token = GenerateCustomerToken(customer);

            var response = _mapper.Map<CustomerAuthResponseDto>(customer);
            response.Token = token;

            return response;
        }

        // Ιδιωτική μέθοδος για τη δημιουργία του JWT των πελατών
        private string GenerateCustomerToken(Customer customer)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, customer.Email),
                new Claim(ClaimTypes.Role, "Customer"), // <-- Όλοι οι πελάτες παίρνουν το Role: Customer
                new Claim("CustomerId", customer.Id.ToString()) // <-- Σημαντικό για τις παραγγελίες!
            };

            var jwtSecret = _configuration["JwtSettings:Secret"] ?? "$uper$ecureL0ngKeyCh@ngeMe!WhyS0L0ngMu$tBeThi$Key";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // Δίνουμε 2 ώρες στους πελάτες για να ψωνίσουν
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}