using AutoMapper;
using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Eshop.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo; // <-- Μόνο με το Interface του Core!
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public ProductService(IProductRepository productRepo, IMapper mapper, IFileService fileService)
        {
            _productRepo = productRepo;
            _mapper = mapper;
            _fileService = fileService;
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync()
        {
            var products = await _productRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductResponseDto>>(products);
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return null;

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<ProductResponseDto> CreateProductAsync(ProductCreateDto dto)
        {
            var product = _mapper.Map<Product>(dto);

            await _productRepo.AddAsync(product);
            await _productRepo.SaveChangesAsync();

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<ProductResponseDto?> UpdateProductAsync(int id, ProductCreateDto dto)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return null;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;

            _productRepo.Update(product);
            await _productRepo.SaveChangesAsync();

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return false;

            _productRepo.Delete(product);
            await _productRepo.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResultDto<ProductResponseDto>> GetFilteredProductsAsync(ProductFilterDto filter)
        {
            // 1. Καλούμε το Repository για να μας φέρει τη σελιδοποιημένη λίστα με τα Product Entities
            var pagedProducts = await _productRepo.GetPagedProductsAsync(filter);

            // 2. Μετατρέπουμε (Map) τα Product Entities σε ProductResponseDtos
            var productDtos = _mapper.Map<IEnumerable<ProductResponseDto>>(pagedProducts.Items);

            // 3. Επιστρέφουμε το νέο PagedResultDto, αλλά αυτή τη φορά με τα DTOs μέσα!
            return new PagedResultDto<ProductResponseDto>
            {
                Items = productDtos,
                PageNumber = pagedProducts.PageNumber,
                PageSize = pagedProducts.PageSize,
                TotalCount = pagedProducts.TotalCount,
                TotalPages = pagedProducts.TotalPages
            };
        }

        public async Task<ProductResponseDto> UploadImageAsync(int productId, IFormFile file, string tenantId)
        {
            // 1. Φέρνουμε το Entity από το Repository
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
                throw new KeyNotFoundException("Το προϊόν δεν βρέθηκε.");

            // 2. Σώζουμε τη νέα εικόνα μέσω του FileService
            var imageUrl = await _fileService.SaveProductImageAsync(file, tenantId);

            // 3. Αν υπήρχε παλιά εικόνα, τη σβήνουμε από τον δίσκο για να μην γεμίζει ο server σκουπίδια
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                _fileService.DeleteImage(product.ImageUrl);
            }

            // 4. Ενημερώνουμε το Entity και κάνουμε Save στο Repository
            product.ImageUrl = imageUrl;
            _productRepo.Update(product);
            await _productRepo.SaveChangesAsync();

            // 5. Επιστρέφουμε το ενημερωμένο DTO στο Frontend
            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<ProductResponseDto> DeleteProductImageAsync(int productId)
        {
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
                throw new KeyNotFoundException("Το προϊόν δεν βρέθηκε.");

            // Αν υπάρχει εικόνα, τη σβήνουμε από τον σκληρό δίσκο
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                _fileService.DeleteImage(product.ImageUrl);
            }

            // Κάνουμε το πεδίο null στη βάση
            product.ImageUrl = null;
            _productRepo.Update(product);
            await _productRepo.SaveChangesAsync(); 

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task ApplyDiscountAsync(int productId, UpdateProductDiscountDto dto)
        {
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
            {
                throw new KeyNotFoundException("Το προϊόν δεν βρέθηκε.");
            }

            decimal finalSalePrice = 0;

            // 💡 Σενάριο Α: Ο Admin έβαλε Ποσοστό Έκπτωσης (π.χ. 15%)
            if (dto.DiscountPercentage.HasValue && dto.DiscountPercentage.Value > 0)
            {
                if (dto.DiscountPercentage.Value >= 100)
                {
                    throw new InvalidOperationException("Η έκπτωση δεν μπορεί να είναι 100% ή παραπάνω.");
                }

                // Υπολογισμός τιμής: Αρχική - (Αρχική * (Ποσοστό / 100))
                var discountAmount = product.Price * ((decimal)dto.DiscountPercentage.Value / 100);
                finalSalePrice = product.Price - discountAmount;
            }
            // 💡 Σενάριο Β: Ο Admin έβαλε κατευθείαν Τιμή Προσφοράς (π.χ. 80€)
            else if (dto.SalePrice.HasValue && dto.SalePrice.Value > 0)
            {
                if (dto.SalePrice.Value >= product.Price)
                {
                    throw new InvalidOperationException("Η τιμή προσφοράς πρέπει να είναι μικρότερη από την αρχική τιμή.");
                }
                finalSalePrice = dto.SalePrice.Value;
            }
            else
            {
                throw new InvalidOperationException("Πρέπει να καταχωρήσετε είτε Τιμή Προσφοράς είτε Ποσοστό Έκπτωσης.");
            }

            // Αποθήκευση στο Entity
            product.SalePrice = Math.Round(finalSalePrice, 2); // Στρογγυλοποίηση στα 2 δεκαδικά
            product.SaleStartDate = dto.SaleStartDate.HasValue ? DateTime.SpecifyKind(dto.SaleStartDate.Value, DateTimeKind.Utc) : null;
            product.SaleEndDate = dto.SaleEndDate.HasValue ? DateTime.SpecifyKind(dto.SaleEndDate.Value, DateTimeKind.Utc) : null;

            _productRepo.Update(product); 
            await _productRepo.SaveChangesAsync();
        }
    }
}