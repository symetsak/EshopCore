using System.Text.Json.Serialization;

namespace Eshop.Core.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; } // Η τιμή προσφοράς. Μπορεί να είναι null.
        public DateTime? SaleStartDate { get; set; } // Πότε ξεκινάει η προσφορά
        public DateTime? SaleEndDate { get; set; }   // Πότε λήγει η προσφορά
        public int DiscountPercentage
        {
            get
            {
                if (!SalePrice.HasValue || Price == 0 || SalePrice >= Price)
                    return 0;

                // Υπολογισμός: ((Αρχική - Νέα) / Αρχική) * 100
                var percentage = ((Price - SalePrice.Value) / Price) * 100;
                return (int)Math.Round(percentage);
            }
        }
        public decimal CurrentPrice
        {
            get
            {
                var now = DateTime.UtcNow;
                // Αν υπάρχει τιμή προσφοράς και είμαστε εντός των ημερομηνιών, δώσε την προσφορά
                if (SalePrice.HasValue &&
                    (!SaleStartDate.HasValue || now >= SaleStartDate.Value) &&
                    (!SaleEndDate.HasValue || now <= SaleEndDate.Value))
                {
                    return SalePrice.Value;
                }

                // Διαφορετικά, επέστρεψε την κανονική τιμή
                return Price;
            }
        }
        public int StockQuantity { get; set; }

        // Foreign Key για την Κατηγορία
        public int CategoryId { get; set; }
        public string? ImageUrl { get; set; }

        [JsonIgnore]
        public Category? Category { get; set; }
    }
}