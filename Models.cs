using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TeamManagerBot.Models
{
    // Перечисления
    public enum UserRole
    {
        Member = 0,
        Admin = 1
    }

    public enum ProjectStatus
    {
        Pending = 0,    // 🟡 Предстоит
        InProgress = 1, // 🟠 В работе
        Completed = 2   // ✅ Готово
    }

    public enum TeamTaskStatus
    {
        Active = 0,
        Completed = 1,
        Archived = 2
    }

    public enum FinancialRecordType
    {
        Income = 0,     // Доход
        Expense = 1,    // Расход
        Deposit = 2,    // Депозит
        Commission = 3, // Комиссия
        Investment = 4  // Вклад
    }

    // НОВЫЙ: Статус средств
    public enum FundStatus
    {
        Working = 1,    // 💰 В обороте (рабочие)
        Reserved = 2,   // 🏦 Резерв (нерабочие)
        Blocked = 3,    // 🔒 Заблокировано
        InTransit = 4   // ⏳ В пути (для крипты/переводов)
    }

    public enum AdvertisementType
    {
        ContentPlan = 0,
        AdCampaign = 1
    }

    public enum AdvertisementStatus
    {
        Planned = 0,
        InProgress = 1,
        Completed = 2,
        Archived = 3
    }

    // Пользователь
    public class User
    {
        [Key]
        public long TelegramId { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? LastName { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Member;

        // НОВОЕ: связь с контактом
        public int? ContactId { get; set; }

        [ForeignKey("ContactId")]
        public virtual TeamContact? Contact { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActiveAt { get; set; }

        // Навигационные свойства
        public virtual ICollection<TeamTask> Tasks { get; set; } = new List<TeamTask>();
        public virtual ICollection<FinancialRecord> FinancialRecords { get; set; } = new List<FinancialRecord>();
        public virtual ICollection<Project> CreatedProjects { get; set; } = new List<Project>();
        public virtual ICollection<StatusUpdate> StatusUpdates { get; set; } = new List<StatusUpdate>();
    }
    // Настройки
    public class SecurityLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(50)]
        public string EventType { get; set; } = string.Empty; // Login, SettingsChange, ProjectCreate, etc.

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public long? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(100)]
        public string? UserAgent { get; set; }

        public bool IsSuspicious { get; set; }
    }

    public class UserSession
    {
        [Key]
        public int Id { get; set; }

        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastActivityAt { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(100)]
        public string? UserAgent { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // Проект
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;

        [MaxLength(500)]
        public string? Link { get; set; }

        [Required]
        public long CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // НОВОЕ: бюджет проекта
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }

        // НОВОЕ: валюта бюджета
        [MaxLength(3)]
        public string? BudgetCurrency { get; set; } = "USD";

        // Навигационные свойства
        public virtual ICollection<TeamTask> Tasks { get; set; } = new List<TeamTask>();
        public virtual ICollection<StatusUpdate> StatusUpdates { get; set; } = new List<StatusUpdate>();
        public virtual ICollection<ProjectDocumentation> Documentations { get; set; } = new List<ProjectDocumentation>();
        public virtual ICollection<FinancialRecord> FinancialRecords { get; set; } = new List<FinancialRecord>();
    }

    // Задача
    public class TeamTask
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public TeamTaskStatus Status { get; set; } = TeamTaskStatus.Active;

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [Required]
        public long AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        public virtual User AssignedTo { get; set; } = null!;

        public long? CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // НОВОЕ: стоимость задачи (если оплачивается отдельно)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cost { get; set; }

        // НОВОЕ: приоритет
        [MaxLength(20)]
        public string? Priority { get; set; } = "Medium"; // High, Medium, Low
    }

    // Финансовая запись - ОБНОВЛЕНА

    public class TeamInvestment
    {
        [Key]
        public int Id { get; set; }

        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal InitialAmount { get; set; } // Начальная сумма вклада

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentAmount { get; set; } // Текущая сумма (уменьшается при выводах)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastWithdrawalAt { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true; // Активен ли вклад

        // История выводов
        public virtual ICollection<InvestmentWithdrawal> Withdrawals { get; set; } = new List<InvestmentWithdrawal>();
    }

    public class InvestmentWithdrawal
    {
        [Key]
        public int Id { get; set; }

        public int InvestmentId { get; set; }

        [ForeignKey("InvestmentId")]
        public virtual TeamInvestment Investment { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime WithdrawnAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
    public class FinancialRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public FinancialRecordType Type { get; set; }

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        [MaxLength(100)]
        public string? Source { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public long? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public int? ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        // НОВЫЕ ПОЛЯ
        [Required]
        [MaxLength(20)]
        public string FundStatus { get; set; } = "Working";

        public int? ContactId { get; set; }

        [ForeignKey("ContactId")]
        public virtual TeamContact? Contact { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Commission { get; set; }

        [MaxLength(20)]
        public string? CommissionPaidBy { get; set; }

        [Column(TypeName = "jsonb")]
        public string? MetadataJson { get; set; }

        // НЕ СОХРАНЯЕМОЕ поле для удобной работы с метаданными
        [NotMapped]
        public Dictionary<string, string> Metadata
        {
            get => string.IsNullOrEmpty(MetadataJson)
                ? new Dictionary<string, string>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson) ?? new();
            set => MetadataJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        public bool IsConfirmed { get; set; } = true;

        [MaxLength(200)]
        public string? TransactionHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Контакт - ОБНОВЛЕН
    public class TeamContact
    {
        [Key]
        public int Id { get; set; }

        // Дата создания
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Основные данные
        [Required, MaxLength(100)]
        public string TelegramUsername { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? FullName { get; set; }

        [MaxLength(50)]
        public string? Nickname { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime? BirthDate { get; set; }

        // Данные карты
        [MaxLength(30)]
        public string? CardNumber { get; set; }

        [MaxLength(10)]
        public string? CVV { get; set; }

        [MaxLength(10)]
        public string? CardExpiry { get; set; }  // MM/YY

        [MaxLength(100)]
        public string? SecurityWord { get; set; }

        // Наши данные на контакте
        [MaxLength(20)]
        public string? OurPhoneNumber { get; set; }  // Наш номер на контакте

        [MaxLength(100)]
        public string? BankPassword { get; set; }  // Пароль от банка

        [MaxLength(10)]
        public string? PinCode { get; set; }  // Пин-код

        [MaxLength(100)]
        public string? OurEmail { get; set; }  // Наша почта на контакте

        // Паспортные данные
        [MaxLength(20)]
        public string? PassportSeries { get; set; }

        [MaxLength(20)]
        public string? PassportNumber { get; set; }

        public DateTime? PassportExpiry { get; set; }  // Срок действия паспорта

        [MaxLength(20)]
        public string? PassportDepartmentCode { get; set; }  // Код подразделения

        [MaxLength(200)]
        public string? PassportIssuedBy { get; set; }  // Кем выдан

        public DateTime? PassportIssueDate { get; set; }  // Дата выдачи

        [MaxLength(20)]
        public string? INN { get; set; }

        // Статус и теги
        [MaxLength(50)]
        public string? CardStatus { get; set; }  // лок, 161, 115, рабочая

        [MaxLength(100)]
        public string? Tags { get; set; }  // Теги для поиска

        [MaxLength(50)]
        public string? ContactType { get; set; } = "Доп";  // Тип контакта

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Связь с пользователем
        public long? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // JSON поля для карт и кошельков
        [Column(TypeName = "jsonb")]
        public string? BankCardsJson { get; set; }

        [NotMapped]
        public List<BankCard> BankCards
        {
            get => string.IsNullOrEmpty(BankCardsJson)
                ? new List<BankCard>()
                : JsonSerializer.Deserialize<List<BankCard>>(BankCardsJson) ?? new();
            set => BankCardsJson = JsonSerializer.Serialize(value);
        }

        [Column(TypeName = "jsonb")]
        public string? CryptoWalletsJson { get; set; }

        [NotMapped]
        public List<CryptoWallet> CryptoWallets
        {
            get => string.IsNullOrEmpty(CryptoWalletsJson)
                ? new List<CryptoWallet>()
                : JsonSerializer.Deserialize<List<CryptoWallet>>(CryptoWalletsJson) ?? new();
            set => CryptoWalletsJson = JsonSerializer.Serialize(value);
        }
    }

    public class BankCard
    {
        public string? CardNumber { get; set; }  // Последние 4 цифры или полный номер
        public string? BankName { get; set; }
        public string? CardType { get; set; } // debit, credit
        public string? CVV { get; set; }
        public string? CardExpiry { get; set; }
        public string? SecurityWord { get; set; }
        public string? CardStatus { get; set; } // рабочая, лок, 115, 161
        public bool IsPrimary { get; set; }
        public string? Notes { get; set; }
    }

    public class CryptoWallet
    {
        public string? Network { get; set; } // BTC, ETH, TRX, BSC, SOL
        public string? Address { get; set; }
        public string? Label { get; set; }
        public bool IsPrimary { get; set; }
    }

    // ===== ПОСТЫ =====
    public class DbPost
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Channel { get; set; } // Telegram канал

        public DateTime? PublishDate { get; set; }

        [MaxLength(500)]
        public string? Link { get; set; }

        [MaxLength(100)]
        public string? Status { get; set; } // Черновик, Опубликовано, Запланировано

        public DateTime? UpdatedAt { get; set; }
    }

    // ===== МАНУАЛЫ =====
    public class DbManual
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? BankName { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty; // Основной, Дополнительный, Тестовый, Обход теневого бана, Снятие 115/161, Разлок 115/161

        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FilePath { get; set; } // Путь к PDF

        [MaxLength(100)]
        public string? Author { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    // ===== ОТЧЁТЫ ИНВЕСТОРАМ =====
    public class DbReport
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? InvestorName { get; set; }

        public DateTime ReportDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalProfit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalDeposit { get; set; }

        public string? Summary { get; set; }

        [MaxLength(500)]
        public string? FilePath { get; set; } // Путь к PDF

        [MaxLength(100)]
        public string? Status { get; set; } // Черновик, Готов, Отправлен

        public DateTime? UpdatedAt { get; set; }
    }

    // ===== ДОКУМЕНТАЦИЯ =====
    public class DbDocument
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ProjectName { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FilePath { get; set; } // Путь к PDF

        [MaxLength(100)]
        public string? DocumentType { get; set; } // Инструкция, Правила, Сводка, API

        public DateTime? UpdatedAt { get; set; }
    }

    // ===== РЕКЛАМА =====
    public class DbAd
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(200)]
        public string CampaignName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ProjectName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Spent { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? Description { get; set; }

        public string? Results { get; set; }

        [MaxLength(500)]
        public string? PostLink { get; set; }

        [MaxLength(100)]
        public string? Status { get; set; } // Активна, Завершена, Планируется

        public DateTime? UpdatedAt { get; set; }
    }

    // ===== FUNPAY АККАУНТЫ =====
    public class DbFunPayAccount
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(100)]
        public string Nickname { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? GoldenKey { get; set; }

        [MaxLength(100)]
        public string? BotUsername { get; set; }

        [MaxLength(100)]
        public string? BotPassword { get; set; }

        [MaxLength(100)]
        public string? ApiKey { get; set; }

        public virtual ICollection<DbFunPayWarning> Warnings { get; set; } = new List<DbFunPayWarning>();

        public DateTime? UpdatedAt { get; set; }
    }

    public class DbFunPayWarning
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FunPayAccountId { get; set; }

        [ForeignKey("FunPayAccountId")]
        public virtual DbFunPayAccount Account { get; set; } = null!;

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Resolution { get; set; }

        [MaxLength(100)]
        public string? Status { get; set; } // Активно, Решено
    }

    // Для расшифрованных данных (если нужно)
    public class TeamContactWithDecryptedData
    {
        public TeamContact Contact { get; set; } = null!;

        // Основные данные
        public string? PhoneNumber { get; set; }
        public string? CardNumber { get; set; }
        public string? CVV { get; set; }
        public string? SecurityWord { get; set; }
        public string? OurPhoneNumber { get; set; }
        public string? BankPassword { get; set; }
        public string? PinCode { get; set; }
        public string? OurEmail { get; set; }
        public string? PassportSeries { get; set; }
        public string? PassportNumber { get; set; }
        public string? PassportDepartmentCode { get; set; }
        public string? PassportIssuedBy { get; set; }
        public string? INN { get; set; }

        // ДОБАВЛЯЕМ ЭТИ ПОЛЯ
        public List<BankCard> BankCards { get; set; } = new();
        public List<CryptoWallet> CryptoWallets { get; set; } = new();
    }

    // Статистика
    public class ContactStatistics
    {
        public int TotalContacts { get; set; }
        public Dictionary<string, int> ContactsByStatus { get; set; } = new();
        public int ContactsWithCards { get; set; }
        public int ContactsWithPassport { get; set; }
    }

    // НОВЫЙ: модель банковской карты
    

    // ========== CRYPTO BOT ==========
    public class CryptoCircle
    {
        [Key]
        public int Id { get; set; }
        public int CircleNumber { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal ExpectedEndAmount { get; set; }
        public decimal ExpectedProfit => ExpectedEndAmount - DepositAmount;
        public decimal? ActualEndAmount { get; set; }
        public decimal? ActualProfit => ActualEndAmount.HasValue ? ActualEndAmount - DepositAmount : null;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public CircleStatus Status { get; set; } = CircleStatus.Active;

        // ДОБАВИТЬ ЭТУ СТРОКУ:
        public virtual ICollection<CryptoDeal> Deals { get; set; } = new List<CryptoDeal>();
    }

    public class CryptoDeal
    {
        [Key]
        public int Id { get; set; }
        public int DealNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int? CircleId { get; set; }

        // ДОБАВИТЬ ЭТУ СТРОКУ:
        public virtual CryptoCircle? Circle { get; set; }
    }

    public enum CircleStatus
    {
        Active,
        Completed,
        Cancelled
    }

    // ========== FUNPAY ==========
    public class FunPaySale
    {
        [Key]
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;  // Номер заказа
        public decimal SaleAmount { get; set; }  // Цена продажи одной вещи
        public decimal PurchaseAmount { get; set; }  // Цена закупки (за единицу или пачку)
        public int Quantity { get; set; } = 1;  // Количество
        public decimal TotalSaleAmount => SaleAmount * Quantity;  // Итог продажи
        public decimal TotalPurchaseAmount => PurchaseAmount * (IsBulkPurchase ? 1 : Quantity);  // Итог закупки
        public decimal Profit => TotalSaleAmount - TotalPurchaseAmount;  // Маржа
        public string Category { get; set; } = string.Empty;  // Категория товара
        public bool IsBulkPurchase { get; set; }  // true - закупали пачкой, false - поштучно
        public DateTime SaleDate { get; set; }
    }

    public class FunPayWithdrawal
    {
        [Key]
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Destination { get; set; } = string.Empty;  // Куда вывели
        public DateTime WithdrawalDate { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // ========== FAST INVEST ==========
    public class FastInvestInvestment
    {
        [Key]
        public int Id { get; set; }

        public int ContactId { get; set; }

        [ForeignKey("ContactId")]
        public virtual TeamContact Investor { get; set; } = null!;

        public DateTime DepositDate { get; set; }  // Дата депа
        public DateTime PlannedWithdrawalDate { get; set; }  // Плановая дата вывода
        public DateTime? ActualWithdrawalDate { get; set; }  // Фактическая дата вывода

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }  // Сумма депа

        [Column(TypeName = "decimal(18,2)")]
        public decimal? WithdrawalAmount { get; set; }  // Сумма вывода

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExpectedProfitPercent { get; set; }  // Ожидаемый процент прибыли

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExpectedProfitAmount { get; set; }  // Ожидаемая сумма прибыли

        public decimal? Profit => WithdrawalAmount.HasValue ? WithdrawalAmount - DepositAmount : null;

        public InvestStatus Status { get; set; } = InvestStatus.Active;
        public string? Comments { get; set; }
    }

    public enum InvestStatus
    {
        Active,      // В работе
        Completed,   // Завершено успешно
        Withdrawn    // Выведен, больше не работает
    }

    // ========== КОМИССИИ ==========
    public class BankCommission
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string BankName { get; set; } = string.Empty;  // Название банка или криптосети

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;  // Перевод, снятие, P2P, SWIFT и т.д.

        [Required, MaxLength(20)]
        public string CommissionType { get; set; } = "percent";  // percent, fixed, combined

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PercentValue { get; set; }  // Процент комиссии (например, 1.5)

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FixedValue { get; set; }  // Фиксированная комиссия (например, 99)

        [MaxLength(10)]
        public string? FixedCurrency { get; set; }  // RUR, USD, USDT и т.д.

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinAmount { get; set; }  // Минимальная сумма для применения

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxAmount { get; set; }  // Максимальная сумма для применения

        [MaxLength(500)]
        public string? Description { get; set; }  // Описание/примечание

        [MaxLength(1000)]
        public string? Advice { get; set; }  // Совет по уменьшению комиссии

        public int Priority { get; set; } = 0;  // Приоритет отображения

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CommissionTip
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Title { get; set; } = string.Empty;  // Заголовок совета

        [Required, MaxLength(2000)]
        public string Content { get; set; } = string.Empty;  // Текст совета

        [MaxLength(100)]
        public string? Category { get; set; }  // bank, crypto, p2p, general

        [MaxLength(100)]
        public string? BankName { get; set; }  // Для какого банка (если конкретный)

        public int Priority { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    // ========== ДРОПЫ ==========
    public class DropAccount
    {
        [Key]
        public int Id { get; set; }

        // Данные дропа
        public string TelegramUsername { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;  // Зашифровать
        public DateTime? BirthDate { get; set; }

        // Данные карты
        public string CardNumber { get; set; } = string.Empty;  // Зашифровать
        public string CVV { get; set; } = string.Empty;  // Зашифровать
        public string CardExpiry { get; set; } = string.Empty;  // MM/YY
        public string SecurityWord { get; set; } = string.Empty;  // Зашифровать

        // Наши данные
        public string OurPhoneOnDrop { get; set; } = string.Empty;  // Зашифровать
        public string BankPassword { get; set; } = string.Empty;  // Зашифровать
        public string PinCode { get; set; } = string.Empty;  // Зашифровать
        public string OurEmail { get; set; } = string.Empty;  // Зашифровать

        // Паспортные данные
        public string PassportSeries { get; set; } = string.Empty;  // Зашифровать
        public string PassportNumber { get; set; } = string.Empty;  // Зашифровать
        public DateTime? PassportExpiry { get; set; }
        public string PassportDepartmentCode { get; set; } = string.Empty;  // Зашифровать
        public string PassportIssuedBy { get; set; } = string.Empty;  // Зашифровать
        public DateTime? PassportIssueDate { get; set; }
        public string INN { get; set; } = string.Empty;  // Зашифровать

        // Статус
        public CardStatus Status { get; set; } = CardStatus.Working;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public enum CardStatus
    {
        Working,
        Locked,
        Blocked161,
        Blocked115,
        Other
    }

    // ========== МАНУАЛЫ ==========
    public class Manual
    {
        [Key]
        public int Id { get; set; }
        public string BankName { get; set; } = string.Empty;  // Название банка или "General"
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;  // Текст мануала
        public ManualCategory Category { get; set; }
        public ManualType Type { get; set; } = ManualType.Main;
        public string? FilePath { get; set; }  // Путь к PDF если есть
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public enum ManualCategory
    {
        Main,           // Основные мануалы
        Additional,     // Дополнительные
        ShadowBan,      // Обход теневого бана
        Unblock115,     // Снятие при 115 ФЗ
        Unblock161,     // Снятие при 161 ФЗ
        Test            // Тестовые мануалы
    }

    public enum ManualType
    {
        Main,      // Основной (в работе)
        Additional,// Дополнительный (узкие случаи)
        Test       // Тестовый (не проверен)
    }

    // ========== РЕКЛАМА ==========
    public class AdCampaign
    {
        [Key]
        public int Id { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;  // Для какого проекта
        public decimal Budget { get; set; }
        public decimal? Spent { get; set; }
        public string? PostContent { get; set; }  // Содержание поста
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Results { get; set; }  // Результаты рекламы
        public List<string>? Screenshots { get; set; }  // Ссылки на скриншоты
    }

    // ========== FUNPAY АККАУНТЫ ==========
    public class FunPayAccount
    {
        [Key]
        public int Id { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string GoldenKey { get; set; } = string.Empty;  // Зашифровать
        public string BotUsername { get; set; } = string.Empty;
        public string BotPassword { get; set; } = string.Empty;  // Зашифровать
        public string ApiKey { get; set; } = string.Empty;  // Зашифровать
        public List<FunPayWarning> Warnings { get; set; } = new();  // Штрафы
    }

    public class FunPayWarning
    {
        [Key]
        public int Id { get; set; }
        public int FunPayAccountId { get; set; }
        public virtual FunPayAccount Account { get; set; } = null!;
        public DateTime Date { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Resolution { get; set; }
    }

    // ОТЧЕТЫ, УВЕДОМЛЕНИЯ
    public class ReportSchedule
    {
        [Key]
        public int Id { get; set; }

        public bool IsEnabled { get; set; } = true;

        [Required, MaxLength(20)]
        public string Frequency { get; set; } = "daily"; // daily, weekly, monthly

        public int? DayOfWeek { get; set; } // 1-7 (Monday-Sunday)

        public int? DayOfMonth { get; set; } // 1-31

        [Required]
        public TimeSpan Time { get; set; } // Время отправки

        public long? LastSentAt { get; set; } // Timestamp последней отправки

        public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public long? UpdatedAt { get; set; }
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        [MaxLength(20)]
        public string? Frequency { get; set; } // once, daily, weekly, monthly, yearly

        public DateTime? SpecificDate { get; set; } // Для одноразовых

        public int? DayOfWeek { get; set; } // 1-7

        public int? DayOfMonth { get; set; }

        public int? Month { get; set; } // Для ежегодных

        [Required]
        public TimeSpan Time { get; set; }

        public long? LastTriggeredAt { get; set; }

        public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public long? UpdatedAt { get; set; }
    }

    // РАЗНОЕ
    public class Plan
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public long CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;

        [MaxLength(500)]
        public string? FilePath { get; set; } // Путь к файлу, если нужно
    }

    public class StatusUpdate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [Required, MaxLength(2000)]
        public string Text { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Link { get; set; }

        public long CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProjectDocumentation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string DocumentType { get; set; } = "manual";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public long CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;
    }

    public class Advertisement
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public AdvertisementType Type { get; set; }

        [MaxLength(1000)]
        public string? Goal { get; set; }

        [MaxLength(2000)]
        public string? ContentStrategy { get; set; }

        [MaxLength(2000)]
        public string? PostTemplates { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string? Schedule { get; set; }

        [Required]
        public AdvertisementStatus Status { get; set; } = AdvertisementStatus.Planned;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }

        public long CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

   

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public PaginatedResult(List<T> items, int page, int pageSize, int totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        public PaginatedResult()
        {
        }
    }

    public class CategoryStat
    {
        public string Category { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int Count { get; set; }
    }

    public class FinanceStatistics
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal Balance { get; set; }

        public decimal WorkingCapital { get; set; }
        public decimal ReservedFunds { get; set; }

        public List<CategoryStat> IncomeByCategory { get; set; } = new();
        public List<CategoryStat> ExpensesByCategory { get; set; } = new();
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public Dictionary<string, decimal> MonthlyTrend { get; set; } = new();
        public Dictionary<string, decimal> BalanceByStatus { get; set; } = new();
    }

    public class KpiStatistics
    {
        public int ActiveTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalTasks { get; set; }
        public decimal TaskCompletionRate { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public decimal ProjectCompletionRate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public Dictionary<string, decimal> UserPerformance { get; set; } = new();
    }

    // Контекст БД - ОБНОВЛЕН
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TeamTask> Tasks { get; set; }
        public DbSet<FinancialRecord> FinancialRecords { get; set; }
        public DbSet<TeamInvestment> TeamInvestments { get; set; }
        public DbSet<InvestmentWithdrawal> InvestmentWithdrawals { get; set; }
        public DbSet<TeamContact> Contacts { get; set; }
        public DbSet<StatusUpdate> StatusUpdates { get; set; }
        public DbSet<ProjectDocumentation> ProjectDocumentations { get; set; }
        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<CryptoCircle> CryptoCircles { get; set; }
        public DbSet<CryptoDeal> CryptoDeals { get; set; }
        public DbSet<FunPaySale> FunPaySales { get; set; }
        public DbSet<FunPayWithdrawal> FunPayWithdrawals { get; set; }
        public DbSet<FunPayAccount> FunPayAccounts { get; set; }
        public DbSet<FunPayWarning> FunPayWarnings { get; set; }
        public DbSet<FastInvestInvestment> FastInvestInvestments { get; set; }
        public DbSet<DropAccount> DropAccounts { get; set; }
        public DbSet<Manual> Manuals { get; set; }
        public DbSet<AdCampaign> AdCampaigns { get; set; }
        public DbSet<BankCommission> BankCommissions { get; set; }
        public DbSet<CommissionTip> CommissionTips { get; set; }
        public DbSet<DbPost> DbPosts { get; set; }
        public DbSet<DbManual> DbManuals { get; set; }
        public DbSet<DbReport> DbReports { get; set; }
        public DbSet<DbDocument> DbDocuments { get; set; }
        public DbSet<DbAd> DbAds { get; set; }
        public DbSet<DbFunPayAccount> DbFunPayAccounts { get; set; }
        public DbSet<DbFunPayWarning> DbFunPayWarnings { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<ReportSchedule> ReportSchedules { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Индексы
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.ContactId)
                .IsUnique(false);

            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Status);

            modelBuilder.Entity<TeamTask>()
                .HasIndex(t => t.Status);

            modelBuilder.Entity<TeamTask>()
                .HasIndex(t => t.AssignedToUserId);

            modelBuilder.Entity<FinancialRecord>()
                .HasIndex(f => f.TransactionDate);

            modelBuilder.Entity<FinancialRecord>()
                .HasIndex(f => f.Type);

            modelBuilder.Entity<FinancialRecord>()
                .HasIndex(f => f.FundStatus);

            modelBuilder.Entity<FinancialRecord>()
                .HasIndex(f => f.ContactId);

            modelBuilder.Entity<TeamContact>()
                .HasIndex(c => c.TelegramUsername);

            modelBuilder.Entity<TeamContact>()
                .HasIndex(c => c.UserId)
                .IsUnique(false);

            modelBuilder.Entity<BankCommission>()
                .HasIndex(c => new { c.BankName, c.Category });

            modelBuilder.Entity<CommissionTip>()
                .HasIndex(t => t.Category);

            modelBuilder.Entity<CommissionTip>()
                .HasIndex(t => t.BankName);

            modelBuilder.Entity<DbPost>()
            .HasIndex(p => p.CreatedAt);

            modelBuilder.Entity<DbManual>()
                .HasIndex(m => m.Category);

            modelBuilder.Entity<DbManual>()
                .HasIndex(m => m.BankName);

            modelBuilder.Entity<DbReport>()
                .HasIndex(r => r.ReportDate);

            modelBuilder.Entity<DbDocument>()
                .HasIndex(d => d.ProjectName);

            modelBuilder.Entity<DbAd>()
                .HasIndex(a => a.ProjectName);

            modelBuilder.Entity<DbAd>()
                .HasIndex(a => a.Status);

            modelBuilder.Entity<DbFunPayAccount>()
                .HasIndex(a => a.Nickname)
                .IsUnique();

            // Crypto
            modelBuilder.Entity<CryptoCircle>()
                .HasIndex(c => c.CircleNumber)
                .IsUnique();

            modelBuilder.Entity<CryptoDeal>()
                .HasIndex(d => d.DealNumber)
                .IsUnique();

            // FunPay
            modelBuilder.Entity<FunPaySale>()
                .HasIndex(s => s.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<FunPayAccount>()
                .HasIndex(a => a.Nickname)
                .IsUnique();

            // FastInvest
            modelBuilder.Entity<FastInvestInvestment>()
                .HasIndex(i => i.Status);

            // Drops
            modelBuilder.Entity<DropAccount>()
                .HasIndex(d => d.TelegramUsername)
                .IsUnique();

            modelBuilder.Entity<DropAccount>()
                .HasIndex(d => d.Status);

            // Manuals
            modelBuilder.Entity<Manual>()
                .HasIndex(m => new { m.BankName, m.Category });

            modelBuilder.Entity<Manual>()
                .HasIndex(m => m.Type);

            // AdCampaigns
            modelBuilder.Entity<AdCampaign>()
                .HasIndex(c => c.ProjectName);

            modelBuilder.Entity<AdCampaign>()
                .HasIndex(c => c.StartDate);

            // Отношения
            modelBuilder.Entity<Project>()
                .HasOne(p => p.CreatedBy)
                .WithMany(u => u.CreatedProjects)
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeamTask>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeamTask>()
                .HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StatusUpdate>()
                .HasOne(su => su.CreatedBy)
                .WithMany(u => u.StatusUpdates)
                .HasForeignKey(su => su.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // НОВЫЕ отношения
            modelBuilder.Entity<User>()
                .HasOne(u => u.Contact)
                .WithOne(c => c.User)
                .HasForeignKey<User>(u => u.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FinancialRecord>()
                .HasOne(f => f.Contact)
                .WithMany()
                .HasForeignKey(f => f.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка JSON полей
            modelBuilder.Entity<FinancialRecord>()
                .Property(f => f.MetadataJson)
                .HasColumnType("jsonb");

            modelBuilder.Entity<TeamContact>()
                .Property(c => c.BankCardsJson)
                .HasColumnType("jsonb");

            modelBuilder.Entity<TeamContact>()
                .Property(c => c.CryptoWalletsJson)
                .HasColumnType("jsonb");
        }
    }
}