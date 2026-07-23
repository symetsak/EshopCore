using System.ComponentModel.DataAnnotations;

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

    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Το όνομα του καταστήματος είναι υποχρεωτικό.")]
        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Το όνομα χρήστη είναι υποχρεωτικό.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο τωρινός κωδικός είναι υποχρεωτικός.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο νέος κωδικός είναι υποχρεωτικός.")]
        [MinLength(6, ErrorMessage = "Ο νέος κωδικός πρέπει να είναι τουλάχιστον 6 χαρακτήρες.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Η επιβεβαίωση κωδικού είναι υποχρεωτική.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Οι κωδικοί δεν ταιριάζουν.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Το όνομα χρήστη (Username) είναι υποχρεωτικό.")]
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο κωδικός πρόσβασης είναι υποχρεωτικός.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Το όνομα του καταστήματος (Tenant) είναι υποχρεωτικό.")]
        public string TenantId { get; set; } = string.Empty; // π.χ. adidas-store
    }

    public class ProductResponseModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal? SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
    }

    public class ProductCreateUpdateModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
    }

    public class UpdateProductDiscountModel
    {
        public int? DiscountPercentage { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
    }

    // Για τη σελιδοποίηση από το GetFilteredProducts
    public class PagedResultModel<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class CategoryModel
    {
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class UserModel
    {
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class OrderReturnUIModel : OrderReturnResponseDto
    {
        public bool ShowDetails { get; set; }
    }

}
