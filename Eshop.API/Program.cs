using Eshop.Application.Services;
using Eshop.Core.Interfaces;
using Eshop.Infrastructure.Data;
using Eshop.Infrastructure.Repositories;
using Eshop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Ρύθμιση της PostgreSQL για το MasterDbContext
var masterConnectionString = builder.Configuration.GetConnectionString("MasterConnection");
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseNpgsql(masterConnectionString));

// 2. Εγγραφή των Dependencies (Εδώ γίνεται η "μαγεία" του Injection!)
// Λέμε στο .NET: Όταν κάποιος ζητάει το ITenantRepository, δώσε του το TenantRepository από το Infrastructure
builder.Services.AddScoped<ITenantRepository, TenantRepository>();

// Λέμε στο .NET πώς να κατασκευάζει το Service του Application Layer
builder.Services.AddScoped<TenantApplicationService>();
builder.Services.AddScoped<ITenantDatabaseService, TenantDatabaseService>();

// 3. Προσθήκη Controllers και Swagger για τις δοκιμές μας
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Ενεργοποιεί το οπτικό περιβάλλον δοκιμών

var app = builder.Build();

// 4. Ενεργοποίηση του Swagger στο Development Mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();