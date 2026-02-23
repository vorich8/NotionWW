using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IUserService
    {
        // Получение пользователей
        Task<User?> GetUserByTelegramIdAsync(long telegramId);
        Task<List<User>> GetAllUsersAsync();

        // Создание и обновление
        Task<User?> GetOrCreateUserAsync(long telegramId, string username, string firstName, string? lastName);
        Task<bool> UpdateUserAsync(User user);
        Task UpdateUserLastActiveAsync(long telegramId);

        // Проверки
        Task<bool> IsAdminAsync(long telegramId);
        Task<bool> UserExistsAsync(long telegramId);

        // Статистика
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetActiveUsersCountAsync(int days = 7);
        Task<List<User>> GetAdminsAsync();

        // Логи, Сессии
        Task LogSecurityEventAsync(string eventType, string description, long? userId = null, string? ipAddress = null, string? userAgent = null);
        Task<List<SecurityLog>> GetRecentSecurityLogsAsync(int count = 50);
        Task<List<UserSession>> GetActiveSessionsAsync();
        Task UpdateUserSessionAsync(long userId, string? ipAddress = null, string? userAgent = null);
        Task EndUserSessionAsync(long userId);
    }
}