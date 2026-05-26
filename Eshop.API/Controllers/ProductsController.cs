using Eshop.Core.Entities;
using Eshop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eshop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Κάνουμε inject το DbContext του πελάτη
        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Products (Φέρνει όλα τα προϊόντα του συγκεκριμένου Tenant)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        // 2. POST: api/Products (Αποθηκεύει ένα προϊόν στη βάση του συγκεκριμένου Tenant)
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            // Προσωρινά βάζουμε CategoryId = 1 για το τεστ, ή βεβαιώσου ότι έχεις φτιάξει κατηγορία με ID 1
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
        }
    }
}
