namespace Eshop.Core.DTOs
{
    public class UpdateΤenantDetailsDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
    }
}