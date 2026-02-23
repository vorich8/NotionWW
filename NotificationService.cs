using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== Report Schedule =====

        public async Task<ReportSchedule> GetReportScheduleAsync()
        {
            var schedule = await _context.ReportSchedules.FirstOrDefaultAsync();
            if (schedule == null)
            {
                schedule = new ReportSchedule
                {
                    IsEnabled = false,
                    Frequency = "daily",
                    Time = new TimeSpan(9, 0, 0), // 09:00 UTC
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                _context.ReportSchedules.Add(schedule);
                await _context.SaveChangesAsync();
            }
            return schedule;
        }

        public async Task<bool> UpdateReportScheduleAsync(ReportSchedule schedule)
        {
            try
            {
                var existing = await _context.ReportSchedules.FirstOrDefaultAsync();
                if (existing == null)
                {
                    schedule.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    _context.ReportSchedules.Add(schedule);
                }
                else
                {
                    existing.IsEnabled = schedule.IsEnabled;
                    existing.Frequency = schedule.Frequency;
                    existing.DayOfWeek = schedule.DayOfWeek;
                    existing.DayOfMonth = schedule.DayOfMonth;
                    existing.Time = schedule.Time;
                    existing.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report schedule");
                return false;
            }
        }

        // ===== Notifications =====

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            notification.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<Notification?> GetNotificationAsync(int id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            return await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateNotificationAsync(Notification notification)
        {
            try
            {
                var existing = await _context.Notifications.FindAsync(notification.Id);
                if (existing == null) return false;

                existing.Title = notification.Title;
                existing.Message = notification.Message;
                existing.Frequency = notification.Frequency;
                existing.SpecificDate = notification.SpecificDate;
                existing.DayOfWeek = notification.DayOfWeek;
                existing.DayOfMonth = notification.DayOfMonth;
                existing.Month = notification.Month;
                existing.Time = notification.Time;
                existing.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification {Id}", notification.Id);
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null) return false;

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {Id}", id);
                return false;
            }
        }

        public async Task<bool> ToggleNotificationAsync(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null) return false;

                notification.IsEnabled = !notification.IsEnabled;
                notification.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling notification {Id}", id);
                return false;
            }
        }

        public async Task<List<Notification>> GetNotificationsToTriggerAsync()
        {
            var now = DateTime.UtcNow;
            var notifications = await _context.Notifications
                .Where(n => n.IsEnabled)
                .ToListAsync();

            return notifications.Where(n => ShouldTrigger(n, now)).ToList();
        }

        private bool ShouldTrigger(Notification n, DateTime now)
        {
            if (n.LastTriggeredAt.HasValue)
            {
                var lastTrigger = DateTimeOffset.FromUnixTimeSeconds(n.LastTriggeredAt.Value).UtcDateTime;
                if ((now - lastTrigger).TotalMinutes < 1) // Защита от повторных срабатываний
                    return false;
            }

            switch (n.Frequency)
            {
                case "once":
                    return n.SpecificDate.HasValue &&
                           now.Date == n.SpecificDate.Value.Date &&
                           now.TimeOfDay >= n.Time &&
                           now.TimeOfDay < n.Time.Add(TimeSpan.FromMinutes(1));

                case "daily":
                    return now.TimeOfDay >= n.Time && now.TimeOfDay < n.Time.Add(TimeSpan.FromMinutes(1));

                case "weekly":
                    return n.DayOfWeek.HasValue &&
                           (int)now.DayOfWeek == n.DayOfWeek.Value &&
                           now.TimeOfDay >= n.Time &&
                           now.TimeOfDay < n.Time.Add(TimeSpan.FromMinutes(1));

                case "monthly":
                    return n.DayOfMonth.HasValue &&
                           now.Day == n.DayOfMonth.Value &&
                           now.TimeOfDay >= n.Time &&
                           now.TimeOfDay < n.Time.Add(TimeSpan.FromMinutes(1));

                case "yearly":
                    return n.Month.HasValue && n.DayOfMonth.HasValue &&
                           now.Month == n.Month.Value &&
                           now.Day == n.DayOfMonth.Value &&
                           now.TimeOfDay >= n.Time &&
                           now.TimeOfDay < n.Time.Add(TimeSpan.FromMinutes(1));

                default:
                    return false;
            }
        }

        public async Task UpdateNotificationTriggerAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.LastTriggeredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await _context.SaveChangesAsync();
            }
        }
    }
}