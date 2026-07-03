using Eshop.API.Extensions;
using Eshop.API.Middleware;
using Eshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Ρύθμιση της PostgreSQL για το MasterDbContext
var masterConnectionString = builder.Configuration.GetConnectionString("MasterConnection");
builder.Services.AddDbContext<MasterDbContext>(options => options.UseNpgsql(masterConnectionString));
builder.Services.AddDbContext<ApplicationDbContext>();

// 2. Clean Architecture Extensions (Dependency Injection & Security)
builder.Services.AddEshopServices(); // όλα τα AddScoped!
builder.Services.AddEshopSecurityAndSwagger(builder.Configuration); // JWT

builder.Services.AddAutoMapper(typeof(Eshop.Application.DTOs.MappingProfile));
builder.Services.AddControllers();

var app = builder.Build();

// 3. Automated Enterprise Migrations
await app.ApplyTenantMigrationsAsync();

// 4. HTTP Pipeline Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Σε σχόλιο για το local stripe testing
app.UseRouting();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStripeWebhook(); // Το Minimal API Webhook μας

app.Run();

