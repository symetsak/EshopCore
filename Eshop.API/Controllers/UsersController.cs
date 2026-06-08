using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // Διάβασμα του TenantId από το Header
            Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId);

            var response = await _userService.LoginAsync(request, tenantId.ToString());

            if (response == null)
            {
                return Unauthorized(new { message = "Το όνομα χρήστη ή ο κωδικός πρόσβασης είναι λάθος." });
            }

            return Ok(response);
        }

        [Authorize]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId);

            try
            {
                var response = await _userService.RefreshAsync(request, tenantId.ToString());
                if (response == null)
                {
                    return Unauthorized(new { message = "Μη έγκυρο Refresh Token." });
                }

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // Πιάνει την περίπτωση που το Refresh Token έχει λήξει
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}