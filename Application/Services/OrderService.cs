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
        private readonly INotificationRepository _notificationRepo;

        public OrderService(IOrderRepository orderRepo, IProductRepository productRepo, IMapper mapper, INotificationRepository notificationRepo)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _mapper = mapper;
            _notificationRepo = notificationRepo;
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
                TotalAmount = 0 
            };

            decimal totalAmount = 0;

            // 2. Επεξεργασία του κάθε προϊόντος στο καλάθι
            foreach (var itemDto in dto.OrderItems)
            {
                // Τραβάμε το προϊόν από τη βάση του Tenant
                var product = await _productRepo.GetByIdAsync(itemDto.ProductId);
                if (product == null) throw new KeyNotFoundException($"Το προϊόν με ID {itemDto.ProductId} δεν βρέθηκε.");

                // ΕΛΕΓΧΟΣ STOCK: Έχουμε αρκετά κομμάτια;
                if (product.StockQuantity < itemDto.Quantity) throw new InvalidOperationException($"Ανεπαρκές απόθεμα για το προϊόν '{product.Name}'. Διαθέσιμο απόθεμα: {product.StockQuantity}.");

                // ΜΕΙΩΣΗ STOCK: Αφαιρούμε τα κομμάτια από το κατάστημα
                product.StockQuantity -= itemDto.Quantity;
                _productRepo.Update(product);

                // Δημιουργία του OrderItem (της γραμμής παραγγελίας)
                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.SalePrice ?? product.Price // Κλειδώνουμε την τρέχουσα τιμή αγοράς
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

            // ΑΥΤΟΜΑΤΗ ΕΙΔΟΠΟΙΗΣΗ: Επιτυχής Καταχώρηση Παραγγελίας
            var welcomeNotification = new Notification
            {
                CustomerId = customerId,
                Title = "Η παραγγελία σας καταχωρήθηκε!",
                Message = $"Λάβαμε την παραγγελία σας #{order.Id} συνολικής αξίας {totalAmount}€. Ευχαριστούμε για την εμπιστοσύνη σας!",
                Type = "Order",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepo.AddAsync(welcomeNotification);
            await _notificationRepo.SaveChangesAsync();

            // 4. Mapping στο Response DTO για να το γυρίσουμε στο frontend
            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            return order == null ? null : _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetCustomerOrdersAsync(int customerId) =>
            _mapper.Map<IEnumerable<OrderResponseDto>>(await _orderRepo.GetByCustomerIdAsync(customerId));

        public async Task<IEnumerable<OrderResponseDto>> GetAllTenantOrdersAsync() =>
            _mapper.Map<IEnumerable<OrderResponseDto>>(await _orderRepo.GetAllOrdersAsync());

        public async Task<OrderResponseDto?> UpdateOrderStatusAsync(int orderId, OrderStatusUpdateDto dto)
        {
            // 1. Φέρνουμε την παραγγελία μαζί με τα items και τα προϊόντα τους
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return null;

            string oldStatus = order.Status;
            string newStatus = dto.Status;

            // 2. Business Logic: Αν η παραγγελία ακυρώνεται, επιστρέφουμε το Stock!
            if (newStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
                !oldStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.StockQuantity += item.Quantity; // Επιστροφή στοκ
                        _productRepo.Update(item.Product);
                    }
                }
            }

            // 3. Ενημέρωση του Status
            order.Status = newStatus;
            await _orderRepo.SaveChangesAsync();

            // ΣΕΝΑΡΙΟ 2: ΑΥΤΟΜΑΤΗ ΕΙΔΟΠΟΙΗΣΗ - Αλλαγή Status από τον Admin
            string notificationTitle = "Ενημέρωση Παραγγελίας";
            string notificationMessage = $"Η κατάσταση της παραγγελίας σας #{order.Id} άλλαξε σε: {newStatus}.";

            if (newStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Η πληρωμή εγκρίθηκε!";
                notificationMessage = $"Η πληρωμή για την παραγγελία #{order.Id} ολοκληρώθηκε επιτυχώς! Ξεκινάμε τη συσκευασία.";
            }
            else if (newStatus.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Η παραγγελία σας απεστάλη!";
                notificationMessage = $"Μεγάλα νέα! Η παραγγελία σας #{order.Id} παραδόθηκε στο courier και έρχεται προς τα εσένα.";
            }
            else if (newStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Η παραγγελία ακυρώθηκε";
                notificationMessage = $"Η παραγγελία σας #{order.Id} έχει ακυρωθεί επιτυχώς.";
            }

            var statusNotification = new Notification
            {
                CustomerId = order.CustomerId,
                Title = notificationTitle,
                Message = notificationMessage,
                Type = "Order",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _notificationRepo.AddAsync(statusNotification);
            await _notificationRepo.SaveChangesAsync();

            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<AdminDashboardDto> GetAdminDashboardStatsAsync()
        {
            // 1. Φέρνουμε όλες τις παραγγελίες του Tenant
            var orders = await _orderRepo.GetAllOrdersAsync();

            // Φιλτράρουμε τις ενεργές παραγγελίες (όχι Cancelled)
            var activeOrders = orders.Where(o => !o.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)).ToList();

            // 2. Υπολογισμός βασικών στατιστικών
            var totalRevenue = activeOrders.Sum(o => o.TotalAmount);
            var totalOrdersCount = orders.Count();
            var activeOrdersCount = activeOrders.Count();

            // Υπολογισμός Μέσης Αξίας Παραγγελίας (Average Order Value)
            decimal averageOrderValue = activeOrdersCount > 0 ? totalRevenue / activeOrdersCount : 0;

            // 3. Advanced LINQ: Εύρεση των Top 3 Προϊόντων
            var topProducts = activeOrders
                .SelectMany(o => o.OrderItems)
                .GroupBy(item => new { item.ProductId, ProductName = item.Product != null ? item.Product.Name : "Άγνωστο Προϊόν" })
                .Select(group => new TopProductDto
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    TotalQuantitySold = group.Sum(item => item.Quantity),
                    TotalRevenueGenerated = group.Sum(item => item.Quantity * item.UnitPrice)
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(3)
                .ToList();

            // 4. ΝΕΟ Advanced LINQ: Τζίρος ανά Κατηγορία Προϊόντος
            var revenueByCategory = activeOrders
                .SelectMany(o => o.OrderItems)
                .GroupBy(item => item.Product != null && item.Product.Category != null ? item.Product.Category.Name : "Χωρίς Κατηγορία")
                .Select(group => new CategoryRevenueDto
                {
                    CategoryName = group.Key,
                    TotalRevenue = group.Sum(item => item.Quantity * item.UnitPrice)
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();

            // 5. Επιστροφή του τελικού, πλήρους DTO
            return new AdminDashboardDto
            {
                TotalRevenue = totalRevenue,
                TotalOrdersCount = totalOrdersCount,
                AverageOrderValue = Math.Round(averageOrderValue, 2), // Στρογγυλοποίηση σε 2 δεκαδικά
                TopProducts = topProducts,
                RevenueByCategory = revenueByCategory
            };
        }
        public async Task<OrderResponseDto?> GetOrderDetailsForAdminAsync(int orderId) =>
            _mapper.Map<OrderResponseDto>(await _orderRepo.GetByIdWithItemsAsync(orderId));
    }
}