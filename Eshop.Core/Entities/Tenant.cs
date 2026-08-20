namespace Eshop.Core.Entities
{
    public class Tenant
    {
        public string Id { get; set; } = string.Empty; // π.χ. "nicks-shoes"
        public string Name { get; set; } = string.Empty; // π.χ. "Nick's Shoe Store"
        public string ConnectionString { get; set; } = string.Empty; // Η βάση του
        public bool IsActive { get; set; } = true;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public decimal Balance { get; set; } = 0;
        public string? Notes { get; set; }
        public ICollection<TenantTransaction> Transactions { get; set; } = new List<TenantTransaction>();
    }
}
