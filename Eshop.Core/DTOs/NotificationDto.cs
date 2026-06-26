using System;

namespace Eshop.Application.DTOs
{
    // Το DTO για τη λίστα των ειδοποιήσεων
    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Το DTO για το unread count (το κόκκινο κυκλάκι στο header)
    public class UnreadNotificationCountDto
    {
        public int UnreadCount { get; set; }
    }
}