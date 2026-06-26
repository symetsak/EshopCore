using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        // Inject τον DB Context του Tenant
        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Λήψη ειδοποιήσεων ταξινομημένες από τις πιο πρόσφατες
        public async Task<IEnumerable<Notification>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Notifications
                .Where(n => n.CustomerId == customerId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // Λήψη μιας ειδοποίησης
        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        // Γρήγορο Count των unread (επιστρέφει μόνο έναν αριθμό)
        public async Task<int> GetUnreadCountAsync(int customerId)
        {
            return await _context.Notifications
                .CountAsync(n => n.CustomerId == customerId && !n.IsRead);
        }

        // Προσθήκη ειδοποίησης
        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
        }

        // Save
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}