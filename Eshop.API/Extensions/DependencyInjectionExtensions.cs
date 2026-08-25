using Eshop.API.BackgroundServices;
using Eshop.API.Services;
using Eshop.Application.Interfaces;
using Eshop.Application.Services;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Eshop.Infrastructure.Interceptors;
using Eshop.Infrastructure.Repositories;
using Eshop.Infrastructure.Services;
using Eshop.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Eshop.API.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddEshopServices(this IServiceCollection services)
        {
            // Tenancy & Core
            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<TenantApplicationService>();
            services.AddScoped<ITenantDatabaseService, TenantDatabaseService>();
            services.AddScoped<ITenantProvider, TenantProvider>();

            // Business Services & Repositories
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<ICouponRepository, CouponRepository>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
            services.AddScoped<IProductReviewService, ProductReviewService>();
            services.AddScoped<IWishlistRepository, WishlistRepository>();
            services.AddScoped<IWishlistService, WishlistService>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IOrderReturnRepository, OrderReturnRepository>();
            services.AddScoped<IOrderReturnService, OrderReturnService>();
            services.AddScoped<ISystemAuthService, SystemAuthService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<AuditLogInterceptor>();
            services.AddScoped<IExportService, ExportService>();

            // Stripe & Payments
            services.AddScoped<Eshop.Application.Payments.StripePaymentStrategy>();
            services.AddScoped<Eshop.Core.Interfaces.IPaymentStrategy, Eshop.Application.Payments.StripePaymentStrategy>();
            services.AddScoped<Eshop.Core.Interfaces.IPaymentStrategyFactory, Eshop.Application.Payments.PaymentStrategyFactory>();

            // Background Workers
            services.AddHostedService<PaymentTimeoutWorker>();
            services.AddHostedService<AuditLogCleanupService>();
            services.AddHostedService<Eshop.Infrastructure.Services.TokenCleanupService>();

            return services;
        }
    }
}