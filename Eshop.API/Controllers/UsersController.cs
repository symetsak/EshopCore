using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Repositories;
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

        [AllowAnonymous]
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

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Το Refresh Token είναι υποχρεωτικό." });
            }

            var result = await _userService.LogoutAsync(request.RefreshToken);

            if (!result)
            {
                return BadRequest(new { message = "Μη έγκυρο ή ήδη ληγμένο Refresh Token." });
            }

            return Ok(new { message = "Αποσύνδεση επιτυχής. Το Refresh Token ακυρώθηκε." });
        }

        [HttpPost("change-password")] 
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            // Διαβάζουμε το TenantId από το Header
            if (!Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
            {
                return BadRequest(new { message = "Το Header 'X-Tenant-Id' λείπει από το αίτημα." });
            }

            // Καλούμε το Service Layer
            bool result = await _userService.ChangePasswordAsync(request, tenantId.ToString());

            if (!result)
            {
                return BadRequest(new { message = "Ο τωρινός κωδικός είναι λάθος ή ο χρήστης δεν βρέθηκε." });
            }

            return Ok(new { message = "Ο κωδικός πρόσβασης άλλαξε επιτυχώς!" });
        }

        [HttpPost("create")]
        [Authorize(Roles = "Administrator")] 
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            bool result = await _userService.CreateUserAsync(dto);
            if (!result) return BadRequest(new { message = "Το όνομα χρήστη χρησιμοποιείται ήδη." });

            return Ok(new { message = "Ο χρήστης δημιουργήθηκε επιτυχώς!" });
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Administrator")] 
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            bool result = await _userService.UpdateUserAsync(id, dto);
            if (!result) return NotFound(new { message = "Ο χρήστης δεν βρέθηκε." });

            return Ok(new { message = "Τα στοιχεία του χρήστη ενημερώθηκαν!" });
        }

        [HttpGet("all")]
        [Authorize] 
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}/notes")]
        [Authorize]
        public async Task<IActionResult> GetUserNotes(int id)
        {
            var notes = await _userService.GetUserNotesAsync(id);
            return Ok(notes);
        }

        [HttpPost("my-notes")]
        [Authorize]
        public async Task<IActionResult> AddMyNote([FromBody] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest(new { message = "Η σημείωση δεν μπορεί να είναι κενή." });

            // Παίρνουμε το Username και το UserId κατευθείαν από το Token (Claims)
            var currentUsername = User.Identity?.Name ?? "Unknown";
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Δεν βρέθηκε το ID του χρήστη στο token." });

            var success = await _userService.AddUserNoteAsync(userId, content, currentUsername);

            if (!success) return NotFound(new { message = "Ο χρήστης δεν βρέθηκε." });

            return Ok(new { message = "Η παρατήρηση προστέθηκε!" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")] 
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result) return NotFound(new { message = "Ο χρήστης δεν βρέθηκε." });

            return Ok(new { message = "Ο χρήστης διαγράφηκε επιτυχώς!" });
        }
    }
}