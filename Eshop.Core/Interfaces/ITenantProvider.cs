namespace Eshop.Core.Interfaces
{
    public interface ITenantProvider
    {
        string? TenantId { get; set; }
        string? ConnectionString { get; set; }
    }
}
