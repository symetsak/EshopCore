namespace Eshop.Core.DTOs
{
    public class OrderReturnFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? SearchTerm { get; set; }

        // Φίλτρα Ημερομηνίας
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }

        // Φίλτρα λίστας (Multi-select)
        public List<string>? Statuses { get; set; }
        public List<string>? ReturnTypes { get; set; }

        public string? SortBy { get; set; }
    }
}