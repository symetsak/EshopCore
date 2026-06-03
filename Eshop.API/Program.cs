using Eshop.API.Middleware;
using Eshop.Application.Services;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Eshop.Infrastructure.Repositories;
using Eshop.Infrastructure.Services;
using Eshop.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
// --- ΡΥΘΜΙΣΗ JWT AUTHENTICATION ---
var jwtKey = builder.Configuration["JwtSettings:Secret"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
        ClockSkew = TimeSpan.FromMinutes(5) // 5 λεπτά ανοχή χρόνου
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MultiTenant Eshop API", Version = "v1" });

    // 1. Ορίζουμε το σύστημα ασφαλείας (JWT Bearer) για το Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Κάνε επικόλληση ΜΟΝΟ το Token σου στο πεδίο Value."
    });

    // 2. Λέμε στο Swagger να εφαρμόσει αυτό το σύστημα παγκόσμια
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Προσθήκη του παγκόσμιου φίλτρου για το Header (Το δικό σου!)
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
app.UseRouting();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();