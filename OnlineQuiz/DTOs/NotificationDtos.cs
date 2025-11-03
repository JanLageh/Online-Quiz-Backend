using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class NotificationDtos
    {
        // Notification Response DTO
        public class NotificationDto
        {
            public long NotificationId { get; set; }
            public long UserId { get; set; }
            public long? CourseId { get; set; }
            public string CourseName { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // Info, Warning, Success, Error
            public bool IsRead { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // Create Notification DTO
        public class CreateNotificationDto
        {
            [Required]
            public long UserId { get; set; }

            public long? CourseId { get; set; }

            [Required]
            [StringLength(120)]
            public string Title { get; set; } = string.Empty;

            [Required]
            public string Message { get; set; } = string.Empty;

            [Required]
            [StringLength(30)]
            public string Type { get; set; } = "Info"; // Info, Warning, Success, Error
        }

        // Bulk Notification DTO
        public class BulkNotificationDto
        {
            [Required]
            public List<long> UserIds { get; set; } = new();

            public long? CourseId { get; set; }

            [Required]
            [StringLength(120)]
            public string Title { get; set; } = string.Empty;

            [Required]
            public string Message { get; set; } = string.Empty;

            [Required]
            [StringLength(30)]
            public string Type { get; set; } = "Info";
        }

        // Mark as Read DTO
        public class MarkAsReadDto
        {
            [Required]
            public List<long> NotificationIds { get; set; } = new();
        }

        // Notification Summary DTO
        public class NotificationSummaryDto
        {
            public int TotalCount { get; set; }
            public int UnreadCount { get; set; }
            public List<NotificationDto> RecentNotifications { get; set; } = new();
        }
    }
}