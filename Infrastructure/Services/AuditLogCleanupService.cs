using Eshop.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Services
{
    public class AuditLogCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogCleanupService> _logger;

        public AuditLogCleanupService(IServiceProvider serviceProvider, ILogger<AuditLogCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Το σύστημα εκκαθάρισης Audit Logs ξεκίνησε.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                // Ελέγχουμε αν είναι 1η Ιανουαρίου (Μήνας 1, Μέρα 1)
                if (now.Month == 1 && now.Day == 1)
                {
                    _logger.LogInformation("Είναι 1η Ιανουαρίου! Ξεκινάει ο καθαρισμός των παλιών Logs...");

                    await CleanupOldLogsAsync(stoppingToken);

                    // Περιμένουμε 24 ώρες για να μην ξανατρέξει την ίδια μέρα
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                else
                {
                    // Αν δεν είναι 1η Ιανουαρίου, υπολογίζουμε πόσος χρόνος μένει μέχρι τα επόμενα μεσάνυχτα
                    var nextMidnight = now.Date.AddDays(1); // Τα μεσάνυχτα της επόμενης μέρας
                    var timeToWait = nextMidnight - now;

                    // Κοιμίζουμε το service μέχρι αύριο τα μεσάνυχτα
                    await Task.Delay(timeToWait, stoppingToken);
                }
            }
        }

        private async Task CleanupOldLogsAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Ανοίγουμε ένα Scope για να ζητήσουμε το MasterDbContext
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

                // Υπολογίζουμε την ημερομηνία πριν από 1 χρόνο (για να κρατήσουμε μόνο τα logs του τελευταίου έτους)
                var oneYearAgo = DateTime.UtcNow.AddYears(-1);

                // Η ΜΑΓΕΙΑ ΤΟΥ .NET 7+: Απευθείας διαγραφή στη βάση (Bulk Delete)
                var deletedCount = await dbContext.AuditLogs
                    .Where(log => log.Timestamp < oneYearAgo)
                    .ExecuteDeleteAsync(stoppingToken);

                _logger.LogInformation($"Ο καθαρισμός ολοκληρώθηκε επιτυχώς! Διαγράφηκαν {deletedCount} παλιά logs.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Υπήρξε σφάλμα κατά τη διαγραφή των παλιών Audit Logs.");
            }
        }
    }
}
