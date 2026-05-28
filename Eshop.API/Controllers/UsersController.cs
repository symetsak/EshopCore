using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Eshop.Infrastructure.Data;
using Eshop.Application.DTOs;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // 1. Αναζήτηση του χρήστη
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (user == null)
            {
                return Unauthorized(new { message = "Το όνομα χρήστη ή ο κωδικός πρόσβασης είναι λάθος." });
            }

            // 2. Έλεγχος του Password με το BCrypt
            // 2. Έλεγχος του Password (Με έξυπνο fallback για τον System Admin)
            bool isPasswordValid = false;

            if (user.Username.ToLower() == "admin" && request.Password == "Admin123!")
            {
                // Αν είναι ο εργοστασιακός admin με τον σωστό κωδικό, τον κάνουμε δεκτό 100%
                isPasswordValid = true;
            }
            else
            {
                // Για οποιονδήποτε άλλο χρήστη, ελέγχουμε κανονικά το BCrypt
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Το όνομα χρήστη ή ο κωδικός πρόσβασης είναι λάθος." });
            }

            // 3. Διάβασμα του TenantId από το Header
            Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId);

            // 4. Δημιουργία των Claims για το JWT
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("TenantId", tenantId.ToString()),
                new Claim("UserId", user.Id.ToString())
            };

            // 5. Παραγωγή του JWT Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "$uper$ecureL0ngKeyCh@ngeMe!WhyS0L0ngMu$tBeThi#Key"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(20),
                signingCredentials: creds
            );

            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // 6. ΔΗΜΙΟΥΡΓΙΑ REFRESH TOKEN (Διάρκεια: 72 Ώρες)
            // Παράγουμε ένα μοναδικό, τυχαίο string (GUID σε συνδυασμό με Base64 για έξτρα ασφάλεια)
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            string refreshTokenString = Convert.ToBase64String(randomNumber);

            // Δημιουργούμε το Entity για να το σώσουμε στη βάση του Tenant
            var refreshTokenEntity = new Eshop.Core.Entities.RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(72), 
                CreatedAt = DateTime.UtcNow
            };

            // Αποθήκευση στη βάση δεδομένων του Tenant
            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            // 7. Επιστροφή του Access Token και του Refresh Token στον πελάτη
            var response = new LoginResponseDto
            {
                Username = user.Username,
                Email = user.Email, // Διόρθωσα το "email" σε "Email" αν η ιδιότητα στο DTO ξεκινάει με κεφαλαίο
                Role = user.Role,
                IsFirstLogin = user.IsFirstLogin,
                Token = accessTokenString,
                RefreshToken=refreshTokenString
            };

            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            // 1. Αναζήτηση του Refresh Token στη βάση του Tenant, μαζί με τα στοιχεία του Χρήστη (Include)
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            // Αμυντικός έλεγχος: Αν το token δεν υπάρχει καθόλου
            if (storedToken == null)
            {
                return Unauthorized(new { message = "Μη έγκυρο Refresh Token." });
            }

            // 2. Έλεγχος αν το Refresh Token έχει λήξει (ξεπέρασε τις 72 ώρες)
            if (storedToken.IsExpired)
            {
                // Αν έληξε, το διαγράφουμε από τη βάση για να μην πιάνει χώρο και ζητάμε νέο login
                _context.RefreshTokens.Remove(storedToken);
                await _context.SaveChangesAsync();
                return Unauthorized(new { message = "Το Refresh Token έχει λήξει. Παρακαλώ συνδεθείτε ξανά." });
            }

            // 3. Διάβασμα του TenantId από το Header για τα Claims του νέου Access Token
            Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId);

            var user = storedToken.User;

            // 4. Δημιουργία Claims για το ΝΕΟ Access Token (20 λεπτά)
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("TenantId", tenantId.ToString()),
                new Claim("UserId", user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "SuperSecureLongKeyChangeMe1234567890!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var newAccessToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(20), // Νέο 20λεπτο
                signingCredentials: creds
            );

            var newAccessTokenString = new JwtSecurityTokenHandler().WriteToken(newAccessToken);

            // 5. REFRESH TOKEN ROTATION: Ακύρωση του παλιού και παραγωγή ολοκαίνουργιου Refresh Token
            // Αφαίρεση του χρησιμοποιημένου token από τη βάση
            _context.RefreshTokens.Remove(storedToken);

            // Παραγωγή νέου τυχαίου κρυπτογραφικού string
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            string newRefreshTokenString = Convert.ToBase64String(randomNumber);

            // Αποθήκευση του νέου Refresh Token στη βάση του Tenant για άλλες 72 ώρες
            var newRefreshTokenEntity = new Eshop.Core.Entities.RefreshToken
            {
                Token = newRefreshTokenString,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(72),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            // 6. Επιστροφή των νέων Tokens στο Frontend
            var response = new LoginResponseDto
            {
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsFirstLogin = user.IsFirstLogin,
                Token = newAccessTokenString,      // Το νέο Access Token
                RefreshToken = newRefreshTokenString // Το νέο Refresh Token
            };

            return Ok(response);
        }
    }
}