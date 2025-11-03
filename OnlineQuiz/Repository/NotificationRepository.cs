using Microsoft.EntityFrameworkCore;
using OnlineQuiz.Data;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using static OnlineQuiz.DTOs.NotificationDtos;

namespace OnlineQuiz.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly OnlineQuizDbContext _context;

        public NotificationRepository(OnlineQuizDbContext context)
        {
            _context = context;
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(long userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Include(n => n.Course)
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    CourseId = n.CourseId,
                    CourseName = n.Course != null ? n.Course.Name : "",
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return notifications;
        }

        public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(long userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            var unreadCount = notifications.Count(n => !n.IsRead);
            var recentNotifications = await GetUserNotificationsAsync(userId);

            return new NotificationSummaryDto
            {
                TotalCount = notifications.Count,
                UnreadCount = unreadCount,
                RecentNotifications = recentNotifications.Take(5).ToList()
            };
        }

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto)
        {
            var notification = new NotificationModel
            {
                UserId = dto.UserId,
                CourseId = dto.CourseId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Load course name for response
            var course = dto.CourseId.HasValue
                ? await _context.Courses.FindAsync(dto.CourseId.Value)
                : null;

            return new NotificationDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                CourseId = notification.CourseId,
                CourseName = course?.Name ?? "",
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

        public async Task<int> CreateBulkNotificationsAsync(BulkNotificationDto dto)
        {
            var notifications = dto.UserIds.Select(userId => new NotificationModel
            {
                UserId = userId,
                CourseId = dto.CourseId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return notifications.Count;
        }

        public async Task<bool> MarkAsReadAsync(long notificationId, long userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> MarkMultipleAsReadAsync(List<long> notificationIds, long userId)
        {
            var notifications = await _context.Notifications
                .Where(n => notificationIds.Contains(n.NotificationId) && n.UserId == userId)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        public async Task<bool> DeleteNotificationAsync(long notificationId, long userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> DeleteAllReadAsync(long userId)
        {
            var readNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead)
                .ToListAsync();

            _context.Notifications.RemoveRange(readNotifications);
            await _context.SaveChangesAsync();

            return readNotifications.Count;
        }
    }
}