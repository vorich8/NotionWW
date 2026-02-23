using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;
using TeamManagerBot.Services;
using Telegram.Bot;

namespace TeamManagerBot.BackgroundServices
{
    public class NotificationWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<NotificationWorker> _logger;

        public NotificationWorker(IServiceProvider services, ILogger<NotificationWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();
                        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
                        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
                        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                        // Проверяем отчеты
                        await CheckReportScheduleAsync(scope, notificationService, financeService, projectService, taskService, botClient, userService);

                        // Проверяем уведомления
                        await CheckNotificationsAsync(scope, notificationService, botClient, userService);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Проверка каждые 30 секунд
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in notification worker");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Notification Worker stopped");
        }

        private async Task CheckReportScheduleAsync(IServiceScope scope, INotificationService notificationService,
            IFinanceService financeService, IProjectService projectService, ITaskService taskService,
            ITelegramBotClient botClient, IUserService userService)
        {
            var schedule = await notificationService.GetReportScheduleAsync();
            if (!schedule.IsEnabled) return;

            var now = DateTime.UtcNow;
            var lastSent = schedule.LastSentAt.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(schedule.LastSentAt.Value).UtcDateTime
                : DateTime.MinValue;

            bool shouldSend = false;

            switch (schedule.Frequency)
            {
                case "daily":
                    shouldSend = now.Date > lastSent.Date && now.TimeOfDay >= schedule.Time;
                    break;
                case "weekly":
                    if (schedule.DayOfWeek.HasValue && now.DayOfWeek == (DayOfWeek)schedule.DayOfWeek.Value)
                        shouldSend = now.Date > lastSent.Date && now.TimeOfDay >= schedule.Time;
                    break;
                case "monthly":
                    if (schedule.DayOfMonth.HasValue && now.Day == schedule.DayOfMonth.Value)
                        shouldSend = now.Date > lastSent.Date && now.TimeOfDay >= schedule.Time;
                    break;
            }

            if (shouldSend)
            {
                await SendReportAsync(scope, financeService, projectService, taskService, botClient, userService);

                schedule.LastSentAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await notificationService.UpdateReportScheduleAsync(schedule);
            }
        }

        private async Task SendReportAsync(IServiceScope scope, IFinanceService financeService,
            IProjectService projectService, ITaskService taskService,
            ITelegramBotClient botClient, IUserService userService)
        {
            try
            {
                var users = await userService.GetAllUsersAsync();
                var adminUsers = users.Where(u => u.Role == UserRole.Admin).ToList();

                var projects = await projectService.GetAllProjectsAsync();
                var tasks = await taskService.GetAllTasksAsync();

                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var incomes = await financeService.GetTotalIncomeAsync(monthStart, DateTime.UtcNow);
                var expenses = await financeService.GetTotalExpensesAsync(monthStart, DateTime.UtcNow);

                var report = $"📊 ЕЖЕДНЕВНЫЙ ОТЧЁТ\n\n" +
                            $"📅 {DateTime.UtcNow.AddHours(3):dd.MM.yyyy HH:mm} MSK\n\n" +
                            $"📊 ПРОЕКТЫ:\n" +
                            $"• Всего: {projects.Count}\n" +
                            $"• В работе: {projects.Count(p => p.Status == ProjectStatus.InProgress)}\n" +
                            $"• Завершено: {projects.Count(p => p.Status == ProjectStatus.Completed)}\n\n" +
                            $"✅ ЗАДАЧИ:\n" +
                            $"• Всего: {tasks.Count}\n" +
                            $"• Активных: {tasks.Count(t => t.Status == TeamTaskStatus.Active)}\n" +
                            $"• Выполнено: {tasks.Count(t => t.Status == TeamTaskStatus.Completed)}\n\n" +
                            $"💰 ФИНАНСЫ ЗА МЕСЯЦ:\n" +
                            $"• Доходы: {incomes:N0} ₽\n" +
                            $"• Расходы: {expenses:N0} ₽\n" +
                            $"• Прибыль: {incomes - expenses:N0} ₽";

                foreach (var admin in adminUsers)
                {
                    try
                    {
                        await botClient.SendMessage(
                            chatId: admin.TelegramId,
                            text: report,
                            cancellationToken: CancellationToken.None
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending report to admin {AdminId}", admin.TelegramId);
                    }
                }

                _logger.LogInformation("Daily report sent to {Count} admins", adminUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating/sending report");
            }
        }

        private async Task CheckNotificationsAsync(IServiceScope scope, INotificationService notificationService,
            ITelegramBotClient botClient, IUserService userService)
        {
            var notifications = await notificationService.GetNotificationsToTriggerAsync();

            foreach (var notification in notifications)
            {
                try
                {
                    var users = await userService.GetAllUsersAsync();

                    foreach (var user in users)
                    {
                        try
                        {
                            await botClient.SendMessage(
                                chatId: user.TelegramId,
                                text: $"🔔 {notification.Title}\n\n{notification.Message}",
                                cancellationToken: CancellationToken.None
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending notification to user {UserId}", user.TelegramId);
                        }
                    }

                    await notificationService.UpdateNotificationTriggerAsync(notification.Id);
                    _logger.LogInformation("Notification {Id} triggered", notification.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification {Id}", notification.Id);
                }
            }
        }
    }
}