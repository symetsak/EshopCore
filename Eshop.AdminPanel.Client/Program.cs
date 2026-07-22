using Blazored.LocalStorage;
using Eshop.AdminPanel.Client;
using Eshop.AdminPanel.Client.Security;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;

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

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

// ΤΟ Core ΤΟΥ SECURITY
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>(sp => (CustomAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

await builder.Build().RunAsync();