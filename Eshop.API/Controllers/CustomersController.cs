using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // POST: api/customers/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CustomerRegisterDto dto)
        {
            try
            {
                var response = await _customerService.RegisterAsync(dto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // Αν το email υπάρχει ήδη, επιστρέφουμε 400 Bad Request με το μήνυμα
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/customers/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CustomerLoginRequestDto dto)
        {
            var response = await _customerService.LoginAsync(dto);
            if (response == null)
            {
                return Unauthorized(new { message = "Το email ή ο κωδικός πρόσβασης είναι λανθασμένα." });
            }

            return Ok(response);
        }

        // POST: api/customers/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] CustomerRefreshRequestDto dto)
        {
            var response = await _customerService.RefreshTokenAsync(dto);
            if (response == null)
            {
                return BadRequest(new { message = "Μη έγκυρο ή ληγμένο Refresh Token." });
            }

            return Ok(response);
        }

        // POST: api/customers/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] CustomerRefreshRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto.RefreshToken))
            {
                return BadRequest(new { message = "Το Refresh Token είναι υποχρεωτικό." });
            }

            var result = await _customerService.LogoutAsync(dto.RefreshToken);

            if (!result)
            {
                return BadRequest(new { message = "Μη έγκυρο ή ήδη ληγμένο Refresh Token." });
            }

            return Ok(new { message = "Αποσύνδεση πελάτη επιτυχής. Το token ακυρώθηκε." });
        }

        // GET: api/customers/profile
        [HttpGet("profile")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetProfile()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized(new { message = "Μη έγκυρο token πελάτη." });
            }

            var profile = await _customerService.GetProfileAsync(customerId);
            if (profile == null)
            {
                return NotFound(new { message = "Ο πελάτης δεν βρέθηκε." });
            }

            return Ok(profile);
        }

        // PUT: api/customers/profile
        [HttpPut("profile")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateProfile([FromBody] CustomerUpdateProfileDto dto)
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
            {
                return Unauthorized(new { message = "Μη έγκυρο token πελάτη." });
            }

            var updatedProfile = await _customerService.UpdateProfileAsync(customerId, dto);
            if (updatedProfile == null)
            {
                return NotFound(new { message = "Ο πελάτης δεν βρέθηκε." });
            }

            return Ok(updatedProfile);
        }
    }
}