using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;
using TelegramUser = Telegram.Bot.Types.User;
using DbUser = TeamManagerBot.Models.User;

namespace TeamManagerBot.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetUserByTelegramIdAsync(long telegramId)
        {
            return await _context.Users
                .Include(u => u.Contact)
                .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Contact)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<User?> GetOrCreateUserAsync(long telegramId, string username, string firstName, string? lastName)
        {
            try
            {
                var user = await GetUserByTelegramIdAsync(telegramId);

                if (user != null)
                {
                    user.Username = username;
                    user.FirstName = firstName;
                    user.LastName = lastName;
                    user.LastActiveAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated user {TelegramId} (@{Username})", telegramId, username);

                    return user;
                }

                user = new User
                {
                    TelegramId = telegramId,
                    Username = username,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = UserRole.Member,
                    CreatedAt = DateTime.UtcNow,
                    LastActiveAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new user {TelegramId} (@{Username})", telegramId, username);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating user {TelegramId}", telegramId);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(user.TelegramId);
                if (existingUser == null)
                    return false;

                existingUser.Username = user.Username;
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Role = user.Role;
                existingUser.ContactId = user.ContactId;
                existingUser.LastActiveAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated user {TelegramId}", user.TelegramId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {TelegramId}", user.TelegramId);
                return false;
            }
        }

        public async Task UpdateUserLastActiveAsync(long telegramId)
        {
            try
            {
                var user = await _context.Users.FindAsync(telegramId);
                if (user != null)
                {
                    user.LastActiveAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last active for user {TelegramId}", telegramId);
            }
        }

        public async Task<bool> IsAdminAsync(long telegramId)
        {
            var user = await _context.Users.FindAsync(telegramId);
            return user?.Role == UserRole.Admin;
        }

        public async Task<bool> UserExistsAsync(long telegramId)
        {
            return await _context.Users.AnyAsync(u => u.TelegramId == telegramId);
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetActiveUsersCountAsync(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Users
                .Where(u => u.LastActiveAt >= cutoffDate)
                .CountAsync();
        }

        public async Task<List<User>> GetAdminsAsync()
        {
            return await _context.Users
                .Where(u => u.Role == UserRole.Admin)
                .ToListAsync();
        }

        public async Task LogSecurityEventAsync(string eventType, string description, long? userId = null, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var log = new SecurityLog
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = eventType,
                    Description = description,
                    UserId = userId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsSuspicious = false // Здесь можно добавить логику определения подозрительных событий
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event");
            }
        }

        public async Task<List<SecurityLog>> GetRecentSecurityLogsAsync(int count = 50)
        {
            return await _context.SecurityLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync()
        {
            // Считаем активными сессии, где была активность за последние 30 минут
            var activeThreshold = DateTime.UtcNow.AddMinutes(-30);

            return await _context.UserSessions
                .Include(s => s.User)
                .Where(s => s.IsActive && s.LastActivityAt >= activeThreshold)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }

        public async Task UpdateUserSessionAsync(long userId, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var now = DateTime.UtcNow;
                var activeThreshold = now.AddMinutes(-30);

                // Ищем существующую активную сессию
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId &&
                                              s.IsActive &&
                                              s.LastActivityAt >= activeThreshold);

                if (session == null)
                {
                    // Создаём новую сессию
                    session = new UserSession
                    {
                        UserId = userId,
                        StartedAt = now,
                        LastActivityAt = now,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        IsActive = true
                    };
                    _context.UserSessions.Add(session);
                }
                else
                {
                    // Обновляем существующую
                    session.LastActivityAt = now;
                    if (ipAddress != null) session.IpAddress = ipAddress;
                    if (userAgent != null) session.UserAgent = userAgent;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user session");
            }
        }

        public async Task EndUserSessionAsync(long userId)
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsActive = false;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending user session");
            }
        }
    }
}