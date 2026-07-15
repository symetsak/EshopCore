namespace Eshop.Core.DTOs
{
    public class ProductCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
    }

    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public int DiscountPercentage { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? SaleStartDate { get; set; } 
        public DateTime? SaleEndDate { get; set; }   
        public bool IsOnSale => SalePrice.HasValue && Price < OriginalPrice; 
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class UpdateProductDiscountDto
    {
        public decimal? SalePrice { get; set; }
        public int? DiscountPercentage { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
    }
}
