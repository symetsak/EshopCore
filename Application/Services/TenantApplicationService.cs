using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Application.Services
{
    public class TenantApplicationService
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ITenantDatabaseService _tenantDbService;

        // Κάνουμε inject το Interface του Core. 
        // Το Application layer δεν ξέρει ΠΟΥ αποθηκεύονται, απλά ζητάει το repository.
        public TenantApplicationService(ITenantRepository tenantRepository, ITenantDatabaseService tenantDbService)
        {
            _tenantRepository = tenantRepository;
            _tenantDbService = tenantDbService;
        }

        public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
        {
            return await _tenantRepository.GetAllAsync();
        }

        public async Task<Tenant?> GetTenantByIdAsync(string id)
        {
            return await _tenantRepository.GetByIdAsync(id);
        }

        public async Task<string> CreateTenantAsync(Tenant tenant)
        {
            // Εδώ μπαίνει το Business Logic (Επιχειρηματικοί κανόνες)

            // Κανόνας 1: Έλεγχος αν το ID είναι κενό
            if (string.IsNullOrWhiteSpace(tenant.Id))
            {
                throw new ArgumentException("Το Tenant ID δεν μπορεί να είναι κενό.");
            }

            // Κανόνας 2: Έλεγχος αν υπάρχει ήδη πελάτης με το ίδιο ID
            var isDuplicate = await _tenantRepository.ExistsAsync(tenant.Id);
            if (isDuplicate)
            {
                throw new InvalidOperationException($"Ο πελάτης με ID '{tenant.Id}' υπάρχει ήδη.");
            }

            // 1. Καλούμε το Interface. Δεν μας νοιάζει ΠΩΣ θα φτιαχτεί η βάση, 
            // το Infrastructure θα αναλάβει τη βαριά δουλειά!
            await _tenantDbService.CreateTenantDatabaseAsync(tenant.ConnectionString);

            // 2. Αποθήκευση στον Master
            await _tenantRepository.AddAsync(tenant);

            return $"Ο πελάτης '{tenant.Name}' και η προσωπική του βάση δεδομένων δημιουργήθηκαν με επιτυχία!";
        }

        public async Task UpdateTenantDetailsAsync(string id, UpdateTenantDetailsDto dto)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                throw new KeyNotFoundException($"Ο πελάτης με ID '{id}' δεν βρέθηκε.");

            tenant.Name = dto.Name;
            tenant.Address = dto.Address;
            tenant.City = dto.City;
            tenant.Email = dto.Email;
            tenant.Mobile = dto.Mobile;

            await _tenantRepository.SaveChangesAsync();
        }

        public async Task<bool> ToggleTenantStatusAsync(string id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                throw new KeyNotFoundException($"Ο πελάτης με ID '{id}' δεν βρέθηκε.");

            tenant.IsActive = !tenant.IsActive;
            await _tenantRepository.SaveChangesAsync();

            return tenant.IsActive;
        }
    }
}