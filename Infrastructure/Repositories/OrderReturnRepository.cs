using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data; // Ή όπου έχεις το φάκελο του DbContext σου
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eshop.Infrastructure.Repositories
{
    public class OrderReturnRepository : IOrderReturnRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderReturnRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OrderReturn?> GetByIdWithItemsAsync(int id)
        {
            // Κάνουμε .Include για να φέρουμε τις γραμμές της επιστροφής 
            // και .ThenInclude για να πάρουμε και τα στοιχεία του προϊόντος (π.χ. Όνομα)
            return await _context.OrderReturns
                .Include(r => r.ReturnItems)
                .ThenInclude(ri => ri.Product)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<OrderReturn>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.OrderReturns
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<OrderReturn>> GetAllReturnsAsync()
        {
            return await _context.OrderReturns
                .Include(r => r.Order)
                .Include(r => r.ReturnItems).ThenInclude(ri => ri.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(OrderReturn orderReturn)
        {
            await _context.OrderReturns.AddAsync(orderReturn);
        }

        public void Update(OrderReturn orderReturn)
        {
            _context.OrderReturns.Update(orderReturn);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // ΠΡΟΣΘΗΚΗ: Νέα μέθοδος για Paged & Filtered Επιστροφές
        public async Task<PagedResultDto<OrderReturn>> GetPagedReturnsAsync(OrderReturnFilterDto filter)
        {
            var query = _context.OrderReturns
                .Include(r => r.Order)
                    .ThenInclude(o => o.Customer) // Χρειάζεται για να ψάχνουμε με το όνομα του πελάτη
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.Product)
                .AsQueryable();

            // 1. ΦΙΛΤΡΟ: Αναζήτηση (ID Επιστροφής, ID Παραγγελίας, Όνομα Πελάτη)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower().Replace("#", "");
                bool isNumeric = int.TryParse(search, out int searchId);

                query = query.Where(r =>
                    (isNumeric && r.Id == searchId) ||
                    (isNumeric && r.OrderId == searchId) ||
                    (r.Order != null && r.Order.Customer != null && (r.Order.Customer.FirstName + " " + r.Order.Customer.LastName).ToLower().Contains(search))
                );
            }

            // 2. ΦΙΛΤΡΟ: Ημερομηνίες
            if (filter.MinDate.HasValue)
            {
                // .Date για να σιγουρευτούμε ότι ξεκινάει από τις 00:00:00 της επιλεγμένης μέρας
                query = query.Where(r => r.CreatedAt >= filter.MinDate.Value.Date);
            }

            if (filter.MaxDate.HasValue)
            {
                // Προσθέτουμε 1 μέρα και κάνουμε < (αυστηρά μικρότερο)
                // Π.χ. αν διάλεξε 30/06, θα ψάξει < 01/07 00:00:00 (άρα πιάνει όλη την 30/06)
                var nextDay = filter.MaxDate.Value.Date.AddDays(1);
                query = query.Where(r => r.CreatedAt < nextDay);
            }

            // 3. ΦΙΛΤΡΟ: Καταστάσεις
            if (filter.Statuses != null && filter.Statuses.Any())
            {
                var statusesLower = filter.Statuses.Select(s => s.ToLower()).ToList();
                query = query.Where(r => r.Status != null && statusesLower.Contains(r.Status.ToLower()));
            }

            // 4. ΦΙΛΤΡΟ: Τύπος Επιστροφής (π.χ. Total, Partial)
            if (filter.ReturnTypes != null && filter.ReturnTypes.Any())
            {
                bool wantsTotal = filter.ReturnTypes.Any(t => t.Equals("Total", StringComparison.OrdinalIgnoreCase));
                bool wantsPartial = filter.ReturnTypes.Any(t => t.Equals("Partial", StringComparison.OrdinalIgnoreCase));

                if (wantsTotal && !wantsPartial)
                {
                    // Θέλει ΜΟΝΟ "Total" (Ολική)
                    query = query.Where(r => r.ReturnType != null && r.ReturnType.ToLower() == "total");
                }
                else if (wantsPartial && !wantsTotal)
                {
                    // Θέλει ΜΟΝΟ "Partial" (Μερική), δηλαδή οτιδήποτε ΔΕΝ είναι "Total" ή είναι null!
                    query = query.Where(r => r.ReturnType == null || r.ReturnType.ToLower() != "total");
                }
                // Αν τα έχει τσεκάρει και τα 2, δεν μπαίνει κανένα WHERE, τα φέρνει όλα κανονικά!
            }

            // 5. ΤΑΞΙΝΟΜΗΣΗ
            query = filter.SortBy?.ToLower() switch
            {
                "id" => query.OrderBy(r => r.Id),
                "id_desc" => query.OrderByDescending(r => r.Id),
                "date" => query.OrderBy(r => r.CreatedAt),
                "date_desc" => query.OrderByDescending(r => r.CreatedAt),
                "amount" => query.OrderBy(r => r.RefundAmount),
                "amount_desc" => query.OrderByDescending(r => r.RefundAmount),
                _ => query.OrderByDescending(r => r.CreatedAt) // Default
            };

            // 6. ΣΕΛΙΔΟΠΟΙΗΣΗ
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultDto<OrderReturn>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
            };
        }
    }
}