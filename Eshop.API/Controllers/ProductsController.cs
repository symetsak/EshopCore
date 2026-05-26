using AutoMapper;
using Eshop.Application.DTOs;
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
        private readonly IMapper _mapper;

        // Κάνουμε inject το DbContext του πελάτη
        public ProductsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 1. GET: api/Products (Φέρνει όλα τα προϊόντα του συγκεκριμένου Tenant)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
        {
            var products = await _context.Products.ToListAsync();

            //Ο AutoMapper μετατρέπει όλη τη λίστα αυτόματα!
            var response = _mapper.Map<IEnumerable<ProductResponseDto>>(products);

            return Ok(products);
        }

        // 2. POST: api/Products (Αποθηκεύει ένα προϊόν στη βάση του συγκεκριμένου Tenant)
        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct(ProductCreateDto dto)
        {
            //Μετατροπή του DTO σε Entity με μία γραμμή
            var product = _mapper.Map<Product>(dto);

            // Προσωρινά βάζουμε CategoryId = 1 για το τεστ, ή βεβαιώσου ότι έχεις φτιάξει κατηγορία με ID 1
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            //Μετατροπή του Entity πίσω σε Response DTO για την απάντηση
            var responseDto = _mapper.Map<ProductResponseDto>(product);

            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, responseDto);
        }
    }
}
