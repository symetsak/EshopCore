using System;
using System.Collections.Generic;

namespace Eshop.Core.Entities
{
    public class OrderReturn
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int CustomerId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        // "Total" για Ολική, "Partial" για Μερική
        public string ReturnType { get; set; } = "Total";

        // Workflow States: Requested, Received, Approved/Rejected, Refunded
        public string Status { get; set; } = "Requested";

        public decimal RefundAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Οι γραμμές της επιστροφής (ποια προϊόντα επιστρέφονται)
        public ICollection<OrderReturnItem> ReturnItems { get; set; } = new List<OrderReturnItem>();

        // Το IBAN του πελάτη (υποχρεωτικό ΜΟΝΟ αν η παραγγελία ήταν με CashOnDelivery)
        public string? Iban { get; set; }
    }
}