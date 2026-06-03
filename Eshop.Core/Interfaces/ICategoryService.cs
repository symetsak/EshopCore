using Eshop.Core.DTOs;

namespace Eshop.Core.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllCategoriesAsync();
        Task<CategoryResponseDto?> GetCategoryByIdAsync(int id);
        Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto dto);
        Task<CategoryResponseDto?> UpdateCategoryAsync(int id, CategoryCreateDto dto);
        Task<bool> DeleteCategoryAsync(int id);
    }
}