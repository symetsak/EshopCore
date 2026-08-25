using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate) // Οι πιο πρόσφατες πρώτες
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            // Επιστρέφει όλες τις παραγγελίες του συγκεκριμένου Tenant (π.χ. για τον Admin)
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product!).ThenInclude(p => p.Category)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Order?> GetByIdWithItemsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)          // Φέρνει τη λίστα των προϊόντων της παραγγελίας
                    .ThenInclude(oi => oi.Product!)       // Για κάθε item, φέρνει τις λεπτομέρειες του Product (Name, Price κλπ)
                    .ThenInclude(p => p.Category)       // Για κάθε Product, φέρνει τις λεπτομέρειες της Category
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<PagedResultDto<Order>> GetPagedOrdersAsync(OrderFilterDto filter)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product!).ThenInclude(p => p.Category)
                .AsQueryable();

            // 1. ΦΙΛΤΡΟ: Αναζήτηση (με ID ή Όνομα/Email Πελάτη)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                // Αφαιρούμε το '#' σε περίπτωση που ο χρήστης ψάξει "#63"
                var search = filter.SearchTerm.ToLower().Replace("#", "");
                bool isNumeric = int.TryParse(search, out int searchId);

                query = query.Where(o =>
                    (isNumeric && o.Id == searchId) ||
                    (o.Customer != null && (o.Customer.FirstName + " " + o.Customer.LastName).ToLower().Contains(search)) ||
                    (o.Customer != null && o.Customer.Email.ToLower().Contains(search))
                );
            }

            // 2. ΦΙΛΤΡΟ: Ημερομηνίες
            if (filter.MinDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= filter.MinDate.Value.Date);
            }

            if (filter.MaxDate.HasValue)
            {
                var nextDay = filter.MaxDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < nextDay);
            }

            // 3. ΦΙΛΤΡΟ: Καταστάσεις (Statuses) - Πολλαπλή επιλογή
            if (filter.Statuses != null && filter.Statuses.Any())
                query = query.Where(o => filter.Statuses.Contains(o.Status));

            // 4. ΦΙΛΤΡΟ: Τρόποι Πληρωμής - Πολλαπλή επιλογή
            if (filter.PaymentMethods != null && filter.PaymentMethods.Any())
                query = query.Where(o => filter.PaymentMethods.Contains(o.PaymentMethod));

            // 5. ΤΑΞΙΝΟΜΗΣΗ (Sorting)
            query = filter.SortBy?.ToLower() switch
            {
                "id" => query.OrderBy(o => o.Id),
                "id_desc" => query.OrderByDescending(o => o.Id),
                "date" => query.OrderBy(o => o.OrderDate),
                "date_desc" => query.OrderByDescending(o => o.OrderDate),
                "amount" => query.OrderBy(o => o.TotalAmount),
                "amount_desc" => query.OrderByDescending(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.OrderDate) // Default: Οι πιο πρόσφατες πρώτες
            };

            // 6. ΣΕΛΙΔΟΠΟΙΗΣΗ
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultDto<Order>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
            };
        }

        public async Task<List<Order>> GetOrdersForExportAsync(OrderFilterDto filter)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product!).ThenInclude(p => p.Category)
                .AsQueryable();

            // 1. ΦΙΛΤΡΟ: Αναζήτηση
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower().Replace("#", "");
                bool isNumeric = int.TryParse(search, out int searchId);

                query = query.Where(o =>
                    (isNumeric && o.Id == searchId) ||
                    (o.Customer != null && (o.Customer.FirstName + " " + o.Customer.LastName).ToLower().Contains(search)) ||
                    (o.Customer != null && o.Customer.Email.ToLower().Contains(search))
                );
            }

            // 2. ΦΙΛΤΡΟ: Ημερομηνίες
            if (filter.MinDate.HasValue)
                query = query.Where(o => o.OrderDate >= filter.MinDate.Value.Date);

            if (filter.MaxDate.HasValue)
            {
                var nextDay = filter.MaxDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < nextDay);
            }

            // 3. ΦΙΛΤΡΟ: Καταστάσεις
            if (filter.Statuses != null && filter.Statuses.Any())
                query = query.Where(o => filter.Statuses.Contains(o.Status));

            // 4. ΦΙΛΤΡΟ: Τρόποι Πληρωμής
            if (filter.PaymentMethods != null && filter.PaymentMethods.Any())
                query = query.Where(o => filter.PaymentMethods.Contains(o.PaymentMethod));

            // 5. ΤΑΞΙΝΟΜΗΣΗ
            query = filter.SortBy?.ToLower() switch
            {
                "id" => query.OrderBy(o => o.Id),
                "id_desc" => query.OrderByDescending(o => o.Id),
                "date" => query.OrderBy(o => o.OrderDate),
                "date_desc" => query.OrderByDescending(o => o.OrderDate),
                "amount" => query.OrderBy(o => o.TotalAmount),
                "amount_desc" => query.OrderByDescending(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            // ΕΠΙΣΤΡΕΦΕΙ ΟΛΑ ΤΑ MATCHES ΧΩΡΙΣ PAGINATION
            return await query.ToListAsync();
        }
    }
}