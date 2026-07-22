using AutoMapper;
using Eshop.Core.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICustomerRepository _customerRepo; // Για ανάσυρση των default στοιχείων διεύθυνσης του πελάτη
        private readonly IProductRepository _productRepo; // Για έλεγχο και ενημέρωση του Stock
        private readonly IMapper _mapper;
        private readonly INotificationRepository _notificationRepo;
        private readonly IEshopNotificationService _notificationService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IOrderReturnRepository _returnRepo;

        public OrderService(IOrderRepository orderRepo, ICustomerRepository customerRepo, IProductRepository productRepo, IMapper mapper, INotificationRepository notificationRepo, IEshopNotificationService notificationService, ITenantProvider tenantProvider, IOrderReturnRepository returnRepo)
        {
            _orderRepo = orderRepo;
            _customerRepo = customerRepo;
            _productRepo = productRepo;
            _mapper = mapper;
            _notificationRepo = notificationRepo;
            _notificationService = notificationService;
            _tenantProvider = tenantProvider;
            _returnRepo = returnRepo;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(int customerId, OrderCreateDto dto)
        {
            if (dto.OrderItems == null || !dto.OrderItems.Any())
            {
                throw new InvalidOperationException("Το καλάθι αγορών είναι άδειο.");
            }

            // Φέρνουμε τον πελάτη από τη βάση για να διαβάσουμε τα default στοιχεία διεύθυνσης
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Ο πελάτης με ID {customerId} δεν βρέθηκε.");
            }

            // 1. ΔΥΝΑΜΙΚΟΣ ΚΑΘΟΡΙΣΜΟΣ STATUS ΒΑΣΕΙ ΤΡΟΠΟΥ ΠΛΗΡΩΜΗΣ
            // Αν είναι Κάρτα πάει κατευθείαν "Paid", αλλιώς (Αντικαταβολή) μένει "Pending"
            string initialStatus = dto.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase)
                ? "PendingPayment"
                : "Pending";

            // 2. Δημιουργία του βασικού αντικειμένου της Παραγγελίας
            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                Status = initialStatus,
                PaymentMethod = dto.PaymentMethod,
                TotalAmount = 0,

                // Fallback Logic Διεύθυνσης: Αν στάλθηκε νέα διεύθυνση από το Checkout τη χρησιμοποιούμε, αλλιώς παίρνουμε τη default του Customer
                Street = !string.IsNullOrWhiteSpace(dto.Street) ? dto.Street : customer.Street,
                StreetNumber = !string.IsNullOrWhiteSpace(dto.StreetNumber) ? dto.StreetNumber : customer.StreetNumber,
                City = !string.IsNullOrWhiteSpace(dto.City) ? dto.City : customer.City,
                ZipCode = !string.IsNullOrWhiteSpace(dto.ZipCode) ? dto.ZipCode : customer.ZipCode
            };

            decimal totalAmount = 0;

            // 3. Επεξεργασία του κάθε προϊόντος στο καλάθι
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

            // 4. Ανάθεση του τελικού ποσού και αποθήκευση στην PostgreSQL
            order.TotalAmount = dto.OverrideTotalAmount ?? totalAmount;

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            // 5. ΔΙΑΦΟΡΟΠΟΙΗΣΗ ΜΗΝΥΜΑΤΟΣ ΥΠΟΔΟΧΗΣ ΒΑΣΕΙ STATUS
            string welcomeTitle = initialStatus == "PendingPayment" ? "Η παραγγελία σας είναι έτοιμη για πληρωμή!" : "Η παραγγελία σας καταχωρήθηκε!";
            string welcomeMessage = initialStatus == "PendingPayment"
                ? $"Η παραγγελία #{order.Id} δημιουργήθηκε. Μεταφέρεστε στο ασφαλές περιβάλλον της Stripe για την ολοκλήρωση (Ποσό: {totalAmount}€)."
                : $"Λάβαμε την παραγγελία σας #{order.Id} συνολικής αξίας {totalAmount}€ με αντικαταβολή. Ευχαριστούμε!";

            // ΑΥΤΟΜΑΤΗ ΕΙΔΟΠΟΙΗΣΗ: Επιτυχής Καταχώρηση Παραγγελίας
            var welcomeNotification = new Notification
            {
                CustomerId = customerId,
                Title = welcomeTitle,
                Message = welcomeMessage,
                Type = "Order",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepo.AddAsync(welcomeNotification);
            await _notificationRepo.SaveChangesAsync();

            // REAL-TIME ΕΙΔΟΠΟΙΗΣΗ ΜΕΣΩ SIGNALR ΣΤΟΝ CUSTOMER
            await _notificationService.SendToCustomerAsync(_tenantProvider.TenantId!, customerId, welcomeTitle, welcomeMessage, new { orderId = order.Id, status = initialStatus });

            // 6. Mapping στο Response DTO για να το γυρίσουμε στο frontend
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
            // Λίστα με τα επιτρεπόμενα Status Παραγγελίας
            var allowedStatuses = new[] { "Pending", "PendingPayment", "Paid", "Shipped", "Completed", "Cancelled", "Refunded", "CancellationRequested" };

            if (!allowedStatuses.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Μη έγκυρο Status παραγγελίας. Οι επιτρεπόμενες τιμές είναι: {string.Join(", ", allowedStatuses)}");
            }

            // 1. Φέρνουμε την παραγγελία μαζί με τα items και τα προϊόντα τους
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return null;

            string oldStatus = order.Status;
            string newStatus = dto.Status;

            // Κανόνας 1:Περιορισμός ακύρωσης μόνο από Pending ή Paid
            if (newStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                if (oldStatus.Equals("Shipped", StringComparison.OrdinalIgnoreCase) || oldStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Δεν μπορείτε να ακυρώσετε μια παραγγελία που έχει ήδη αποσταλεί ή ολοκληρωθεί. Ο πελάτης πρέπει να ξεκινήσει διαδικασία επιστροφής (Return) αφού την παραλάβει.");
                }
            }

            //Κανόνας 2: Μετάβαση σε Refunded μόνο αν ήταν Cancelled
            if (newStatus.Equals("Refunded", StringComparison.OrdinalIgnoreCase) && !oldStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Μια παραγγελία μπορεί να πάει σε κατάσταση 'Refunded' μόνο αν έχει ήδη ακυρωθεί ('Cancelled').");
            }

            // Κανόνας 3: Διαχείρηση Αποθέματος και συμπεριφοράς πληρωμών
            string notificationTitle = "Ενημέρωση Παραγγελίας";
            string notificationMessage = $"Η κατάσταση της παραγγελίας σας #{order.Id} άλλαξε σε: {newStatus}.";

            if (newStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) && !oldStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                // Το stock επιστρέφει ΠΑΝΤΑ (είτε Card είτε CashOnDelivery)
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.StockQuantity += item.Quantity;
                        _productRepo.Update(item.Product);
                    }
                }

                // Διαφοροποίηση μηνύματος βάσει τρόπου πληρωμής
                if (order.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase))
                {
                    notificationTitle = "Το αίτημα ακύρωσης εγκρίθηκε!";
                    notificationMessage = $"Η παραγγελία σας #{order.Id} ακυρώθηκε. Τα χρήματά σας θα επιστραφούν εντός 14 ημερών.";
                }
                else
                {
                    notificationTitle = "Το αίτημα ακύρωσης εγκρίθηκε!";
                    notificationMessage = $"Η παραγγελία σας #{order.Id} ακυρώθηκε επιτυχώς.";
                }
            }
            else if (newStatus.Equals("CancellationRequested", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Λάβαμε το αίτημα ακύρωσης";
                notificationMessage = $"Το αίτημά σας για την ακύρωση της παραγγελίας #{order.Id} εξετάζεται από το κατάστημα. Θα ενημερωθείτε σύντομα.";

                // Δημιουργία και αποθήκευση της ειδοποίησης του Admin
                var adminNotification = new Notification
                {
                    CustomerId = null, // Πηγαίνει στον Admin 
                    Title = "Νέο Αίτημα Ακύρωσης Παραγγελίας!",
                    Message = $"Ο πελάτης ζήτησε ακύρωση για την παραγγελία #{order.Id} (Τρόπος Πληρωμής: {order.PaymentMethod}). Παρακαλούμε ελέγξτε την αποθήκη.",
                    Type = "AdminAlert",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _notificationRepo.AddAsync(adminNotification);
            }
            else if (newStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Η πληρωμή εγκρίθηκε!";
                notificationMessage = $"Η πληρωμή για την παραγγελία #{order.Id} ολοκληρώθηκε επιτυχώς! Ξεκινάμε τη συσκευασία.";
            }
            else if (newStatus.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Η παραγγελία σας απεστάλη!";
                notificationMessage = $"Μεγάλα νέα! Η παραγγελία σας #{order.Id} παραδόθηκε στο courier.";
            }
            else if (newStatus.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Η επιστροφή χρημάτων ολοκληρώθηκε!";
                notificationMessage = $"Τα χρήματα για την ακυρωμένη παραγγελία #{order.Id} έχουν επιστραφούν επιτυχώς στον λογαριασμό σας.";
            }

            // 3. Ενημέρωση του Status
            order.Status = newStatus;
            await _orderRepo.SaveChangesAsync();

            // 4. Αποστολή ειδοποίησης
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

            // 5. Αποστολή real-time ειδοποίησης στον σωστό αποδέκτη μέσω SignalR
            if (newStatus.Equals("CancellationRequested", StringComparison.OrdinalIgnoreCase))
            {
                // Αν είναι αίτημα ακύρωσης, το SignalR ειδοποιεί live ΜΟΝΟ τους Admins του Tenant
                await _notificationService.SendToAdminsAsync(_tenantProvider.TenantId!, "Νέο Αίτημα Ακύρωσης Παραγγελίας!", $"Ο πελάτης ζήτησε ακύρωση για την παραγγελία #{order.Id}.", new { orderId = order.Id });
            }
            else
            {
                // Για όλες τις άλλες αλλαγές status (Shipped, Paid, κτλ), ενημερώνεται live ΜΟΝΟ ο Customer
                await _notificationService.SendToCustomerAsync(_tenantProvider.TenantId!, order.CustomerId, notificationTitle, notificationMessage, new { orderId = order.Id, newStatus = newStatus });
            }

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
                .Where(item => item.Product != null && item.Product.Category != null) // Διασφαλίζουμε ότι υπάρχουν τα navigation properties
                .GroupBy(item => item.Product!.Category!.Name) // Παίρνουμε απευθείας το όνομα της Κατηγορίας
                .Select(group => new CategoryRevenueDto
                {
                    CategoryName = group.Key,
                    TotalRevenue = group.Sum(item => item.Quantity * item.UnitPrice)
                })
                .Where(c => c.TotalRevenue > 0)
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();

            // ΥΠΟΛΟΓΙΣΜΟΣ ΝΕΩΝ ΜΕΤΡΙΚΩΝ ΓΙΑ ΥΠΑΛΛΗΛΟΥΣ ΚΑΙ ADMINS
            var pendingOrders = activeOrders.Count(o => o.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || o.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase));

            var allProducts = await _productRepo.GetAllAsync();
            var lowStockCount = allProducts.Count(p => p.StockQuantity <= 5);

            var allReturns = await _returnRepo.GetAllReturnsAsync();
            var pendingReturns = allReturns.Count(r => r.Status.Equals("Requested", StringComparison.OrdinalIgnoreCase) || r.Status.Equals("Received", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"Total active order items: {activeOrders.SelectMany(o => o.OrderItems).Count()}");
            Console.WriteLine($"Items with Product loaded: {activeOrders.SelectMany(o => o.OrderItems).Count(i => i.Product != null)}");
            Console.WriteLine($"Items with Category loaded: {activeOrders.SelectMany(o => o.OrderItems).Count(i => i.Product?.Category != null)}");

            // 5. Επιστροφή του τελικού, πλήρους DTO
            return new AdminDashboardDto
            {
                TotalRevenue = totalRevenue,
                TotalOrdersCount = totalOrdersCount,
                AverageOrderValue = Math.Round(averageOrderValue, 2),
                TopProducts = topProducts,
                RevenueByCategory = revenueByCategory,
                PendingOrdersCount = pendingOrders,
                PendingReturnsCount = pendingReturns,
                LowStockProductsCount = lowStockCount
            };
        }
        public async Task<OrderResponseDto?> GetOrderDetailsForAdminAsync(int orderId) =>
            _mapper.Map<OrderResponseDto>(await _orderRepo.GetByIdWithItemsAsync(orderId));
    }
}