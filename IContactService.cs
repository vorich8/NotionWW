using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IContactService
    {
        // Основные CRUD
        Task<TeamContact?> CreateContactAsync(TeamContact contact);
        Task<TeamContact?> GetContactAsync(int contactId);
        Task<List<TeamContact>> GetAllContactsAsync();
        Task<bool> UpdateContactAsync(TeamContact contact);
        Task<bool> DeleteContactAsync(int contactId);

        // Поиск и фильтрация
        Task<List<TeamContact>> SearchContactsAsync(string searchTerm);
        Task<List<TeamContact>> GetContactsByStatusAsync(string status);
        Task<List<TeamContact>> GetContactsByTypeAsync(string contactType);
        Task<List<TeamContact>> GetContactsWithCardsAsync();
        Task<List<TeamContact>> GetContactsWithCryptoAsync();

        // Статистика
        Task<ContactStatistics> GetContactStatisticsAsync();

        // Простое создание
        Task<TeamContact?> CreateSimpleContactAsync(string telegramUsername, string? fullName = null,
            string? contactType = null, string? tags = null);

        // Расшифровка данных (для отображения)
        Task<TeamContactWithDecryptedData?> GetContactWithDecryptedDataAsync(int contactId);

        // ===== БАНКОВСКИЕ КАРТЫ =====
        Task<bool> AddBankCardAsync(int contactId, BankCard card);
        Task<bool> UpdateBankCardAsync(int contactId, string cardNumber, BankCard updatedCard);
        Task<bool> RemoveBankCardAsync(int contactId, string cardNumber);
        Task<bool> SetPrimaryBankCardAsync(int contactId, string cardNumber);
        Task<List<BankCard>> GetBankCardsAsync(int contactId);
        Task<BankCard?> GetPrimaryBankCardAsync(int contactId);

        // ===== КРИПТО-КОШЕЛЬКИ =====
        Task<bool> AddCryptoWalletAsync(int contactId, CryptoWallet wallet);
        Task<bool> UpdateCryptoWalletAsync(int contactId, string address, CryptoWallet updatedWallet);
        Task<bool> RemoveCryptoWalletAsync(int contactId, string address);
        Task<bool> SetPrimaryCryptoWalletAsync(int contactId, string address);
        Task<List<CryptoWallet>> GetCryptoWalletsAsync(int contactId);
        Task<CryptoWallet?> GetPrimaryCryptoWalletAsync(int contactId);
    }
}