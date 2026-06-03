using AutoMapper;
using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IMapper _mapper;

        public CategoryService(ICategoryRepository categoryRepo, IMapper mapper)
        {
            _categoryRepo = categoryRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryResponseDto>>(categories);
        }

        public async Task<CategoryResponseDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return null;

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto dto)
        {
            var category = _mapper.Map<Category>(dto);
            await _categoryRepo.AddAsync(category);
            await _categoryRepo.SaveChangesAsync();

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<CategoryResponseDto?> UpdateCategoryAsync(int id, CategoryCreateDto dto)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return null;

            category.Name = dto.Name;
            category.DisplayOrder = dto.DisplayOrder;

            _categoryRepo.Update(category);
            await _categoryRepo.SaveChangesAsync();

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return false;

            _categoryRepo.Delete(category);
            await _categoryRepo.SaveChangesAsync();
            return true;
        }
    }
}