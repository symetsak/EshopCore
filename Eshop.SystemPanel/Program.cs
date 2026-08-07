using Blazored.LocalStorage;
using Eshop.SystemPanel;
using Eshop.SystemPanel.Providers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5284/") });

// 1. Αυτό για το LocalStorage
builder.Services.AddBlazoredLocalStorage();

// 2. Αυτό για τον μηχανισμό Authorize
builder.Services.AddAuthorizationCore();

// 3. Αυτό για τον "Φρουρό" που φτιάξαμε
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();
