using System;

namespace Eshop.Core.Entities
{
    public class Coupon
    {
        public int Id { get; set; }

        // Ο κωδικός που θα πληκτρολογεί ο χρήστης (π.χ. SUMMER20, XMAS10)
        public string Code { get; set; } = string.Empty;

        // Τύπος έκπτωσης: "Percentage" (π.χ. 20%) ή "FixedAmount" (π.χ. 10€)
        public string DiscountType { get; set; } = "Percentage";

        // Η αξία της έκπτωσης (π.χ. 20.00 ή 10.00)
        public decimal DiscountValue { get; set; }

        // Ελάχιστο ποσό καλαθιού για να ισχύει το κουπόνι (π.χ. "Ισχύει για αγορές άνω των 50€")
        public decimal MinimumSubTotalRequired { get; set; } = 0;

        // Ημερομηνίες έναρξης και λήξης
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Αν το κουπόνι είναι ενεργό γενικότερα
        public bool IsActive { get; set; } = true;
    }
}