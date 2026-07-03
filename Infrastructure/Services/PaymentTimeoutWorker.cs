using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql; // Απευθείας Driver της PostgreSQL για 100% multi-tenant απομόνωση
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eshop.API.BackgroundServices
{
    public class PaymentTimeoutWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentTimeoutWorker> _logger;

        // ΓΙΑ ΤΟ ΤΕΣΤ: Το ρομποτάκι ξυπνάει κάθε 10 λεπτά
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);
        // Μετά από πόση ώρα θεωρούμε μια κράτηση ληγμένη (π.χ. 20 λεπτά)
        private readonly TimeSpan _timeoutLimit = TimeSpan.FromMinutes(20);

        public PaymentTimeoutWorker(IServiceProvider serviceProvider, ILogger<PaymentTimeoutWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[Background] Ο PaymentTimeoutWorker ξεκίνησε επιτυχώς τη λειτουργία του!");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("[Background] Σκανάρισμα όλων των Tenants για ληγμένες κρατήσεις (PendingPaid)...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

                        // 1. Φέρνουμε όλους τους ενεργούς Tenants από τη Master DB
                        var activeTenants = await masterDb.Tenants
                            .Where(t => t.IsActive)
                            .ToListAsync(stoppingToken);

                        // 2. Επεξεργασία κάθε Tenant αυτόνομα
                        foreach (var tenant in activeTenants)
                        {
                            if (string.IsNullOrEmpty(tenant.ConnectionString)) continue;

                            try
                            {
                                // RAW POSTGRESQL ΣΥΝΔΕΣΗ (Χωρίς DbContext και Scoped Providers)
                                using (var conn = new NpgsqlConnection(tenant.ConnectionString))
                                {
                                    await conn.OpenAsync(stoppingToken);

                                    // Υπολογίζουμε την ώρα cut-off (20 λεπτά πριν από τώρα)
                                    var cutOffTime = DateTime.UtcNow.Subtract(_timeoutLimit);

                                    // Α) Αναζήτηση παραγγελιών με status "PendingPaid" (Case-Insensitive στην Postgres)
                                    string selectSql = @"
                                        SELECT ""Id"" FROM ""Orders"" 
                                         WHERE LOWER(""Status"") = 'pendingpaid' AND ""OrderDate"" < @CutOffTime;";

                                    var orderIds = new System.Collections.Generic.List<int>();
                                    using (var cmd = new NpgsqlCommand(selectSql, conn))
                                    {
                                        // Περνάμε την παράμετρο της ώρας στην Postgres
                                        cmd.Parameters.AddWithValue("@CutOffTime", cutOffTime);

                                        using (var reader = await cmd.ExecuteReaderAsync(stoppingToken))
                                        {
                                            while (await reader.ReadAsync(stoppingToken))
                                            {
                                                orderIds.Add(reader.GetInt32(0));
                                            }
                                        }
                                    }

                                    // Αν βρεθούν εκκρεμείς παραγγελίες, ξεκινάει το καθάρισμα
                                    if (orderIds.Any())
                                    {
                                        _logger.LogWarning($"[Tenant: {tenant.Id}] Βρέθηκαν {orderIds.Count} απλήρωτες παραγγελίες. Έναρξη αποδέσμευσης stock...");

                                        foreach (var orderId in orderIds)
                                        {
                                            // Β) Επιστροφή των προϊόντων πίσω στο Stock της αποθήκης (Update με Inner Join)
                                            string updateStockSql = @"
                                                UPDATE ""Products"" AS p
                                                SET ""StockQuantity"" = p.""StockQuantity"" + oi.""Quantity""
                                                FROM ""OrderItems"" AS oi
                                                WHERE oi.""ProductId"" = p.""Id"" AND oi.""OrderId"" = @OrderId;";

                                            using (var cmd = new NpgsqlCommand(updateStockSql, conn))
                                            {
                                                cmd.Parameters.AddWithValue("@OrderId", orderId);
                                                await cmd.ExecuteNonQueryAsync(stoppingToken);
                                            }

                                            // Γ) Αλλαγή του status της παραγγελίας σε Cancelled
                                            string updateOrderStatusSql = @"
                                                UPDATE ""Orders"" 
                                                SET ""Status"" = 'Cancelled' 
                                                WHERE ""Id"" = @OrderId;";

                                            using (var cmd = new NpgsqlCommand(updateOrderStatusSql, conn))
                                            {
                                                cmd.Parameters.AddWithValue("@OrderId", orderId);
                                                await cmd.ExecuteNonQueryAsync(stoppingToken);
                                            }

                                            _logger.LogInformation($"[Tenant: {tenant.Id}] Η παραγγελία #{orderId} ακυρώθηκε αυτόματα λόγω μη ολοκλήρωσης πληρωμής.");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Σφάλμα στη βάση του Tenant '{tenant.Id}': {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Γενικό σφάλμα στον Worker: {ex.Message}");
                }

                // Αναμονή 10 δευτερολέπτων μέχρι τον επόμενο έλεγχο
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}