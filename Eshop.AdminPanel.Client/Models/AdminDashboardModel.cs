namespace Eshop.AdminPanel.Client.Models
{
    public class AdminDashboardModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<TopProductModel> TopProducts { get; set; } = new();
        public List<CategoryRevenueModel> RevenueByCategory { get; set; } = new();
        public int PendingOrdersCount { get; set; }
        public int PendingReturnsCount { get; set; }
        public int LowStockProductsCount { get; set; }
    }

    public class TopProductModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
    }

    public class CategoryRevenueModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
    }
}