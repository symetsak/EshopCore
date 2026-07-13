using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Eshop.Infrastructure.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;

        public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[Cleanup Worker] Ο Background Worker εκκαθάρισης ληγμένων Refresh Tokens ξεκίνησε στο Infrastructure Layer.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("[Cleanup Worker] Σκανάρισμα όλων των Tenants για ληγμένα Refresh Tokens...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

                        // 1. Φέρνουμε όλους τους ενεργούς Tenants από τη Master DB (Ακριβώς όπως ο PaymentWorker)
                        var activeTenants = await masterDb.Tenants.Where(t => t.IsActive).ToListAsync(stoppingToken);

                        _logger.LogInformation($"[Cleanup Worker] Το EF Core διάβασε {activeTenants.Count} ενεργούς Tenants από τη Master DB.");

                        // 2. Επεξεργασία κάθε Tenant αυτόνομα
                        foreach (var tenant in activeTenants)
                        {
                            if (string.IsNullOrEmpty(tenant.ConnectionString)) continue;

                            try
                            {
                                // RAW POSTGRESQL ΣΥΝΔΕΣΗ
                                using (var conn = new NpgsqlConnection(tenant.ConnectionString))
                                {
                                    await conn.OpenAsync(stoppingToken);

                                    // 3. SQL Query με αυτόματο UTC υπολογισμό από την ίδια την Postgres
                                    string deleteSql = @"
                                        DELETE FROM ""RefreshTokens"" 
                                        WHERE ""ExpiresAt"" < NOW();";

                                    using (var cmd = new NpgsqlCommand(deleteSql, conn))
                                    {
                                        int rowsAffected = await cmd.ExecuteNonQueryAsync(stoppingToken);

                                        // Εκτύπωση ΜΟΝΟ αν βρει και σβήσει κάτι
                                        if (rowsAffected > 0)
                                        {
                                            _logger.LogInformation($"[Tenant: {tenant.Id}] Ο Worker σκάναρε τη βάση. Διαγράφηκαν {rowsAffected} ληγμένα tokens.");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"[Cleanup Worker] Σφάλμα στη βάση του Tenant '{tenant.Id}': {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Cleanup Worker] Γενικό σφάλμα στον Worker εκκαθάρισης: {ex.Message}");
                }

                // Αναμονή 10 δευτερολέπτων για το τεστ
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}