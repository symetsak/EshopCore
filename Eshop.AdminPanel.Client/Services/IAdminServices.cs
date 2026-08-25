using Eshop.AdminPanel.Client.Models;

namespace Eshop.AdminPanel.Client.Services
{
    public interface IAuthService
    {
        Task<HttpResponseMessage> LoginAsync(LoginModel model);
        Task<HttpResponseMessage> LogoutAsync(string refreshToken);
        Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordModel model);
    }

    public interface IDashboardService
    {
        Task<AdminDashboardModel?> GetStatsAsync();
    }

    public interface IOrderService
    {
        // Το νέο Paged endpoint
        Task<PagedResultModel<OrderResponseDto>?> GetPagedOrdersAsync(int pageNumber, int pageSize, string? searchTerm, DateTime? minDate, DateTime? maxDate, List<string>? statuses, List<string>? paymentMethods, string? sortBy);
        Task<HttpResponseMessage> UpdateOrderStatusAsync(int orderId, string status);
        Task<byte[]> ExportOrdersExcelAsync(string? searchTerm, DateTime? minDate, DateTime? maxDate, List<string>? statuses, List<string>? paymentMethods, string? sortBy);
        Task<byte[]> ExportOrdersPdfAsync(string? searchTerm, DateTime? minDate, DateTime? maxDate, List<string>? statuses, List<string>? paymentMethods, string? sortBy);
    }

    public interface IProductService
    {
        Task<PagedResultModel<ProductResponseModel>?> GetProductsAsync(int pageNumber, int pageSize, string? searchString, int? categoryId, decimal? minPrice, decimal? maxPrice, decimal? minSalePrice, decimal? maxSalePrice, string? sortBy);
        Task<HttpResponseMessage> CreateProductAsync(ProductCreateUpdateModel model);
        Task<HttpResponseMessage> UpdateProductAsync(int id, ProductCreateUpdateModel model);
        Task<HttpResponseMessage> DeleteProductAsync(int id);
        Task<HttpResponseMessage> UpdateDiscountAsync(int id, UpdateProductDiscountModel model);
        Task<HttpResponseMessage> DeleteDiscountAsync(int id);
        Task<HttpResponseMessage> UploadImageAsync(int id, MultipartFormDataContent content);
        Task<HttpResponseMessage> DeleteImageAsync(int id);
        Task<byte[]> ExportProductsExcelAsync(string? searchString, int? categoryId, decimal? minPrice, decimal? maxPrice, decimal? minSalePrice, decimal? maxSalePrice, string? sortBy);
        Task<byte[]> ExportProductsPdfAsync(string? searchString, int? categoryId, decimal? minPrice, decimal? maxPrice, decimal? minSalePrice, decimal? maxSalePrice, string? sortBy);
    }

    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetCategoriesAsync();
        Task<HttpResponseMessage> CreateCategoryAsync(CategoryModel model);
        Task<HttpResponseMessage> UpdateCategoryAsync(int id, CategoryModel model);
        Task<HttpResponseMessage> DeleteCategoryAsync(int id);
    }

    public interface ICouponService
    {
        Task<List<CouponDto>> GetCouponsAsync();
        Task<HttpResponseMessage> CreateCouponAsync(object payload);
        Task<HttpResponseMessage> UpdateCouponAsync(int id, object payload);
        Task<HttpResponseMessage> DeleteCouponAsync(int id);
    }

    public interface IReturnService
    {
        Task<PagedResultModel<OrderReturnResponseDto>?> GetPagedReturnsAsync(int pageNumber, int pageSize, string? searchTerm, DateTime? minDate, DateTime? maxDate, List<string>? statuses, List<string>? returnTypes, string? sortBy);
        Task<HttpResponseMessage> UpdateReturnStatusAsync(int returnId, string status);
        Task<byte[]> ExportReturnsExcelAsync(string? searchTerm, DateTime? minDate, DateTime? maxDate, List<string>? statuses, List<string>? returnTypes, string? sortBy);
        Task<byte[]> ExportReturnsPdfAsync(string? searchTerm, DateTime? minDate, DateTime? maxDate, List<string>? statuses, List<string>? returnTypes, string? sortBy);
    }

    public interface IUserService
    {
        Task<List<UserClientDto>> GetUsersAsync();
        Task<HttpResponseMessage> CreateUserAsync(object payload);
        Task<HttpResponseMessage> UpdateUserAsync(int id, object payload);
        Task<HttpResponseMessage> DeleteUserAsync(int id);
        Task<List<UserNoteDto>> GetUserNotesAsync(int userId);
        Task<HttpResponseMessage> AddMyNoteAsync(string content);
    }

    public interface INotificationService
    {
        Task<List<NotificationClientDto>> GetNotificationsAsync();
        Task<HttpResponseMessage> MarkAsReadAsync(int id);
    }
}