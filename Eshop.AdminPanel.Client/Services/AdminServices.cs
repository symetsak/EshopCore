using Eshop.AdminPanel.Client.Models;
using System.Net.Http.Json;

namespace Eshop.AdminPanel.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        public AuthService(HttpClient http) => _http = http;

        public async Task<HttpResponseMessage> LoginAsync(LoginModel model)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/Users/login");
            request.Content = JsonContent.Create(new { username = model.Username, password = model.Password });
            request.Headers.Add("X-Tenant-Id", model.TenantId.ToLower().Trim());
            return await _http.SendAsync(request);
        }

        // Διαγράψαμε το tenantId, ο Handler αναλαμβάνει!
        public async Task<HttpResponseMessage> LogoutAsync(string refreshToken)
        {
            return await _http.PostAsJsonAsync("api/Users/logout", new { RefreshToken = refreshToken });
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordModel model)
        {
            // Ο Handler θα βάλει αυτόματα το Tenant ID και το Bearer Token!
            return await _http.PostAsJsonAsync("api/users/change-password", new
            {
                username = model.Username,
                currentPassword = model.CurrentPassword,
                newPassword = model.NewPassword
            });
        }
    }

    public class DashboardService : IDashboardService
    {
        private readonly HttpClient _http;
        public DashboardService(HttpClient http) => _http = http;

        public async Task<AdminDashboardModel?> GetStatsAsync() =>
            await _http.GetFromJsonAsync<AdminDashboardModel>("api/Orders/admin/dashboard");
    }

    public class OrderService : IOrderService
    {
        private readonly HttpClient _http;
        public OrderService(HttpClient http) => _http = http;

        public async Task<List<OrderResponseDto>> GetOrdersAsync() =>
            await _http.GetFromJsonAsync<List<OrderResponseDto>>("api/orders/admin/all") ?? new();

        public async Task<HttpResponseMessage> UpdateOrderStatusAsync(int orderId, string status) =>
            await _http.PutAsJsonAsync($"api/orders/admin/{orderId}/status", new { Status = status });
    }

    public class ProductService : IProductService
    {
        private readonly HttpClient _http;
        public ProductService(HttpClient http) => _http = http;

        public async Task<PagedResultModel<ProductResponseModel>?> GetProductsAsync(int pageNumber, int pageSize, string search)
        {
            var url = $"api/Products?PageNumber={pageNumber}&PageSize={pageSize}";
            if (!string.IsNullOrEmpty(search)) url += $"&SearchTerm={Uri.EscapeDataString(search)}";
            return await _http.GetFromJsonAsync<PagedResultModel<ProductResponseModel>>(url);
        }

        public async Task<HttpResponseMessage> CreateProductAsync(ProductCreateUpdateModel model) => await _http.PostAsJsonAsync("api/Products", model);
        public async Task<HttpResponseMessage> UpdateProductAsync(int id, ProductCreateUpdateModel model) => await _http.PutAsJsonAsync($"api/Products/{id}", model);
        public async Task<HttpResponseMessage> DeleteProductAsync(int id) => await _http.DeleteAsync($"api/Products/{id}");
        public async Task<HttpResponseMessage> UpdateDiscountAsync(int id, UpdateProductDiscountModel model) => await _http.PutAsJsonAsync($"api/Products/{id}/discounts", model);
        public async Task<HttpResponseMessage> DeleteDiscountAsync(int id) => await _http.DeleteAsync($"api/Products/{id}/discounts");

        public async Task<HttpResponseMessage> UploadImageAsync(int id, MultipartFormDataContent content) => await _http.PostAsync($"api/Products/{id}/image", content);
        public async Task<HttpResponseMessage> DeleteImageAsync(int id) => await _http.DeleteAsync($"api/Products/{id}/image");
    }

    public class CategoryService : ICategoryService
    {
        private readonly HttpClient _http;
        public CategoryService(HttpClient http) => _http = http;

        public async Task<List<CategoryDto>> GetCategoriesAsync() => await _http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? new();
        public async Task<HttpResponseMessage> CreateCategoryAsync(CategoryModel model) => await _http.PostAsJsonAsync("api/Categories", model);
        public async Task<HttpResponseMessage> UpdateCategoryAsync(int id, CategoryModel model) => await _http.PutAsJsonAsync($"api/Categories/{id}", model);
        public async Task<HttpResponseMessage> DeleteCategoryAsync(int id) => await _http.DeleteAsync($"api/categories/{id}");
    }

    public class CouponService : ICouponService
    {
        private readonly HttpClient _http;
        public CouponService(HttpClient http) => _http = http;

        public async Task<List<CouponDto>> GetCouponsAsync() => await _http.GetFromJsonAsync<List<CouponDto>>("api/Coupons") ?? new();
        public async Task<HttpResponseMessage> CreateCouponAsync(object payload) => await _http.PostAsJsonAsync("api/Coupons", payload);
        public async Task<HttpResponseMessage> UpdateCouponAsync(int id, object payload) => await _http.PutAsJsonAsync($"api/Coupons/{id}", payload);
        public async Task<HttpResponseMessage> DeleteCouponAsync(int id) => await _http.DeleteAsync($"api/Coupons/{id}");
    }

    public class ReturnService : IReturnService
    {
        private readonly HttpClient _http;
        public ReturnService(HttpClient http) => _http = http;

        public async Task<List<OrderReturnResponseDto>?> GetReturnsAsync() => await _http.GetFromJsonAsync<List<OrderReturnResponseDto>>("api/admin/returns");
        public async Task<HttpResponseMessage> UpdateReturnStatusAsync(int returnId, string status) => await _http.PutAsJsonAsync($"api/admin/returns/{returnId}/status", new OrderReturnStatusUpdateDto { Status = status });
    }

    public class UserService : IUserService
    {
        private readonly HttpClient _http;
        public UserService(HttpClient http) => _http = http;

        public async Task<List<UserClientDto>> GetUsersAsync() => await _http.GetFromJsonAsync<List<UserClientDto>>("api/Users/all") ?? new();
        public async Task<HttpResponseMessage> CreateUserAsync(object payload) => await _http.PostAsJsonAsync("api/Users/create", payload);
        public async Task<HttpResponseMessage> UpdateUserAsync(int id, object payload) => await _http.PutAsJsonAsync($"api/Users/update/{id}", payload);
        public async Task<HttpResponseMessage> DeleteUserAsync(int id) => await _http.DeleteAsync($"api/Users/{id}");
        public async Task<List<UserNoteDto>> GetUserNotesAsync(int userId) => await _http.GetFromJsonAsync<List<UserNoteDto>>($"api/Users/{userId}/notes") ?? new();
        public async Task<HttpResponseMessage> AddMyNoteAsync(string content) => await _http.PostAsJsonAsync("api/Users/my-notes", content);
    }

    public class NotificationService : INotificationService
    {
        private readonly HttpClient _http;
        public NotificationService(HttpClient http) => _http = http;

        public async Task<List<NotificationClientDto>> GetNotificationsAsync() => await _http.GetFromJsonAsync<List<NotificationClientDto>>("api/notifications/adminNotifications") ?? new();
        public async Task<HttpResponseMessage> MarkAsReadAsync(int id) => await _http.PutAsync($"api/notifications/admin/{id}/read", null);
    }
}