using Eshop.API.Attributes;
using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [ApiController]
    [Route("api/system")]
    [IgnoreTenant]
    public class SystemAuthController : ControllerBase
    {
        private readonly ISystemAuthService _systemAuthService;

        public SystemAuthController(ISystemAuthService systemAuthService)
        {
            _systemAuthService = systemAuthService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequestDto request)
        {
            // Ο Controller απλά ρωτάει το Service: "Είναι σωστά τα στοιχεία;"
            var token = _systemAuthService.Login(request);

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "Λάθος στοιχεία σύνδεσης." });
            }

            return Ok(new { Token = token });
        }
    }
}