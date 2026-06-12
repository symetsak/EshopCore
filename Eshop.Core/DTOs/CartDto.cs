using System;
using System.Collections.Generic;
using System.Linq;

namespace Eshop.Core.DTOs
{
    // 1. Αυτό που στέλνει το Frontend για προσθήκη
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    // 2. Η γραμμή του καλαθιού που επιστρέφει το API
    public class CartItemResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Price * Quantity;
    }

    // 3. Ολόκληρο το καλάθι που επιστρέφει το API
    public class CartResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public List<CartItemResponseDto> Items { get; set; } = new();
        public decimal SubTotal => Items.Sum(item => item.TotalPrice);
        public decimal Discount { get; set; } = 0; // ΕΔΩ ΘΑ ΜΠΕΙ ΤΟ ΚΟΥΠΟΝΙ ΑΡΓΟΤΕΡΑ
        public decimal Total => SubTotal - Discount;
    }
}