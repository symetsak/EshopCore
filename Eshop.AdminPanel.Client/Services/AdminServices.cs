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
            var request = new HttpRequestMessage(HttpMethod.Post, "api/users/change-password");

            request.Content = JsonContent.Create(new
            {
                username = model.Username,
                currentPassword = model.CurrentPassword,
                newPassword = model.NewPassword
            });

            // Το προσθέτουμε ξανά χειροκίνητα παίρνοντάς το από τη φόρμα!
            request.Headers.Add("X-Tenant-Id", model.TenantId.ToLower().Trim());

            return await _http.SendAsync(request);
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

        // ΠΡΟΣΘΗΚΗ: Η νέα μέθοδος που δέχεται φίλτρα, σελιδοποίηση και ταξινόμηση
        public async Task<PagedResultModel<OrderResponseDto>?> GetPagedOrdersAsync(int pageNumber, int pageSize, string? searchTerm, DateTime? minDate, DateTime? maxDate, List<string>? statuses, List<string>? paymentMethods, string? sortBy)
        {
            var queryParams = new List<string>
            {
                $"PageNumber={pageNumber}",
                $"PageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"SearchTerm={Uri.EscapeDataString(searchTerm)}");

            if (minDate.HasValue)
                queryParams.Add($"MinDate={minDate.Value:yyyy-MM-dd}");

            if (maxDate.HasValue)
                queryParams.Add($"MaxDate={maxDate.Value:yyyy-MM-dd}");

            if (statuses != null && statuses.Any())
            {
                foreach (var status in statuses)
                    queryParams.Add($"Statuses={Uri.EscapeDataString(status)}");
            }

            if (paymentMethods != null && paymentMethods.Any())
            {
                foreach (var method in paymentMethods)
                    queryParams.Add($"PaymentMethods={Uri.EscapeDataString(method)}");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"SortBy={Uri.EscapeDataString(sortBy)}");

            var queryString = string.Join("&", queryParams);
            var url = $"api/orders/admin/all?{queryString}"; // Το ανανεωμένο endpoint μας!

            return await _http.GetFromJsonAsync<PagedResultModel<OrderResponseDto>>(url) ?? new PagedResultModel<OrderResponseDto>();
        }

        public async Task<HttpResponseMessage> UpdateOrderStatusAsync(int orderId, string status) =>
            await _http.PutAsJsonAsync($"api/orders/admin/{orderId}/status", new { Status = status });
    }

    public class ProductService : IProductService
    {
        private readonly HttpClient _http;
        public ProductService(HttpClient http) => _http = http;

        public async Task<PagedResultModel<ProductResponseModel>?> GetProductsAsync(int pageNumber, int pageSize, string? searchString, int? categoryId, decimal? minPrice, decimal? maxPrice, decimal? minSalePrice, decimal? maxSalePrice, string? sortBy)
        {
            // Ξεκινάμε να χτίζουμε τις παραμέτρους του URL
            var queryParams = new List<string>
            {
                $"PageNumber={pageNumber}",
                $"PageSize={pageSize}"
            };

            // Προσθέτουμε μόνο όσα φίλτρα έχει επιλέξει ο χρήστης (δεν είναι null)
            if (!string.IsNullOrWhiteSpace(searchString))
                queryParams.Add($"SearchTerm={Uri.EscapeDataString(searchString)}");

            if (categoryId.HasValue)
                queryParams.Add($"CategoryIds={categoryId.Value}");

            if (minPrice.HasValue)
                queryParams.Add($"MinPrice={minPrice.Value}");

            if (maxPrice.HasValue)
                queryParams.Add($"MaxPrice={maxPrice.Value}");

            if (minSalePrice.HasValue)
                queryParams.Add($"MinSalePrice={minSalePrice.Value}");

            if (maxSalePrice.HasValue)
                queryParams.Add($"MaxSalePrice={maxSalePrice.Value}");

            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"SortBy={Uri.EscapeDataString(sortBy)}");

            // Ενώνουμε τις παραμέτρους με το σύμβολο '&'
            var queryString = string.Join("&", queryParams);
            var url = $"api/Products?{queryString}"; // Το τελικό URL

            // Κάνουμε το HTTP Request στο Backend
            return await _http.GetFromJsonAsync<PagedResultModel<ProductResponseModel>>(url) ?? new PagedResultModel<ProductResponseModel>();
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

        // Η νέα μέθοδος που δέχεται φίλτρα, σελιδοποίηση και ταξινόμηση
        public async Task<PagedResultModel<OrderReturnResponseDto>?> GetPagedReturnsAsync(int pageNumber, int pageSize, string? searchTerm, DateTime? minDate, DateTime? maxDate, List<string>? statuses, List<string>? returnTypes, string? sortBy)
        {
            var queryParams = new List<string>
            {
                $"PageNumber={pageNumber}",
                $"PageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"SearchTerm={Uri.EscapeDataString(searchTerm)}");

            if (minDate.HasValue)
                queryParams.Add($"MinDate={minDate.Value:yyyy-MM-dd}");

            if (maxDate.HasValue)
                queryParams.Add($"MaxDate={maxDate.Value:yyyy-MM-dd}");

            if (statuses != null && statuses.Any())
            {
                foreach (var status in statuses)
                    queryParams.Add($"Statuses={Uri.EscapeDataString(status)}");
            }

            if (returnTypes != null && returnTypes.Any())
            {
                foreach (var type in returnTypes)
                    queryParams.Add($"ReturnTypes={Uri.EscapeDataString(type)}");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"SortBy={Uri.EscapeDataString(sortBy)}");

            var queryString = string.Join("&", queryParams);
            var url = $"api/admin/returns?{queryString}"; // Το endpoint του API μας

            return await _http.GetFromJsonAsync<PagedResultModel<OrderReturnResponseDto>>(url) ?? new PagedResultModel<OrderReturnResponseDto>();
        }

        public async Task<HttpResponseMessage> UpdateReturnStatusAsync(int returnId, string status) =>
            await _http.PutAsJsonAsync($"api/admin/returns/{returnId}/status", new OrderReturnStatusUpdateDto { Status = status });
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