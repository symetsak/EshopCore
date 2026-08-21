using Eshop.Application.Interfaces;
using Eshop.Core.Entities;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Eshop.Infrastructure.Interceptors
{
    public class AuditLogInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IServiceProvider _serviceProvider;

        public AuditLogInterceptor(ICurrentUserService currentUserService, IServiceProvider serviceProvider)
        {
            _currentUserService = currentUserService;
            _serviceProvider = serviceProvider;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;

            // Αν για κάποιο λόγο δεν υπάρχει context ή είναι το ίδιο το MasterDbContext, το αγνοούμε!
            // (Δεν θέλουμε να ρουφιανεύουμε τον πίνακα των ρουφιάνων!)
            if (context == null || context is MasterDbContext)
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

            var auditEntries = CreateAuditEntries(context);

            if (auditEntries.Any())
            {
                // Ζητάμε ένα "φρέσκο" MasterDbContext για να σώσουμε τα logs με ασφάλεια
                using var scope = _serviceProvider.CreateScope();
                var masterContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

                masterContext.AuditLogs.AddRange(auditEntries);
                await masterContext.SaveChangesAsync(cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // Η ίδια λογική και για το σύγχρονο SaveChanges (για κάθε ενδεχόμενο)
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var context = eventData.Context;

            if (context == null || context is MasterDbContext)
                return base.SavingChanges(eventData, result);

            var auditEntries = CreateAuditEntries(context);

            if (auditEntries.Any())
            {
                using var scope = _serviceProvider.CreateScope();
                var masterContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

                masterContext.AuditLogs.AddRange(auditEntries);
                masterContext.SaveChanges();
            }

            return base.SavingChanges(eventData, result);
        }

        // Εδώ διαβάζουμε τι άλλαξε.
        private List<AuditLog> CreateAuditEntries(DbContext context)
        {
            var tenantId = _currentUserService.TenantId ?? "UnknownTenant";
            var username = _currentUserService.Username ?? "System";

            var auditLogs = new List<AuditLog>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditLog = new AuditLog
                {
                    TableName = entry.Entity.GetType().Name,
                    Username = username,
                    TenantId = tenantId,
                    Timestamp = DateTime.UtcNow
                };

                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary) continue;

                    string propertyName = property.Metadata.Name;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            newValues[propertyName] = property.CurrentValue;
                            auditLog.Action = "INSERT";
                            break;

                        case EntityState.Deleted:
                            oldValues[propertyName] = property.OriginalValue;
                            auditLog.Action = "DELETE";
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                oldValues[propertyName] = property.OriginalValue;
                                newValues[propertyName] = property.CurrentValue;
                                auditLog.Action = "UPDATE";
                            }
                            break;
                    }
                }

                auditLog.OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
                auditLog.NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;

                auditLogs.Add(auditLog);
            }

            return auditLogs;
        }
    }
}