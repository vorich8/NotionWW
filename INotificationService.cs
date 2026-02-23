using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface INotificationService
    {
        // Report Schedule
        Task<ReportSchedule> GetReportScheduleAsync();
        Task<bool> UpdateReportScheduleAsync(ReportSchedule schedule);

        // Notifications
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task<Notification?> GetNotificationAsync(int id);
        Task<List<Notification>> GetAllNotificationsAsync();
        Task<bool> UpdateNotificationAsync(Notification notification);
        Task<bool> DeleteNotificationAsync(int id);
        Task<bool> ToggleNotificationAsync(int id);

        // Trigger checks
        Task<List<Notification>> GetNotificationsToTriggerAsync();
        Task UpdateNotificationTriggerAsync(int id);
    }
}