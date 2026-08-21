using Eshop.API.Extensions;
using Eshop.API.Hubs;
using Eshop.API.Middleware;
using Eshop.API.Services;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Services;
using Eshop.Infrastructure.Data;
using Eshop.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// 1. Ρύθμιση της PostgreSQL για το MasterDbContext (ΜΕ RESILIENCY)
var masterConnectionString = builder.Configuration.GetConnectionString("MasterConnection");
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseNpgsql(masterConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));

// 2. Εγγραφή του ApplicationDbContext (ΜΕ ΤΟΝ INTERCEPTOR)
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AuditLogInterceptor>();
    options.AddInterceptors(interceptor);
});

builder.Services.AddScoped<IMasterDbContext>(provider => provider.GetRequiredService<MasterDbContext>());

// 3. Clean Architecture Extensions (Dependency Injection & Security)
builder.Services.AddEshopServices();
builder.Services.AddEshopSecurityAndSwagger(builder.Configuration);
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
              .SetIsOriginAllowed(origin => true)
              .AllowCredentials();
    });
});

builder.Services.AddHttpContextAccessor();

// Ενεργοποίηση Health Checks και αυτόματος έλεγχος της Master DB
builder.Services.AddHealthChecks().AddDbContextCheck<MasterDbContext>("MasterDatabase").AddCheck<TenantDatabasesHealthCheck>("TenantDatabases");
var app = builder.Build();

// 4. Automated Enterprise Migrations
await app.ApplyTenantMigrationsAsync();

// 5. HTTP Pipeline Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolverMiddleware>();
app.MapControllers();
app.MapStripeWebhook();
app.MapHub<NotificationHub>("/api/notificationhub");
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();