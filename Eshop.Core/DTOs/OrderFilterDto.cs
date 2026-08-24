namespace Eshop.Core.DTOs
{
    public class OrderFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? SearchTerm { get; set; }

        // Φίλτρα Ημερομηνίας
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }

        // Φίλτρα λίστας (ώστε ο χρήστης να μπορεί να επιλέξει π.χ. Card ΚΑΙ CashOnDelivery μαζί)
        public List<string>? Statuses { get; set; }
        public List<string>? PaymentMethods { get; set; }

        public string? SortBy { get; set; }
    }
}