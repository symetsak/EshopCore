using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Eshop.Application.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IMapper _mapper;

        // Inject το Repository και τον AutoMapper
        public NotificationService(INotificationRepository notificationRepo, IMapper mapper)
        {
            _notificationRepo = notificationRepo;
            _mapper = mapper;
        }

        // Λήψη όλων των ειδοποιήσεων
        public async Task<IEnumerable<NotificationResponseDto>> GetCustomerNotificationsAsync(int customerId)
        {
            var notifications = await _notificationRepo.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<NotificationResponseDto>>(notifications);
        }

        // Λήψη του unread count
        public async Task<UnreadNotificationCountDto> GetUnreadCountAsync(int customerId)
        {
            var count = await _notificationRepo.GetUnreadCountAsync(customerId);
            return new UnreadNotificationCountDto { UnreadCount = count };
        }

        // Σήμανση ως διαβασμένη με έλεγχο ασφαλείας!
        public async Task<bool> MarkAsReadAsync(int notificationId, int customerId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);

            // Ασφάλεια: Ελέγχουμε αν η ειδοποίηση υπάρχει ΚΑΙ αν ανήκει όντως στον χρήστη που κάνει το αίτημα
            if (notification == null || notification.CustomerId != customerId)
            {
                throw new InvalidOperationException("Η ειδοποίηση δεν βρέθηκε ή δεν έχετε δικαίωμα πρόσβασης.");
            }

            // Αν είναι ήδη διαβασμένη, δεν κάνουμε τίποτα, απλά επιστρέφουμε true
            if (notification.IsRead) return true;

            notification.IsRead = true;
            return await _notificationRepo.SaveChangesAsync();
        }
    }
}