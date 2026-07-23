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
        Task<List<OrderResponseDto>> GetOrdersAsync();
        Task<HttpResponseMessage> UpdateOrderStatusAsync(int orderId, string status);
    }

    public interface IProductService
    {
        Task<PagedResultModel<ProductResponseModel>?> GetProductsAsync(int pageNumber, int pageSize, string search);
        Task<HttpResponseMessage> CreateProductAsync(ProductCreateUpdateModel model);
        Task<HttpResponseMessage> UpdateProductAsync(int id, ProductCreateUpdateModel model);
        Task<HttpResponseMessage> DeleteProductAsync(int id);
        Task<HttpResponseMessage> UpdateDiscountAsync(int id, UpdateProductDiscountModel model);
        Task<HttpResponseMessage> DeleteDiscountAsync(int id);
        Task<HttpResponseMessage> UploadImageAsync(int id, MultipartFormDataContent content);
        Task<HttpResponseMessage> DeleteImageAsync(int id);
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
        Task<List<OrderReturnResponseDto>?> GetReturnsAsync();
        Task<HttpResponseMessage> UpdateReturnStatusAsync(int returnId, string status);
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