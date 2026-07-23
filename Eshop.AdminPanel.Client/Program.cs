using Blazored.LocalStorage;
using Eshop.AdminPanel.Client;
using Eshop.AdminPanel.Client.Security;
using Eshop.AdminPanel.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Καταχώρηση του Unauthorized Interceptor Handler
builder.Services.AddTransient<UnauthorizedHandler>();

// 2. Ρύθμιση HttpClient με χρήση του Handler και BaseAddress το API σου (http://localhost:5284/)
builder.Services.AddHttpClient("EshopAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5284/");
})
.AddHttpMessageHandler<UnauthorizedHandler>();

// 3. Ορισμός του προεπιλεγμένου (default) HttpClient που γίνεται @inject σε όλες τις σελίδες
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("EshopAPI"));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

// ΤΟ Core ΤΟΥ SECURITY
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>(sp => (CustomAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

await builder.Build().RunAsync();