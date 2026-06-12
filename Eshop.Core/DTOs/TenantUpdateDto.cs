namespace Eshop.Core.DTOs
{
    public class TenantUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}