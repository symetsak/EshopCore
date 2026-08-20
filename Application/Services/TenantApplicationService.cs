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

        public async Task UpdateTenantDetailsAsync(string id, UpdateΤenantDetailsDto dto)
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

        public async Task<IEnumerable<TenantTransactionDto>> GetTenantTransactionsAsync(string tenantId)
        {
            var transactions = await _tenantRepository.GetTransactionsByTenantIdAsync(tenantId);

            return transactions.Select(t => new TenantTransactionDto
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                Description = t.Description,
                Amount = t.Amount,
                Type = (int)t.Type
            });
        }

        public async Task<decimal> AddTransactionAndUpdateBalanceAsync(string tenantId, CreateTransactionDto dto)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null)
                throw new KeyNotFoundException($"Ο πελάτης με ID '{tenantId}' δεν βρέθηκε.");

            // Δημιουργία της συναλλαγής
            var transaction = new TenantTransaction
            {
                TenantId = tenant.Id,
                Description = dto.Description,
                Amount = dto.Amount,
                Type = (TransactionType)dto.Type,
                CreatedAt = DateTime.UtcNow
            };

            // Ενημέρωση του Υπολοίπου (Balance)
            if (transaction.Type == TransactionType.Charge)
            {
                tenant.Balance += transaction.Amount; // Χρέωση: Το υπόλοιπο μεγαλώνει
            }
            else if (transaction.Type == TransactionType.Payment)
            {
                tenant.Balance -= transaction.Amount; // Πληρωμή: Το υπόλοιπο μικραίνει
            }

            await _tenantRepository.AddTransactionAsync(transaction);
            await _tenantRepository.SaveChangesAsync();

            return tenant.Balance; // Επιστρέφουμε το νέο υπόλοιπο
        }

        public async Task<decimal> DeleteTransactionAndUpdateBalanceAsync(string tenantId, int transactionId)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null) throw new KeyNotFoundException("Ο πελάτης δεν βρέθηκε.");

            var transaction = await _tenantRepository.GetTransactionByIdAsync(transactionId);
            if (transaction == null || transaction.TenantId != tenantId)
                throw new KeyNotFoundException("Η συναλλαγή δεν βρέθηκε ή δεν ανήκει σε αυτόν τον πελάτη.");

            // Αντιστροφή της πράξης στο Υπόλοιπο (Reverse)
            if (transaction.Type == TransactionType.Charge)
            {
                tenant.Balance -= transaction.Amount; 
            }
            else if (transaction.Type == TransactionType.Payment)
            {
                tenant.Balance += transaction.Amount;
            }

            await _tenantRepository.DeleteTransactionAsync(transaction);
            await _tenantRepository.SaveChangesAsync();

            return tenant.Balance;
        }

        public async Task UpdateTenantNotesAsync(string id, string? notes)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null) throw new KeyNotFoundException($"Ο πελάτης με ID '{id}' δεν βρέθηκε.");

            tenant.Notes = notes;
            await _tenantRepository.SaveChangesAsync();
        }
    }
}