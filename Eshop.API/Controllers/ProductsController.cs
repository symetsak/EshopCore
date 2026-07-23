using Eshop.API.Filters;
using Eshop.Core.DTOs;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.API.Controllers
{
    [Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredProducts([FromQuery] ProductFilterDto filter)
        {
            var result = await _productService.GetFilteredProductsAsync(filter);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Το προϊόν με ID {id} δεν βρέθηκε." });
            }
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator, Employee")]
        [TenantAuthorize]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            var newProduct = await _productService.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = newProduct.Id }, newProduct);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator, Employee")]
        [TenantAuthorize]
        public async Task<IActionResult> Update(int id, [FromBody] ProductCreateDto dto)
        {
            var updatedProduct = await _productService.UpdateProductAsync(id, dto);
            if (updatedProduct == null)
            {
                return NotFound(new { message = $"Το προϊόν με ID {id} δεν βρέθηκε για να ενημερωθεί." });
            }
            return Ok(updatedProduct);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator, Employee")]
        [TenantAuthorize]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _productService.DeleteProductAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Το προϊόν με ID {id} δεν βρέθηκε για να διαγραφεί." });
            }
            return Ok(new { message = "Το προϊόν διαγράφηκε επιτυχώς." });
        }

        [HttpPost("{id}/image")]
        [Authorize(Roles = "Administrator, Employee")]
        [TenantAuthorize]
        public async Task<IActionResult> UploadProductImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Παρακαλώ επιλέξτε μια έγκυρη εικόνα." });
            }

            var tenantId = HttpContext.Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { message = "Tenant ID is missing." });
            }

            try
            {
                // Καλούμε το Service και παίρνουμε έτοιμο το ProductResponseDto με το νέο ImageUrl μέσα!
                var updatedProduct = await _productService.UploadImageAsync(id, file, tenantId);
                return Ok(updatedProduct);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/products/{id}/image
        [HttpDelete("{id}/image")]
        [Authorize(Roles = "Administrator, Employee")]
        [TenantAuthorize]
        public async Task<IActionResult> DeleteProductImage(int id)
        {
            try
            {
                var updatedProduct = await _productService.DeleteProductImageAsync(id);
                return Ok(updatedProduct);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/discounts")]
        [Authorize(Roles = "Administrator, Employee")]
        [TenantAuthorize]
        public async Task<IActionResult> ApplyProductDiscount(int id, [FromBody] UpdateProductDiscountDto dto)
        {
            try
            {
                await _productService.ApplyDiscountAsync(id, dto);
                return Ok(new { message = "Η προσφορά εφαρμόστηκε με επιτυχία!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/discounts")]
        [Authorize(Roles = "Administrator, Employee")]
        [TenantAuthorize]
        public async Task<IActionResult> RemoveProductDiscount(int id)
        {
            try
            {
                await _productService.RemoveDiscountAsync(id);
                return Ok(new { message = "Η προσφορά αφαιρέθηκε με επιτυχία!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}