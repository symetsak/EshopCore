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
    }
}