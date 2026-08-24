using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
        }

        public void Update(Product product)
        {
            _context.Products.Update(product);
        }

        public void Delete(Product product)
        {
            _context.Products.Remove(product);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResultDto<Product>> GetPagedProductsAsync(ProductFilterDto filter)
        {
            // Ξεκινάμε το Query. Το Include φέρνει και την κατηγορία για να έχουμε το όνομά της
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // 1. ΦΙΛΤΡΟ: Αναζήτηση Κειμένου (Όνομα ή Περιγραφή)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search) ||
                                         (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            // 2. ΦΙΛΤΡΟ: Πολλαπλή Επιλογή Κατηγοριών (CategoryIds)
            if (filter.CategoryIds != null && filter.CategoryIds.Any())
            {
                // Κρατάει μόνο τα προϊόντα που το CategoryId τους υπάρχει στη λίστα που έστειλε το frontend
                query = query.Where(p => filter.CategoryIds.Contains(p.CategoryId));
            }

            // 3. ΦΙΛΤΡΟ: Εύρος Τιμών (Min & Max Price)
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            // 3.5. ΠΡΟΣΘΗΚΗ: ΦΙΛΤΡΟ: Εύρος Τιμής Προσφοράς (Min & Max Sale Price)
            if (filter.MinSalePrice.HasValue)
            {
                query = query.Where(p => p.SalePrice >= filter.MinSalePrice.Value);
            }
            if (filter.MaxSalePrice.HasValue)
            {
                query = query.Where(p => p.SalePrice <= filter.MaxSalePrice.Value);
            }

            // 4. ΤΑΞΙΝΟΜΗΣΗ (Sorting)
            query = filter.SortBy?.ToLower() switch
            {
                // ΠΡΟΣΘΗΚΗ: Ταξινομήσεις από το Blazor Frontend
                "name" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "saleprice" => query.OrderBy(p => p.SalePrice),
                "saleprice_desc" => query.OrderByDescending(p => p.SalePrice),
                "stockquantity" => query.OrderBy(p => p.StockQuantity),
                "stockquantity_desc" => query.OrderByDescending(p => p.StockQuantity),

                "newest" => query.OrderByDescending(p => p.Id), // Οι νέες αφίξεις έχουν μεγαλύτερο ID

                // Advanced: Ταξινόμηση Best Sellers με βάση τα OrderItems των ολοκληρωμένων παραγγελιών
                "bestsellers" => query.OrderByDescending(p => _context.OrderItems
                                        .Where(oi => oi.ProductId == p.Id && oi.Order.Status != "Cancelled")
                                        .Sum(oi => oi.Quantity)),

                // Default ταξινόμηση αν δεν στείλει τίποτα το frontend (αλφαβητικά)
                _ => query.OrderBy(p => p.Name)
            };

            // 5. ΣΕΛΙΔΟΠΟΙΗΣΗ (Pagination)
            // Πρώτα μετράμε πόσα προϊόντα βρέθηκαν ΣΥΝΟΛΙΚΑ με αυτά τα φίλτρα
            var totalCount = await query.CountAsync();

            // Εφαρμόζουμε το Skip και Take για να πάρουμε ΜΟΝΟ τα προϊόντα της συγκεκριμένης σελίδας
            // Παράδειγμα: Αν PageNumber = 2 και PageSize = 12, κάνουμε Skip(12) και Take(12)
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Υπολογισμός συνολικών σελίδων (π.χ. 25 προϊόντα / 12 ανά σελίδα = 3 σελίδες)
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            // 6. Επιστροφή του πακέτου
            return new PagedResultDto<Product>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}