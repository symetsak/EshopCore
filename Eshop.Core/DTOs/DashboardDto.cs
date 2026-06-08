namespace Eshop.Core.DTOs
{
    public class AdminDashboardDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<TopProductDto> TopProducts { get; set; } = new List<TopProductDto>();
        public List<CategoryRevenueDto> RevenueByCategory { get; set; } = new List<CategoryRevenueDto>();
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
    }

    public class CategoryRevenueDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
    }
}

