using OnlineQuiz.DTOs;
using static OnlineQuiz.DTOs.NotificationDtos;

namespace OnlineQuiz.IServices
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetUserNotificationsAsync(long userId, bool unreadOnly = false);
        Task<NotificationSummaryDto> GetNotificationSummaryAsync(long userId);
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto);
        Task<int> CreateBulkNotificationsAsync(BulkNotificationDto dto);
        Task<bool> MarkAsReadAsync(long notificationId, long userId);
        Task<int> MarkMultipleAsReadAsync(List<long> notificationIds, long userId);
        Task<bool> DeleteNotificationAsync(long notificationId, long userId);
        Task<int> DeleteAllReadAsync(long userId);

        // Helper methods for common notification scenarios
        Task NotifyEnrollmentAsync(long userId, long courseId, string courseName);
        Task NotifyQuizPublishedAsync(long courseId, string quizTitle);
        Task NotifyQuizGradedAsync(long userId, long quizId, string quizTitle, decimal score);
    }
}