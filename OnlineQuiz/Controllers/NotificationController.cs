using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.IServices;
using static OnlineQuiz.DTOs.NotificationDtos;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // For testing - remove in production
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        private long GetCurrentUserIdOrDefault()
        {
            var idClaim = User.FindFirst("nameid")?.Value;
            if (long.TryParse(idClaim, out var userId))
                return userId;
            return 1; // Default for testing
        }

        /// Get all notifications for current user
        /// GET /api/notification
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
        {
            try
            {
                var userId = GetCurrentUserIdOrDefault();
                var notifications = await _service.GetUserNotificationsAsync(userId, unreadOnly);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Get notification summary (count and recent)
        /// GET /api/notification/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var userId = GetCurrentUserIdOrDefault();
                var summary = await _service.GetNotificationSummaryAsync(userId);
                return Ok(summary);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

            /// Create a notification (Admin/Teacher only)
            /// POST /api/notification
            [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                var notification = await _service.CreateNotificationAsync(dto);
                return Ok(notification);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Send bulk notifications (Admin/Teacher only)
        /// POST /api/notification/bulk
        [HttpPost("bulk")]
        public async Task<IActionResult> CreateBulkNotifications([FromBody] BulkNotificationDto dto)
        {
            try
            {
                var count = await _service.CreateBulkNotificationsAsync(dto);
                return Ok(new { message = $"Sent {count} notifications successfully", count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Mark notification as read
        /// PUT /api/notification/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            try
            {
                var userId = GetCurrentUserIdOrDefault();
                var success = await _service.MarkAsReadAsync(id, userId);

                if (!success)
                    return NotFound(new { message = "Notification not found" });

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Mark multiple notifications as read
        /// PUT /api/notification/read-multiple
        [HttpPut("read-multiple")]
        public async Task<IActionResult> MarkMultipleAsRead([FromBody] MarkAsReadDto dto)
        {
            try
            {
                var userId = GetCurrentUserIdOrDefault();
                var count = await _service.MarkMultipleAsReadAsync(dto.NotificationIds, userId);
                return Ok(new { message = $"Marked {count} notifications as read", count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Delete a notification
        /// DELETE /api/notification/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            try
            {
                var userId = GetCurrentUserIdOrDefault();
                var success = await _service.DeleteNotificationAsync(id, userId);

                if (!success)
                    return NotFound(new { message = "Notification not found" });

                return Ok(new { message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Delete all read notifications
        /// DELETE /api/notification/read
        [HttpDelete("read")]
        public async Task<IActionResult> DeleteAllRead()
        {
            try
            {
                var userId = GetCurrentUserIdOrDefault();
                var count = await _service.DeleteAllReadAsync(userId);
                return Ok(new { message = $"Deleted {count} read notifications", count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}