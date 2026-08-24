using System.Collections.Generic;

namespace Eshop.Core.DTOs
{
    public class ProductFilterDto
    {
        // 1. Αναζήτηση κειμένου
        public string? SearchTerm { get; set; }

        // 2. Φίλτρο Κατηγοριών (Λίστα για πολλαπλή επιλογή!)
        public List<int>? CategoryIds { get; set; }

        // 3. Εύρος Τιμών
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinSalePrice { get; set; }
        public decimal? MaxSalePrice { get; set; }

        // 4. Ταξινόμηση (π.χ. "price_asc", "price_desc", "newest", "bestsellers")
        public string? SortBy { get; set; }

        // 5. Σελιδοποίηση (με default τιμές αν δεν στείλει το frontend)
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12; // 12 προϊόντα ανά σελίδα είναι standard για grid
    }
}
