using AutoMapper;
using Eshop.API.Middleware;
using Eshop.Application.Services;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Eshop.Infrastructure.Repositories;
using Eshop.Infrastructure.Services;
using Eshop.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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

builder.Services.AddScoped<Eshop.Core.Interfaces.IProductRepository, Eshop.Infrastructure.Repositories.ProductRepository>();
builder.Services.AddScoped<Eshop.Core.Interfaces.IProductService, Eshop.Application.Services.ProductService>();
builder.Services.AddScoped<Eshop.Core.Interfaces.IUserRepository, Eshop.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<Eshop.Core.Interfaces.IUserService, Eshop.Application.Services.UserService>();
builder.Services.AddScoped<Eshop.Core.Interfaces.ICategoryRepository, Eshop.Infrastructure.Repositories.CategoryRepository>();
builder.Services.AddScoped<Eshop.Core.Interfaces.ICategoryService, Eshop.Application.Services.CategoryService>();
builder.Services.AddScoped<Eshop.Core.Interfaces.ICustomerRepository, Eshop.Infrastructure.Repositories.CustomerRepository>();
builder.Services.AddScoped<Eshop.Core.Interfaces.ICustomerService, Eshop.Application.Services.CustomerService>();
builder.Services.AddScoped<Eshop.Core.Interfaces.IOrderRepository, Eshop.Infrastructure.Repositories.OrderRepository>();
builder.Services.AddScoped<Eshop.Core.Interfaces.IOrderService, Eshop.Application.Services.OrderService>();
builder.Services.AddScoped<Eshop.Core.Interfaces.IFileService, Eshop.Infrastructure.Services.FileService>();
builder.Services.AddScoped<Eshop.Core.Interfaces.ICartRepository, Eshop.Infrastructure.Repositories.CartRepository>();
builder.Services.AddScoped<Eshop.Core.Interfaces.ICartService, Eshop.Application.Services.CartService>();
builder.Services.AddScoped<Eshop.Core.Interfaces.ICouponRepository, Eshop.Infrastructure.Repositories.CouponRepository>();
builder.Services.AddScoped<Eshop.Core.Interfaces.ICouponService, Eshop.Application.Services.CouponService>();


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

/*
// ΤΕΣΤ ΣΥΓΚΕΝΤΡΩΣΗΣ AUTOMAPPER
using (var scope = app.Services.CreateScope())
{
    var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
    try
    {
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
        Console.WriteLine("Το AutoMapper είναι 100% ΕΓΚΥΡΟ!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("ΣΦΑΛΜΑ ΣΤΟ AUTOMAPPER:");
        Console.WriteLine(ex.Message);
        // throw; // Αν ξεσχολιάσεις το throw, το app θα κρασάρει στην εκκίνηση δείχνοντάς σου το λάθος!
    }
}
*/

// ΑΥΤΟΜΑΤΟΠΟΙΗΣΗ MIGRATIONS ΓΙΑ ΟΛΟΥΣ ΤΟΥΣ TENANTS (ENTERPRISE FLOW)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Έναρξη αυτόματων migrations για τη Master Βάση...");
        var masterContext = services.GetRequiredService<MasterDbContext>();
        await masterContext.Database.MigrateAsync(); // Ενημερώνει τη Master βάση αν έχει εκκρεμότητες

        var tenantRepo = services.GetRequiredService<ITenantRepository>();
        var tenants = await tenantRepo.GetAllAsync(); // Παίρνει όλους τους tenants από τη Master

        logger.LogInformation("Βρέθηκαν {Count} Tenants. Έναρξη migrations για τις βάσεις τους...", tenants.Count());

        foreach (var tenant in tenants)
        {
            if (string.IsNullOrEmpty(tenant.ConnectionString)) continue;

            logger.LogInformation("Εκτέλεση Migration για τον Tenant: {TenantId}...", tenant.Id);

            // Δημιουργούμε ένα dynamic instance του ApplicationDbContext ειδικά γι' αυτό το connection string
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(tenant.ConnectionString);

            using (var tenantContext = new ApplicationDbContext(optionsBuilder.Options))
            {
                await tenantContext.Database.MigrateAsync(); // ΤΡΕΧΕΙ ΤΟ MIGRATION ΣΤΗ ΒΑΣΗ ΤΟΥ TENANT!
            }
        }

        logger.LogInformation("Όλα τα migrations εκτελέστηκαν επιτυχώς με επιτυχία!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Προέκυψε σοβαρό σφάλμα κατά την εκτέλεση των αυτόματων migrations!");
    }
}

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