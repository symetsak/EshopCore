using AutoMapper;
using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo; // Για έλεγχο και ενημέρωση του Stock
        private readonly IMapper _mapper;

        public OrderService(IOrderRepository orderRepo, IProductRepository productRepo, IMapper mapper)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _mapper = mapper;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(int customerId, OrderCreateDto dto)
        {
            if (dto.OrderItems == null || !dto.OrderItems.Any())
            {
                throw new InvalidOperationException("Το καλάθι αγορών είναι άδειο.");
            }

            // 1. Δημιουργία του βασικού αντικειμένου της Παραγγελίας
            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = 0 // Θα το υπολογίσουμε δυναμικά παρακάτω
            };

            decimal totalAmount = 0;

            // 2. Επεξεργασία του κάθε προϊόντος στο καλάθι
            foreach (var itemDto in dto.OrderItems)
            {
                // Τραβάμε το προϊόν από τη βάση του Tenant
                var product = await _productRepo.GetByIdAsync(itemDto.ProductId);
                if (product == null)
                {
                    throw new KeyNotFoundException($"Το προϊόν με ID {itemDto.ProductId} δεν βρέθηκε.");
                }

                // ΕΛΕΓΧΟΣ STOCK: Έχουμε αρκετά κομμάτια;
                if (product.StockQuantity < itemDto.Quantity)
                {
                    throw new InvalidOperationException($"Ανεπαρκές απόθεμα για το προϊόν '{product.Name}'. Διαθέσιμο απόθεμα: {product.StockQuantity}.");
                }

                // ΜΕΙΩΣΗ STOCK: Αφαιρούμε τα κομμάτια από το κατάστημα
                product.StockQuantity -= itemDto.Quantity;
                _productRepo.Update(product);

                // Δημιουργία του OrderItem (της γραμμής παραγγελίας)
                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price // Κλειδώνουμε την τρέχουσα τιμή αγοράς
                };

                // Υπολογισμός μερικού και συνολικού ποσού
                totalAmount += orderItem.Quantity * orderItem.UnitPrice;

                // Προσθήκη στην παραγγελία
                order.OrderItems.Add(orderItem);
            }

            // 3. Ανάθεση του τελικού ποσού και αποθήκευση στην PostgreSQL
            order.TotalAmount = totalAmount;

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            // 4. Mapping στο Response DTO για να το γυρίσουμε στο frontend
            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return null;
            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetCustomerOrdersAsync(int customerId)
        {
            var orders = await _orderRepo.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllTenantOrdersAsync()
        {
            var orders = await _orderRepo.GetAllOrdersAsync();
            return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
        }
    }
}