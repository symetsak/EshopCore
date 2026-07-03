using System;
using System.Collections.Generic;

namespace Eshop.Application.DTOs
{
    // ΤΟ REQUEST DTO (Αυτό που στέλνει το Frontend)
    public class OrderReturnRequestDto
    {
        public int OrderId { get; set; }
        public string Title { get; set; } = null!;
        public string Reason { get; set; } = null!;
        // "Total" (Ολική) ή "Partial" (Μερική)
        public string ReturnType { get; set; } = "Total";
        // Αν είναι "Partial", εδώ θα έρχονται τα επιλεγμένα προϊόντα με τα checkboxes
        public List<OrderReturnItemRequestDto> ReturnItems { get; set; } = new();
        // Υποχρεωτικό στο business logic ΜΟΝΟ αν η παραγγελία ήταν με αντικαταβολή
        public string? Iban { get; set; }
    }

    // Sub-DTO για τα προϊόντα που επιλέγονται στη μερική επιστροφή
    public class OrderReturnItemRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // ΤΟ RESPONSE DTO (Αυτό που γυρίζει πίσω)
    public class OrderReturnResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string Title { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string ReturnType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal RefundAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<OrderReturnItemResponseDto> ReturnItems { get; set; } = new();
        public string? Iban { get; set; }
    }

    // Sub-DTO για την εμφάνιση των επιστραμμένων προϊόντων στη λίστα
    public class OrderReturnItemResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // DTO ΓΙΑ ΤΗΝ ΑΛΛΑΓΗ STATUS (Από τον Admin)
    public class OrderReturnStatusUpdateDto
    {
        // Received, Approved, Rejected, Refunded
        public string Status { get; set; } = null!;
    }
}