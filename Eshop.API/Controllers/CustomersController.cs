using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
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
    }
}