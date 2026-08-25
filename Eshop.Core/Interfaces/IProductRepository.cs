using Eshop.Core.DTOs;
using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task AddAsync(Product product);
        void Update(Product product);
        void Delete(Product product);
        Task SaveChangesAsync();
        Task<PagedResultDto<Product>> GetPagedProductsAsync(ProductFilterDto filter);
        Task<List<Product>> GetProductsForExportAsync(ProductFilterDto filter);
    }
}