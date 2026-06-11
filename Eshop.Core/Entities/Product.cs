using System.Text.Json.Serialization;

namespace Eshop.Core.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        // Foreign Key για την Κατηγορία
        public int CategoryId { get; set; }
        public string? ImageUrl { get; set; }

        [JsonIgnore]
        public Category? Category { get; set; }
    }
}