using Eshop.API.Attributes;
using Eshop.Application.Services;
using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreTenant]
    [Authorize]
    public class TenantsController : ControllerBase
    {
        private readonly TenantApplicationService _tenantAppService;

        // Clean Architecture: Μόνο το Application Service γίνεται inject εδώ!
        public TenantsController(TenantApplicationService tenantAppService)
        {
            _tenantAppService = tenantAppService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenants = await _tenantAppService.GetAllTenantsAsync();
            return Ok(tenants);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var tenant = await _tenantAppService.GetTenantByIdAsync(id);
            if (tenant == null) return NotFound($"Ο πελάτης με ID '{id}' δεν βρέθηκε.");

            return Ok(tenant);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tenant tenant)
        {
            try
            {
                var resultMessage = await _tenantAppService.CreateTenantAsync(tenant);
                return Ok(new { message = resultMessage });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            catch (Exception) { return StatusCode(500, "Παρουσιάστηκε ένα εσωτερικό σφάλμα στον server."); }
        }

        // 1. Ενημέρωση Στοιχείων
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenantDetails(string id, [FromBody] UpdateΤenantDetailsDto dto)
        {
            try
            {
                await _tenantAppService.UpdateTenantDetailsAsync(id, dto);
                return Ok(new { message = "Τα στοιχεία ενημερώθηκαν επιτυχώς." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 2. Toggle Status (Αναστολή/Ενεργοποίηση)
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleTenantStatus(string id)
        {
            try
            {
                var newStatus = await _tenantAppService.ToggleTenantStatusAsync(id);
                var statusMsg = newStatus ? "ενεργοποιήθηκε" : "απενεργοποιήθηκε (Suspend)";
                return Ok(new { message = $"Ο πελάτης {statusMsg} επιτυχώς.", isActive = newStatus });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 3. Λήψη Ιστορικού Συναλλαγών
        [HttpGet("{id}/transactions")]
        public async Task<IActionResult> GetTransactions(string id)
        {
            var transactions = await _tenantAppService.GetTenantTransactionsAsync(id);
            return Ok(transactions);
        }

        // 4. Προσθήκη Νέας Συναλλαγής (Χρέωση / Πληρωμή)
        [HttpPost("{id}/transactions")]
        public async Task<IActionResult> AddTransaction(string id, [FromBody] CreateTransactionDto dto)
        {
            try
            {
                var newBalance = await _tenantAppService.AddTransactionAndUpdateBalanceAsync(id, dto);
                return Ok(new { message = "Η συναλλαγή καταχωρήθηκε επιτυχώς.", newBalance });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 5. Διαγραφή/Αναίρεση Συναλλαγής
        [HttpDelete("{tenantId}/transactions/{transactionId}")]
        public async Task<IActionResult> DeleteTransaction(string tenantId, int transactionId)
        {
            try
            {
                var newBalance = await _tenantAppService.DeleteTransactionAndUpdateBalanceAsync(tenantId, transactionId);
                return Ok(new { message = "Η συναλλαγή διαγράφηκε επιτυχώς.", newBalance });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/notes")]
        public async Task<IActionResult> UpdateTenantNotes(string id, [FromBody] UpdateTenantNotesDto dto)
        {
            try
            {
                await _tenantAppService.UpdateTenantNotesAsync(id, dto.Notes);
                return Ok(new { message = "Οι σημειώσεις αποθηκεύτηκαν επιτυχώς." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}