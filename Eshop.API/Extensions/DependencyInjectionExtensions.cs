using Eshop.API.BackgroundServices;
using Eshop.Application.Services;
using Eshop.Core.Interfaces;
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
            // Λέμε στο .NET: Όταν κάποιος ζητάει το ITenantRepository, δώσε του το TenantRepository από το Infrastructure
            services.AddScoped<ITenantRepository, TenantRepository>();
            // Λέμε στο .NET πώς να κατασκευάζει το Service του Application Layer
            services.AddScoped<TenantApplicationService>();
            services.AddScoped<ITenantDatabaseService, TenantDatabaseService>();
            // Ο TenantProvider πρέπει να είναι Scoped (ένας ανά HTTP Request)
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

            // Stripe & Payments
            // Κάνουμε register το συγκεκριμένο class για να μπορεί να το τραβήξει το Factory
            services.AddScoped<Eshop.Application.Payments.StripePaymentStrategy>();
            // Κάνουμε register το Strategy Interface (για γενική χρήση αν χρειαστεί)
            services.AddScoped<Eshop.Core.Interfaces.IPaymentStrategy, Eshop.Application.Payments.StripePaymentStrategy>();
            // Κάνουμε register το ίδιο το Factory
            services.AddScoped<Eshop.Core.Interfaces.IPaymentStrategyFactory, Eshop.Application.Payments.PaymentStrategyFactory>();

            // Εγγραφή του Background Worker για αυτόματη ακύρωση απλήρωτων παραγγελιών κάρτας
            services.AddHostedService<PaymentTimeoutWorker>();

            // Εγγραφή του Background Worker για αυτόματη εκαθάριση ληγμένων Refresh Tokens από τη βάση
            services.AddHostedService<Eshop.Infrastructure.Services.TokenCleanupService>();

            return services;
        }
    }
}