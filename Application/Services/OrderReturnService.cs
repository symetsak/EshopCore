using AutoMapper;
using Eshop.Application.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eshop.Application.Services
{
    public class OrderReturnService : IOrderReturnService
    {
        private readonly IOrderReturnRepository _returnRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IMapper _mapper;

        public OrderReturnService(
            IOrderReturnRepository returnRepo,
            IOrderRepository orderRepo,
            IProductRepository productRepo,
            INotificationRepository notificationRepo,
            IMapper mapper)
        {
            _returnRepo = returnRepo;
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _notificationRepo = notificationRepo;
            _mapper = mapper;
        }

        // 1. ΔΗΜΙΟΥΡΓΙΑ ΑΙΤΗΜΑΤΟΣ ΕΠΙΣΤΡΟΦΗΣ (Customer)
        public async Task<OrderReturnResponseDto> CreateReturnRequestAsync(int customerId, OrderReturnRequestDto dto)
        {
            // Α) Φέρνουμε την παραγγελία μαζί με τα είδη της
            var order = await _orderRepo.GetByIdAsync(dto.OrderId);
            if (order == null || order.CustomerId != customerId)
            {
                throw new KeyNotFoundException("Η παραγγελία δεν βρέθηκε ή δεν σας ανήκει.");
            }

            // ΕΛΕΓΧΟΣ Α: Πρέπει η παραγγελία να είναι ολοκληρωμένη (Completed)
            if (!order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Δεν μπορείτε να ζητήσετε επιστροφή για μια παραγγελία που βρίσκεται σε κατάσταση: '{order.Status}'. Η παραγγελία πρέπει να έχει ολοκληρωθεί.");
            }

            // ΕΛΕΓΧΟΣ Β: Έλεγχος αν έχει γίνει ήδη αίτημα επιστροφής για αυτή την παραγγελία
            // Ψάχνουμε αν υπάρχουν ήδη επιστροφές για αυτό το OrderId
            var allReturns = await _returnRepo.GetAllReturnsAsync();
            var alreadyExists = allReturns.Any(r => r.OrderId == dto.OrderId && !r.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                throw new InvalidOperationException("Έχει ήδη υποβληθεί αίτημα επιστροφής για αυτή την παραγγελία.");
            }

            var orderReturn = new OrderReturn
            {
                OrderId = dto.OrderId,
                CustomerId = customerId,
                Title = dto.Title,
                Reason = dto.Reason,
                ReturnType = dto.ReturnType,
                Status = "Requested", // Αρχικό στάδιο workflow
                CreatedAt = DateTime.UtcNow
            };

            decimal calculatedRefundAmount = 0;

            // Β) Σενάριο 1: ΟΛΙΚΗ ΕΠΙΣΤΡΟΦΗ (Total)
            if (dto.ReturnType.Equals("Total", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var orderItem in order.OrderItems)
                {
                    var returnItem = new OrderReturnItem
                    {
                        ProductId = orderItem.ProductId,
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice
                    };
                    calculatedRefundAmount += orderItem.Quantity * orderItem.UnitPrice;
                    orderReturn.ReturnItems.Add(returnItem);
                }
            }
            // Γ) Σενάριο 2: ΜΕΡΙΚΗ ΕΠΙΣΤΡΟΦΗ (Partial)
            else
            {
                if (dto.ReturnItems == null || !dto.ReturnItems.Any())
                {
                    throw new InvalidOperationException("Πρέπει να επιλέξετε τουλάχιστον ένα προϊόν για μερική επιστροφή.");
                }

                foreach (var requestItem in dto.ReturnItems)
                {
                    // Επιβεβαιώνουμε ότι το προϊόν υπήρχε όντως στην παραγγελία
                    var originalOrderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == requestItem.ProductId);
                    if (originalOrderItem == null)
                    {
                        throw new InvalidOperationException($"Το προϊόν με ID {requestItem.ProductId} δεν περιλαμβάνεται στην παραγγελία.");
                    }

                    if (requestItem.Quantity > originalOrderItem.Quantity)
                    {
                        throw new InvalidOperationException($"Δεν μπορείτε να επιστρέψετε μεγαλύτερη ποσότητα από αυτή που αγοράσατε ({originalOrderItem.Quantity}).");
                    }

                    var returnItem = new OrderReturnItem
                    {
                        ProductId = requestItem.ProductId,
                        Quantity = requestItem.Quantity,
                        UnitPrice = originalOrderItem.UnitPrice
                    };
                    calculatedRefundAmount += requestItem.Quantity * originalOrderItem.UnitPrice;
                    orderReturn.ReturnItems.Add(returnItem);
                }
            }

            orderReturn.RefundAmount = calculatedRefundAmount;

            await _returnRepo.AddAsync(orderReturn);
            await _returnRepo.SaveChangesAsync();

            // ΑΥΤΟΜΑΤΗ ΕΙΔΟΠΟΙΗΣΗ: Επιβεβαίωση λήψης αιτήματος
            var returnNotification = new Notification
            {
                CustomerId = customerId,
                Title = "Λάβαμε το αίτημά σας για επιστροφή!",
                Message = $"Το αίτημα επιστροφής για την παραγγελία #{order.Id} καταχωρήθηκε επιτυχώς και βρίσκεται υπό εξέταση.",
                Type = "Info",
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepo.AddAsync(returnNotification);
            await _notificationRepo.SaveChangesAsync();

            return _mapper.Map<OrderReturnResponseDto>(orderReturn);
        }

        public async Task<IEnumerable<OrderReturnResponseDto>> GetCustomerReturnsAsync(int customerId) =>
            _mapper.Map<IEnumerable<OrderReturnResponseDto>>(await _returnRepo.GetByCustomerIdAsync(customerId));

        public async Task<IEnumerable<OrderReturnResponseDto>> GetAllReturnsAsync() =>
            _mapper.Map<IEnumerable<OrderReturnResponseDto>>(await _returnRepo.GetAllReturnsAsync());

        public async Task<OrderReturnResponseDto?> GetReturnByIdAsync(int id) =>
            _mapper.Map<OrderReturnResponseDto>(await _returnRepo.GetByIdWithItemsAsync(id));

        // 2. ΔΙΑΧΕΙΡΙΣΗ WORKFLOW STATUS (Admin)
        public async Task<OrderReturnResponseDto?> UpdateReturnStatusAsync(int returnId, OrderReturnStatusUpdateDto dto)
        {
            // Λίστα με τα επιτρεπόμενα Status Επιστροφής
            var allowedReturnStatuses = new[] { "Requested", "Received", "Approved", "Rejected", "Refunded" };

            if (!allowedReturnStatuses.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Μη έγκυρο Status επιστροφής. Οι επιτρεπόμενες τιμές είναι: {string.Join(", ", allowedReturnStatuses)}");
            }

            var orderReturn = await _returnRepo.GetByIdWithItemsAsync(returnId);
            if (orderReturn == null) return null;

            string oldStatus = orderReturn.Status;
            string newStatus = dto.Status;

            // ΚΡΙΣΙΜΟ BUSINESS LOGIC: Αν το status γίνει "Approved", γυρνάμε τις ποσότητες στην αποθήκη!
            if (newStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) &&
                !oldStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var returnItem in orderReturn.ReturnItems)
                {
                    var product = await _productRepo.GetByIdAsync(returnItem.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += returnItem.Quantity; // Επιστροφή στοκ στο κατάστημα!
                        _productRepo.Update(product);
                    }
                }
            }

            orderReturn.Status = newStatus;
            orderReturn.UpdatedAt = DateTime.UtcNow;

            _returnRepo.Update(orderReturn);
            await _returnRepo.SaveChangesAsync();

            // ΑΥΤΟΜΑΤΗ ΕΙΔΟΠΟΙΗΣΗ: Ενημέρωση του Πελάτη βάσει του Workflow Status σου!
            string notificationTitle = "Ενημέρωση Επιστροφής";
            string notificationMessage = $"Η κατάσταση του αιτήματος επιστροφής σας άλλαξε σε: {newStatus}.";

            if (newStatus.Equals("Received", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Λάβαμε τα επιστρεφόμενα προϊόντα!";
                notificationMessage = $"Τα προϊόντα της επιστροφής #{orderReturn.Id} έφτασαν στις εγκαταστάσεις μας και περνάνε από ποιοτικό έλεγχο.";
            }
            else if (newStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Η επιστροφή εγκρίθηκε!";
                notificationMessage = $"Ευχαριστούμε! Η επιστροφή #{orderReturn.Id} εγκρίθηκε επιτυχώς.";
            }
            else if (newStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Η επιστροφή απορρίφθηκε";
                notificationMessage = $"Δυστυχώς, η επιστροφή #{orderReturn.Id} δεν έγινε δεκτή καθώς δεν πληρούσε τους όρους επιστροφών.";
            }
            else if (newStatus.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
            {
                notificationTitle = "Τα χρήματά σας επιστράφηκαν!";
                notificationMessage = $"Η διαδικασία ολοκληρώθηκε! Το ποσό των {orderReturn.RefundAmount}€ πιστώθηκε στον λογαριασμό σας.";
            }

            var statusNotification = new Notification
            {
                CustomerId = orderReturn.CustomerId,
                Title = notificationTitle,
                Message = notificationMessage,
                Type = "Info",
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepo.AddAsync(statusNotification);
            await _notificationRepo.SaveChangesAsync();

            return _mapper.Map<OrderReturnResponseDto>(orderReturn);
        }
    }
}