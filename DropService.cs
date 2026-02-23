using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class DropService : IDropService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DropService> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly IUserService _userService;

        public DropService(
            ApplicationDbContext context,
            ILogger<DropService> logger,
            IEncryptionService encryptionService,
            IUserService userService)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
            _userService = userService;
        }

        public async Task<DropAccount> CreateDropAccountAsync(DropAccount account)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Шифруем чувствительные данные
                account.PhoneNumber = _encryptionService.EncryptToBase64WithIV(account.PhoneNumber);
                account.CardNumber = _encryptionService.EncryptToBase64WithIV(account.CardNumber);
                account.CVV = _encryptionService.EncryptToBase64WithIV(account.CVV);
                account.SecurityWord = _encryptionService.EncryptToBase64WithIV(account.SecurityWord);
                account.OurPhoneOnDrop = _encryptionService.EncryptToBase64WithIV(account.OurPhoneOnDrop);
                account.BankPassword = _encryptionService.EncryptToBase64WithIV(account.BankPassword);
                account.PinCode = _encryptionService.EncryptToBase64WithIV(account.PinCode);
                account.OurEmail = _encryptionService.EncryptToBase64WithIV(account.OurEmail);
                account.PassportSeries = _encryptionService.EncryptToBase64WithIV(account.PassportSeries);
                account.PassportNumber = _encryptionService.EncryptToBase64WithIV(account.PassportNumber);
                account.PassportDepartmentCode = _encryptionService.EncryptToBase64WithIV(account.PassportDepartmentCode);
                account.PassportIssuedBy = _encryptionService.EncryptToBase64WithIV(account.PassportIssuedBy);
                account.INN = _encryptionService.EncryptToBase64WithIV(account.INN);

                account.CreatedAt = DateTime.UtcNow;

                _context.DropAccounts.Add(account);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created drop account for @{TelegramUsername}", account.TelegramUsername);
                return account;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating drop account for @{TelegramUsername}", account.TelegramUsername);
                throw;
            }
        }

        public async Task<DropAccount?> GetDropAccountAsync(int dropId)
        {
            return await _context.DropAccounts.FindAsync(dropId);
        }

        public async Task<DropAccount?> GetDropAccountByTelegramAsync(string telegramUsername)
        {
            return await _context.DropAccounts
                .FirstOrDefaultAsync(d => d.TelegramUsername == telegramUsername);
        }

        public async Task<List<DropAccount>> GetAllDropAccountsAsync()
        {
            return await _context.DropAccounts
                .OrderBy(d => d.TelegramUsername)
                .ToListAsync();
        }

        public async Task<List<DropAccount>> GetDropAccountsByStatusAsync(CardStatus status)
        {
            return await _context.DropAccounts
                .Where(d => d.Status == status)
                .OrderBy(d => d.TelegramUsername)
                .ToListAsync();
        }

        public async Task<List<DropAccount>> GetDropAccountsByBankAsync(string bankName)
        {
            // Примечание: банк можно извлечь из номера карты или добавить отдельное поле
            // Пока оставим заглушку
            return await _context.DropAccounts
                .Where(d => d.CardNumber.Contains(bankName)) // Временное решение
                .ToListAsync();
        }

        public async Task<bool> UpdateDropAccountAsync(DropAccount account)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existing = await _context.DropAccounts.FindAsync(account.Id);
                if (existing == null) return false;

                // Обновляем только незашифрованные поля
                existing.TelegramUsername = account.TelegramUsername;
                existing.FullName = account.FullName;
                existing.BirthDate = account.BirthDate;
                existing.CardExpiry = account.CardExpiry;
                existing.Status = account.Status;
                existing.Notes = account.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                // Обновляем зашифрованные поля только если они изменились
                if (!string.IsNullOrEmpty(account.PhoneNumber))
                    existing.PhoneNumber = _encryptionService.EncryptToBase64WithIV(account.PhoneNumber);

                if (!string.IsNullOrEmpty(account.CardNumber))
                    existing.CardNumber = _encryptionService.EncryptToBase64WithIV(account.CardNumber);

                if (!string.IsNullOrEmpty(account.CVV))
                    existing.CVV = _encryptionService.EncryptToBase64WithIV(account.CVV);

                if (!string.IsNullOrEmpty(account.SecurityWord))
                    existing.SecurityWord = _encryptionService.EncryptToBase64WithIV(account.SecurityWord);

                if (!string.IsNullOrEmpty(account.OurPhoneOnDrop))
                    existing.OurPhoneOnDrop = _encryptionService.EncryptToBase64WithIV(account.OurPhoneOnDrop);

                if (!string.IsNullOrEmpty(account.BankPassword))
                    existing.BankPassword = _encryptionService.EncryptToBase64WithIV(account.BankPassword);

                if (!string.IsNullOrEmpty(account.PinCode))
                    existing.PinCode = _encryptionService.EncryptToBase64WithIV(account.PinCode);

                if (!string.IsNullOrEmpty(account.OurEmail))
                    existing.OurEmail = _encryptionService.EncryptToBase64WithIV(account.OurEmail);

                if (!string.IsNullOrEmpty(account.PassportSeries))
                    existing.PassportSeries = _encryptionService.EncryptToBase64WithIV(account.PassportSeries);

                if (!string.IsNullOrEmpty(account.PassportNumber))
                    existing.PassportNumber = _encryptionService.EncryptToBase64WithIV(account.PassportNumber);

                if (account.PassportExpiry.HasValue)
                    existing.PassportExpiry = account.PassportExpiry;

                if (!string.IsNullOrEmpty(account.PassportDepartmentCode))
                    existing.PassportDepartmentCode = _encryptionService.EncryptToBase64WithIV(account.PassportDepartmentCode);

                if (!string.IsNullOrEmpty(account.PassportIssuedBy))
                    existing.PassportIssuedBy = _encryptionService.EncryptToBase64WithIV(account.PassportIssuedBy);

                if (account.PassportIssueDate.HasValue)
                    existing.PassportIssueDate = account.PassportIssueDate;

                if (!string.IsNullOrEmpty(account.INN))
                    existing.INN = _encryptionService.EncryptToBase64WithIV(account.INN);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated drop account {DropId}", account.Id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating drop account {DropId}", account.Id);
                return false;
            }
        }

        public async Task<bool> UpdateDropAccountStatusAsync(int dropId, CardStatus newStatus, string? notes = null)
        {
            try
            {
                var account = await _context.DropAccounts.FindAsync(dropId);
                if (account == null) return false;

                account.Status = newStatus;
                if (notes != null)
                    account.Notes = notes;

                account.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated drop account {DropId} status to {Status}", dropId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating drop account status {DropId}", dropId);
                return false;
            }
        }

        public async Task<bool> DeleteDropAccountAsync(int dropId)
        {
            try
            {
                var account = await _context.DropAccounts.FindAsync(dropId);
                if (account == null) return false;

                _context.DropAccounts.Remove(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted drop account {DropId}", dropId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting drop account {DropId}", dropId);
                return false;
            }
        }

        public async Task<DropAccountDecrypted?> GetDecryptedDropAccountAsync(int dropId, long userId)
        {
            // Проверяем, имеет ли пользователь право видеть расшифрованные данные
            var user = await _userService.GetUserByTelegramIdAsync(userId);
            if (user?.Role != UserRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to access decrypted drop data without admin rights", userId);
                return null;
            }

            var account = await _context.DropAccounts.FindAsync(dropId);
            if (account == null) return null;

            try
            {
                var result = new DropAccountDecrypted
                {
                    Account = account,
                    PhoneNumber = DecryptIfNotNull(account.PhoneNumber),
                    CardNumber = DecryptIfNotNull(account.CardNumber),
                    CVV = DecryptIfNotNull(account.CVV),
                    SecurityWord = DecryptIfNotNull(account.SecurityWord),
                    OurPhoneOnDrop = DecryptIfNotNull(account.OurPhoneOnDrop),
                    BankPassword = DecryptIfNotNull(account.BankPassword),
                    PinCode = DecryptIfNotNull(account.PinCode),
                    OurEmail = DecryptIfNotNull(account.OurEmail),
                    PassportSeries = DecryptIfNotNull(account.PassportSeries),
                    PassportNumber = DecryptIfNotNull(account.PassportNumber),
                    PassportDepartmentCode = DecryptIfNotNull(account.PassportDepartmentCode),
                    PassportIssuedBy = DecryptIfNotNull(account.PassportIssuedBy),
                    INN = DecryptIfNotNull(account.INN)
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting drop account {DropId}", dropId);
                return null;
            }
        }

        private string? DecryptIfNotNull(string? encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;
            return _encryptionService.DecryptFromBase64WithIV(encrypted);
        }

        public async Task<DropStatistics> GetDropStatisticsAsync()
        {
            var drops = await _context.DropAccounts.ToListAsync();

            // Группировка по статусам
            var stats = new DropStatistics
            {
                TotalDrops = drops.Count,
                WorkingDrops = drops.Count(d => d.Status == CardStatus.Working),
                LockedDrops = drops.Count(d => d.Status == CardStatus.Locked),
                BlockedDrops = drops.Count(d => d.Status == CardStatus.Blocked161 || d.Status == CardStatus.Blocked115),
                DropsByBank = new Dictionary<string, int>()
            };

            // Группировка по банкам (упрощенно - из расшифрованных данных, но тут шифр)
            // В реальности нужно добавить поле BankName в модель или определять по первым цифрам карты

            return stats;
        }
    }
}