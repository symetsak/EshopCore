namespace Eshop.Core.DTOs
{
    public class CategoryCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}