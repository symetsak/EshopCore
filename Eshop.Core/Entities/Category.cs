namespace Eshop.Core.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        // Σχέση: Μια κατηγορία έχει πολλά προϊόντα
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}