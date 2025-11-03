using Microsoft.EntityFrameworkCore;
using OnlineQuiz.Data;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using static OnlineQuiz.DTOs.NotificationDtos;

namespace OnlineQuiz.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly OnlineQuizDbContext _context;

        public NotificationService(INotificationRepository repo, OnlineQuizDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public Task<List<NotificationDto>> GetUserNotificationsAsync(long userId, bool unreadOnly = false)
            => _repo.GetUserNotificationsAsync(userId, unreadOnly);

        public Task<NotificationSummaryDto> GetNotificationSummaryAsync(long userId)
            => _repo.GetNotificationSummaryAsync(userId);

        public Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto)
            => _repo.CreateNotificationAsync(dto);

        public Task<int> CreateBulkNotificationsAsync(BulkNotificationDto dto)
            => _repo.CreateBulkNotificationsAsync(dto);

        public Task<bool> MarkAsReadAsync(long notificationId, long userId)
            => _repo.MarkAsReadAsync(notificationId, userId);

        public Task<int> MarkMultipleAsReadAsync(List<long> notificationIds, long userId)
            => _repo.MarkMultipleAsReadAsync(notificationIds, userId);

        public Task<bool> DeleteNotificationAsync(long notificationId, long userId)
            => _repo.DeleteNotificationAsync(notificationId, userId);

        public Task<int> DeleteAllReadAsync(long userId)
            => _repo.DeleteAllReadAsync(userId);

        // Helper methods for common notification scenarios
        public async Task NotifyEnrollmentAsync(long userId, long courseId, string courseName)
        {
            var notification = new CreateNotificationDto
            {
                UserId = userId,
                CourseId = courseId,
                Title = "Enrolled in Course",
                Message = $"You have been enrolled in {courseName}",
                Type = "Success"
            };

            await _repo.CreateNotificationAsync(notification);
        }

        public async Task NotifyQuizPublishedAsync(long courseId, string quizTitle)
        {
            // Get all enrolled students in the course
            var enrolledStudentIds = await _context.Enrollments
                .Where(e => e.CourseId == courseId)
                .Select(e => e.UserId)
                .ToListAsync();

            if (enrolledStudentIds.Any())
            {
                var bulkNotification = new BulkNotificationDto
                {
                    UserIds = enrolledStudentIds,
                    CourseId = courseId,
                    Title = "New Quiz Published",
                    Message = $"A new quiz '{quizTitle}' has been published",
                    Type = "Info"
                };

                await _repo.CreateBulkNotificationsAsync(bulkNotification);
            }
        }

        public async Task NotifyQuizGradedAsync(long userId, long quizId, string quizTitle, decimal score)
        {
            var notification = new CreateNotificationDto
            {
                UserId = userId,
                Title = "Quiz Graded",
                Message = $"Your quiz '{quizTitle}' has been graded. Score: {score}%",
                Type = "Success"
            };

            await _repo.CreateNotificationAsync(notification);
        }
    }
}