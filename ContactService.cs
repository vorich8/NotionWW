using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactService> _logger;
        private readonly IUserService _userService;

        public ContactService(
            ApplicationDbContext context,
            ILogger<ContactService> logger,
            IUserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        // ===== ОСНОВНЫЕ CRUD =====

        public async Task<TeamContact?> CreateContactAsync(TeamContact contact)
        {
            try
            {
                contact.CreatedAt = DateTime.UtcNow;
                contact.BankCardsJson = JsonSerializer.Serialize(new List<BankCard>());
                contact.CryptoWalletsJson = JsonSerializer.Serialize(new List<CryptoWallet>());

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created contact @{TelegramUsername}", contact.TelegramUsername);
                return contact;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return null;
            }
        }

        public async Task<TeamContact?> CreateSimpleContactAsync(string telegramUsername, string? fullName = null,
            string? contactType = null, string? tags = null)
        {
            try
            {
                var username = telegramUsername.StartsWith('@') ? telegramUsername.Substring(1) : telegramUsername;

                var existing = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.TelegramUsername.ToLower() == username.ToLower());

                if (existing != null)
                    return existing;

                var contact = new TeamContact
                {
                    TelegramUsername = username,
                    FullName = fullName,
                    ContactType = contactType ?? "Доп",
                    Tags = tags,
                    CreatedAt = DateTime.UtcNow,
                    BankCardsJson = JsonSerializer.Serialize(new List<BankCard>()),
                    CryptoWalletsJson = JsonSerializer.Serialize(new List<CryptoWallet>())
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                return contact;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating simple contact");
                return null;
            }
        }

        public async Task<TeamContact?> GetContactAsync(int contactId)
        {
            return await _context.Contacts.FindAsync(contactId);
        }

        public async Task<List<TeamContact>> GetAllContactsAsync()
        {
            return await _context.Contacts
                .OrderBy(c => c.TelegramUsername)
                .ToListAsync();
        }

        public async Task<bool> UpdateContactAsync(TeamContact contact)
        {
            try
            {
                var existing = await _context.Contacts.FindAsync(contact.Id);
                if (existing == null) return false;

                // Обновляем все поля кроме JSON (они обновляются отдельными методами)
                existing.TelegramUsername = contact.TelegramUsername;
                existing.FullName = contact.FullName;
                existing.Nickname = contact.Nickname;
                existing.PhoneNumber = contact.PhoneNumber;
                existing.BirthDate = contact.BirthDate;
                existing.CardNumber = contact.CardNumber;
                existing.CVV = contact.CVV;
                existing.CardExpiry = contact.CardExpiry;
                existing.SecurityWord = contact.SecurityWord;
                existing.OurPhoneNumber = contact.OurPhoneNumber;
                existing.BankPassword = contact.BankPassword;
                existing.PinCode = contact.PinCode;
                existing.OurEmail = contact.OurEmail;
                existing.PassportSeries = contact.PassportSeries;
                existing.PassportNumber = contact.PassportNumber;
                existing.PassportExpiry = contact.PassportExpiry;
                existing.PassportDepartmentCode = contact.PassportDepartmentCode;
                existing.PassportIssuedBy = contact.PassportIssuedBy;
                existing.PassportIssueDate = contact.PassportIssueDate;
                existing.INN = contact.INN;
                existing.CardStatus = contact.CardStatus;
                existing.Tags = contact.Tags;
                existing.ContactType = contact.ContactType;
                existing.Notes = contact.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact {ContactId}", contact.Id);
                return false;
            }
        }

        public async Task<bool> DeleteContactAsync(int contactId)
        {
            Console.WriteLine($"\n📦📦📦 ContactService.DeleteContactAsync НАЧАЛО для ID: {contactId} 📦📦📦");

            try
            {
                Console.WriteLine($"1. Поиск контакта в БД...");
                var contact = await _context.Contacts.FindAsync(contactId);

                if (contact == null)
                {
                    Console.WriteLine($"❌ Контакт {contactId} не найден в БД");
                    return false;
                }

                Console.WriteLine($"2. Найден контакт: {contact.TelegramUsername} (FullName: {contact.FullName})");
                Console.WriteLine($"3. Удаляем контакт из контекста...");

                _context.Contacts.Remove(contact);

                Console.WriteLine($"4. Сохраняем изменения в БД...");
                var result = await _context.SaveChangesAsync();

                Console.WriteLine($"5. SaveChangesAsync результат: {result} записей изменено");

                if (result > 0)
                {
                    Console.WriteLine($"6. ✅ Контакт успешно удален из БД");
                }
                else
                {
                    Console.WriteLine($"6. ⚠️ Контакт не был удален (result = 0)");
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌❌❌ ОШИБКА в ContactService.DeleteContactAsync: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error deleting contact {ContactId}", contactId);
                return false;
            }
            finally
            {
                Console.WriteLine($"📦📦📦 ContactService.DeleteContactAsync КОНЕЦ для ID: {contactId} 📦📦📦\n");
            }
        }

        // ===== ПОИСК И ФИЛЬТРАЦИЯ =====

        public async Task<List<TeamContact>> SearchContactsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<TeamContact>();

            searchTerm = searchTerm.ToLower();

            return await _context.Contacts
                .Where(c => c.TelegramUsername.ToLower().Contains(searchTerm) ||
                            (c.FullName != null && c.FullName.ToLower().Contains(searchTerm)) ||
                            (c.PhoneNumber != null && c.PhoneNumber.Contains(searchTerm)) ||
                            (c.CardNumber != null && c.CardNumber.Contains(searchTerm)) ||
                            (c.Tags != null && c.Tags.ToLower().Contains(searchTerm)))
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<TeamContact>> GetContactsByStatusAsync(string status)
        {
            return await _context.Contacts
                .Where(c => c.CardStatus == status)
                .ToListAsync();
        }

        public async Task<List<TeamContact>> GetContactsByTypeAsync(string contactType)
        {
            return await _context.Contacts
                .Where(c => c.ContactType == contactType)
                .ToListAsync();
        }

        public async Task<List<TeamContact>> GetContactsWithCardsAsync()
        {
            return await _context.Contacts
                .Where(c => !string.IsNullOrEmpty(c.BankCardsJson) && c.BankCardsJson != "[]")
                .ToListAsync();
        }

        public async Task<List<TeamContact>> GetContactsWithCryptoAsync()
        {
            return await _context.Contacts
                .Where(c => !string.IsNullOrEmpty(c.CryptoWalletsJson) && c.CryptoWalletsJson != "[]")
                .ToListAsync();
        }

        // ===== СТАТИСТИКА =====

        public async Task<ContactStatistics> GetContactStatisticsAsync()
        {
            var contacts = await _context.Contacts.ToListAsync();

            return new ContactStatistics
            {
                TotalContacts = contacts.Count,
                ContactsByStatus = contacts
                    .Where(c => !string.IsNullOrEmpty(c.CardStatus))
                    .GroupBy(c => c.CardStatus!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ContactsWithCards = contacts.Count(c => !string.IsNullOrEmpty(c.BankCardsJson) && c.BankCardsJson != "[]"),
                ContactsWithPassport = contacts.Count(c => !string.IsNullOrEmpty(c.PassportNumber))
            };
        }

        // ===== РАСШИФРОВКА =====

        public async Task<TeamContactWithDecryptedData?> GetContactWithDecryptedDataAsync(int contactId)
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null) return null;

            return new TeamContactWithDecryptedData
            {
                Contact = contact,
                PhoneNumber = contact.PhoneNumber,
                CardNumber = contact.CardNumber,
                CVV = contact.CVV,
                SecurityWord = contact.SecurityWord,
                OurPhoneNumber = contact.OurPhoneNumber,
                BankPassword = contact.BankPassword,
                PinCode = contact.PinCode,
                OurEmail = contact.OurEmail,
                PassportSeries = contact.PassportSeries,
                PassportNumber = contact.PassportNumber,
                PassportDepartmentCode = contact.PassportDepartmentCode,
                PassportIssuedBy = contact.PassportIssuedBy,
                INN = contact.INN,
                // ДОБАВЛЯЕМ ЭТИ СТРОКИ
                BankCards = contact.BankCards,
                CryptoWallets = contact.CryptoWallets
            };
        }

        // ===== БАНКОВСКИЕ КАРТЫ =====

        public async Task<bool> AddBankCardAsync(int contactId, BankCard card)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null) return false;

                var cards = contact.BankCards;

                // Если это первая карта или помечена как основная
                if (cards.Count == 0 || card.IsPrimary)
                {
                    // Сбрасываем IsPrimary у всех карт
                    foreach (var c in cards)
                        c.IsPrimary = false;
                }

                cards.Add(card);
                contact.BankCardsJson = JsonSerializer.Serialize(cards);
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Added bank card to contact {ContactId}", contactId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bank card to contact {ContactId}", contactId);
                return false;
            }
        }

        public async Task<bool> UpdateBankCardAsync(int contactId, string cardNumber, BankCard updatedCard)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null) return false;

                var cards = contact.BankCards;
                var index = cards.FindIndex(c => c.CardNumber == cardNumber);

                if (index == -1) return false;

                // Сохраняем флаг IsPrimary из обновленной карты
                if (updatedCard.IsPrimary)
                {
                    foreach (var c in cards)
                        c.IsPrimary = false;
                }

                cards[index] = updatedCard;
                contact.BankCardsJson = JsonSerializer.Serialize(cards);
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated bank card for contact {ContactId}", contactId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank card for contact {ContactId}", contactId);
                return false;
            }
        }

        public async Task<bool> RemoveBankCardAsync(int contactId, string cardNumber)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null) return false;

                var cards = contact.BankCards;
                var cardToRemove = cards.FirstOrDefault(c => c.CardNumber == cardNumber);

                if (cardToRemove != null)
                {
                    cards.Remove(cardToRemove);
                    contact.BankCardsJson = JsonSerializer.Serialize(cards);
                    contact.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Removed bank card from contact {ContactId}", contactId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing bank card from contact {ContactId}", contactId);
                return false;
            }
        }

        public async Task<bool> SetPrimaryBankCardAsync(int contactId, string cardNumber)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null) return false;

                var cards = contact.BankCards;
                var cardToSet = cards.FirstOrDefault(c => c.CardNumber == cardNumber);

                if (cardToSet == null) return false;

                // Сбрасываем IsPrimary у всех карт
                foreach (var card in cards)
                    card.IsPrimary = false;

                // Устанавливаем выбранную карту как основную
                cardToSet.IsPrimary = true;

                contact.BankCardsJson = JsonSerializer.Serialize(cards);
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Set primary bank card for contact {ContactId}", contactId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary bank card for contact {ContactId}", contactId);
                return false;
            }
        }

        public async Task<List<BankCard>> GetBankCardsAsync(int contactId)
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null) return new List<BankCard>();

            return contact.BankCards;
        }

        public async Task<BankCard?> GetPrimaryBankCardAsync(int contactId)
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null) return null;

            return contact.BankCards.FirstOrDefault(c => c.IsPrimary);
        }

        // ===== КРИПТО-КОШЕЛЬКИ =====

        public async Task<bool> AddCryptoWalletAsync(int contactId, CryptoWallet wallet)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null) return false;

                var wallets = contact.CryptoWallets;

                // Если это первый кошелек или помечен как основной
                if (wallets.Count == 0 || wallet.IsPrimary)
                {
                    foreach (var w in wallets)
                        w.IsPrimary = false;
                }

                wallets.Add(wallet);
                contact.CryptoWalletsJson = JsonSerializer.Serialize(wallets);
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Added crypto wallet to contact {ContactId}", contactId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding crypto wallet to contact {ContactId}", contactId);
                return false;
            }
        }

        public async Task<bool> UpdateCryptoWalletAsync(int contactId, string address, CryptoWallet updatedWallet)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null) return false;

                var wallets = contact.CryptoWallets;
                var index = wallets.FindIndex(w => w.Address == address);

                if (index == -1) return false;

                if (updatedWallet.IsPrimary)
                {
                    foreach (var w in wallets)
                        w.IsPrimary = false;
                }

                wallets[index] = updatedWallet;
                contact.CryptoWalletsJson = JsonSerializer.Serialize(wallets);
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated crypto wallet for contact {ContactId}", contactId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating crypto wallet for contact {ContactId}", contactId);
                return false;
            }
        }

        public async Task<bool> RemoveCryptoWalletAsync(int contactId, string address)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null) return false;

                var wallets = contact.CryptoWallets;
                var walletToRemove = wallets.FirstOrDefault(w => w.Address == address);

                if (walletToRemove != null)
                {
                    wallets.Remove(walletToRemove);
                    contact.CryptoWalletsJson = JsonSerializer.Serialize(wallets);
                    contact.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Removed crypto wallet from contact {ContactId}", contactId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing crypto wallet from contact {ContactId}", contactId);
                return false;
            }
        }

        public async Task<bool> SetPrimaryCryptoWalletAsync(int contactId, string address)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null) return false;

                var wallets = contact.CryptoWallets;
                var walletToSet = wallets.FirstOrDefault(w => w.Address == address);

                if (walletToSet == null) return false;

                foreach (var wallet in wallets)
                    wallet.IsPrimary = false;

                walletToSet.IsPrimary = true;

                contact.CryptoWalletsJson = JsonSerializer.Serialize(wallets);
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Set primary crypto wallet for contact {ContactId}", contactId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary crypto wallet for contact {ContactId}", contactId);
                return false;
            }
        }

        public async Task<List<CryptoWallet>> GetCryptoWalletsAsync(int contactId)
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null) return new List<CryptoWallet>();

            return contact.CryptoWallets;
        }

        public async Task<CryptoWallet?> GetPrimaryCryptoWalletAsync(int contactId)
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null) return null;

            return contact.CryptoWallets.FirstOrDefault(w => w.IsPrimary);
        }
    }
}