using Eshop.API.Extensions;
using Eshop.API.Hubs;
using Eshop.API.Middleware;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Eshop.Core.Interfaces;
using Eshop.API.Services;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Ρύθμιση της PostgreSQL για το MasterDbContext
var masterConnectionString = builder.Configuration.GetConnectionString("MasterConnection");
builder.Services.AddDbContext<MasterDbContext>(options => options.UseNpgsql(masterConnectionString));
builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddScoped<IMasterDbContext>(provider => provider.GetRequiredService<MasterDbContext>());

// 2. Clean Architecture Extensions (Dependency Injection & Security)
builder.Services.AddEshopServices(); // όλα τα AddScoped!
builder.Services.AddEshopSecurityAndSwagger(builder.Configuration); // JWT

builder.Services.AddAutoMapper(typeof(Eshop.Application.DTOs.MappingProfile));
builder.Services.AddControllers();

builder.Services.AddSignalR();
builder.Services.AddTransient<IEshopNotificationService, EshopNotificationService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true) // Επιτρέπει τη σύνδεση από τοπικά αρχεία και localhost
              .AllowCredentials(); 
    });
});

var app = builder.Build();

// 3. Automated Enterprise Migrations
await app.ApplyTenantMigrationsAsync();

// 4. HTTP Pipeline Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();
// app.UseHttpsRedirection(); // Σε σχόλιο για το local stripe testing
app.UseRouting();
app.UseCors();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolverMiddleware>();

app.MapControllers();
app.MapStripeWebhook(); // Το Minimal API Webhook μας
app.MapHub<NotificationHub>("/api/notificationhub");

app.Run();

