using Eshop.Core.Interfaces;

namespace Eshop.Infrastructure.Tenancy
{
    // Scoped Service: Ζει όσο διαρκεί ένα HTTP Request
    public class TenantProvider : ITenantProvider
    {
        public string? TenantId { get; set; }
        public string? ConnectionString { get; set; }
    }
}