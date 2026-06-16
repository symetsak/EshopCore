using Eshop.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace Eshop.Core.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync();
        Task<ProductResponseDto?> GetProductByIdAsync(int id);
        Task<ProductResponseDto> CreateProductAsync(ProductCreateDto dto);
        Task<ProductResponseDto?> UpdateProductAsync(int id, ProductCreateDto dto);
        Task<bool> DeleteProductAsync(int id);
        Task<PagedResultDto<ProductResponseDto>> GetFilteredProductsAsync(ProductFilterDto filter);
        Task<ProductResponseDto> UploadImageAsync(int productId, IFormFile file, string tenantId);
        Task<ProductResponseDto> DeleteProductImageAsync(int productId);
        Task ApplyDiscountAsync(int productId, UpdateProductDiscountDto dto);
    }
}