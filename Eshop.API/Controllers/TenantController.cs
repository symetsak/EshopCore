using Eshop.Application.Services;
using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private readonly TenantApplicationService _tenantAppService;
        private readonly ITenantRepository _tenantRepository;

        // Κάνουμε inject το Application Service
        public TenantsController(TenantApplicationService tenantAppService, ITenantRepository tenantRepository)
        {
            _tenantAppService = tenantAppService;
            _tenantRepository = tenantRepository;
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(string id, [FromBody] TenantUpdateDto dto)
        {
            // 1. Ψάχνουμε τον Tenant στη Master Βάση
            var tenant = await _tenantRepository.GetByIdAsync(id);

            if (tenant == null)
            {
                return NotFound(new { message = $"Ο Tenant με ID '{id}' δεν βρέθηκε." });
            }

            // 2. Ενημερώνουμε τα πεδία
            tenant.Name = dto.Name;
            tenant.IsActive = dto.IsActive;

            // 3. Σώζουμε τις αλλαγές direct μέσω του Repo
            await _tenantRepository.SaveChangesAsync();

            return Ok(new { message = $"Ο Tenant '{id}' ενημερώθηκε επιτυχώς.", tenant });
        }
    }
}