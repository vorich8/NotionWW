using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IDropService
    {
        Task<DropAccount> CreateDropAccountAsync(DropAccount account);
        Task<DropAccount?> GetDropAccountAsync(int dropId);
        Task<DropAccount?> GetDropAccountByTelegramAsync(string telegramUsername);
        Task<List<DropAccount>> GetAllDropAccountsAsync();
        Task<List<DropAccount>> GetDropAccountsByStatusAsync(CardStatus status);
        Task<List<DropAccount>> GetDropAccountsByBankAsync(string bankName);
        Task<bool> UpdateDropAccountAsync(DropAccount account);
        Task<bool> UpdateDropAccountStatusAsync(int dropId, CardStatus newStatus, string? notes = null);
        Task<bool> DeleteDropAccountAsync(int dropId);

        // Расшифровка данных (с проверкой прав)
        Task<DropAccountDecrypted?> GetDecryptedDropAccountAsync(int dropId, long userId);

        // Статистика
        Task<DropStatistics> GetDropStatisticsAsync();
    }

    public class DropAccountDecrypted
    {
        public DropAccount Account { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? CardNumber { get; set; }
        public string? CVV { get; set; }
        public string? SecurityWord { get; set; }
        public string? OurPhoneOnDrop { get; set; }
        public string? BankPassword { get; set; }
        public string? PinCode { get; set; }
        public string? OurEmail { get; set; }
        public string? PassportSeries { get; set; }
        public string? PassportNumber { get; set; }
        public string? PassportDepartmentCode { get; set; }
        public string? PassportIssuedBy { get; set; }
        public string? INN { get; set; }
    }

    public class DropStatistics
    {
        public int TotalDrops { get; set; }
        public int WorkingDrops { get; set; }
        public int LockedDrops { get; set; }
        public int BlockedDrops { get; set; }
        public Dictionary<string, int> DropsByBank { get; set; } = new();
    }
}