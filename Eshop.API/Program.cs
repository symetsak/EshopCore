using Eshop.API.Middleware;
using Eshop.Application.Services;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Eshop.Infrastructure.Repositories;
using Eshop.Infrastructure.Services;
using Eshop.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Ρύθμιση της PostgreSQL για το MasterDbContext
var masterConnectionString = builder.Configuration.GetConnectionString("MasterConnection");
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseNpgsql(masterConnectionString));

// 2. Εγγραφή των Dependencies 
// Λέμε στο .NET: Όταν κάποιος ζητάει το ITenantRepository, δώσε του το TenantRepository από το Infrastructure
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
// Λέμε στο .NET πώς να κατασκευάζει το Service του Application Layer
builder.Services.AddScoped<TenantApplicationService>();
builder.Services.AddScoped<ITenantDatabaseService, TenantDatabaseService>();
// Ο TenantProvider πρέπει να είναι Scoped (ένας ανά HTTP Request)
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// Λέμε στον AutoMapper να ψάξει να βρει όλα τα Profiles στο Application layer
builder.Services.AddAutoMapper(typeof(Eshop.Application.DTOs.MappingProfile));

// Δηλώνουμε το ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>();

// 3. Προσθήκη Controllers και Swagger για τις δοκιμές μας
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MultiTenant Eshop API", Version = "v1" });

    // Προσθήκη του παγκόσμιου φίλτρου για το Header
    c.OperationFilter<AddTenantHeaderOperationFilter>();
});

var app = builder.Build();

// 4. Ενεργοποίηση του Swagger στο Development Mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();