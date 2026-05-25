using Microsoft.AspNetCore.Mvc;
using Eshop.Application.Services;
using Eshop.Core.Entities;

namespace Eshop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private readonly TenantApplicationService _tenantAppService;

        // Κάνουμε inject το Application Service
        public TenantsController(TenantApplicationService tenantAppService)
        {
            _tenantAppService = tenantAppService;
        }

        // GET: api/tenants
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenants = await _tenantAppService.GetAllTenantsAsync();
            return Ok(tenants);
        }

        // GET: api/tenants/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var tenant = await _tenantAppService.GetTenantByIdAsync(id);
            if (tenant == null) return NotFound($"Ο πελάτης με ID '{id}' δεν βρέθηκε.");

            return Ok(tenant);
        }

        // POST: api/tenants
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tenant tenant)
        {
            try
            {
                var resultMessage = await _tenantAppService.CreateTenantAsync(tenant);
                return Ok(new { message = resultMessage });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Παρουσιάστηκε ένα εσωτερικό σφάλμα στον server.");
            }
        }
    }
}