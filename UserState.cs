using System.Text.Json.Serialization;

namespace TeamManagerBot.Handlers
{
    public class UserState
    {
        public string CurrentAction { get; set; } = string.Empty;
        public int Step { get; set; } = 1;

        // ID связанных сущностей
        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }
        public int? ContactId { get; set; }
        public int? FinanceRecordId { get; set; }

        // НОВОЕ: ID для банковских карт и крипто-кошельков
        public int? BankCardId { get; set; }
        public int? CryptoWalletId { get; set; }

        // НОВОЕ: Для фильтрации и поиска
        public string? SearchQuery { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Данные состояния (уже есть)
        public Dictionary<string, object?> Data { get; set; } = new();

        // НОВОЕ: Временные данные для многошаговых форм
        [JsonIgnore]
        public Dictionary<string, object?> TempData { get; set; } = new();

        // НОВОЕ: Время создания состояния (для автоматической очистки)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // НОВОЕ: Последнее обновление
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        // НОВОЕ: Флаг для ожидания подтверждения
        public bool AwaitingConfirmation { get; set; }

        // НОВОЕ: Сообщение для подтверждения
        public string? ConfirmationMessage { get; set; }

        // НОВОЕ: Callback для подтверждения
        public string? ConfirmationCallback { get; set; }

        // НОВОЕ: Метод для обновления времени
        public void Touch()
        {
            LastUpdatedAt = DateTime.UtcNow;
        }

        // НОВОЕ: Проверка на истечение срока (по умолчанию 30 минут)
        public bool IsExpired(int timeoutMinutes = 30)
        {
            return (DateTime.UtcNow - LastUpdatedAt).TotalMinutes > timeoutMinutes;
        }

        // НОВОЕ: Очистка временных данных
        public void ClearTempData()
        {
            TempData.Clear();
        }

        // НОВОЕ: Получить значение из Data с приведением типа
        public T? GetData<T>(string key)
        {
            if (Data.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        // НОВОЕ: Установить значение в Data
        public void SetData(string key, object? value)
        {
            Data[key] = value;
            Touch();
        }



        // НОВОЕ: Получить значение из TempData с приведением типа
        public T? GetTempData<T>(string key)
        {
            if (TempData.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        // НОВОЕ: Установить значение в TempData
        public void SetTempData(string key, object? value)
        {
            TempData[key] = value;
            Touch();
        }

        // НОВОЕ: Сброс состояния
        public void Reset()
        {
            CurrentAction = string.Empty;
            Step = 1;
            ProjectId = null;
            TaskId = null;
            ContactId = null;
            FinanceRecordId = null;
            BankCardId = null;
            CryptoWalletId = null;
            SearchQuery = null;
            StartDate = null;
            EndDate = null;
            Data.Clear();
            TempData.Clear();
            AwaitingConfirmation = false;
            ConfirmationMessage = null;
            ConfirmationCallback = null;
            Touch();
        }
    }

    // НОВЫЙ: Статические классы с константами действий
    public static class UserActions
    {
        // Проекты
        public const string CreateProject = "create_project";
        public const string EditProject = "edit_project";
        public const string DeleteProject = "delete_project";
        public const string ChangeProjectStatus = "change_project_status";

        // Задачи
        public const string CreateTask = "create_task";
        public const string EditTask = "edit_task";
        public const string CompleteTask = "complete_task";
        public const string ReactivateTask = "reactivate_task";
        public const string ArchiveTask = "archive_task";
        public const string DeleteTask = "delete_task";

        // Финансы
        public const string AddIncome = "add_income";
        public const string AddExpense = "add_expense";
        public const string AddDeposit = "add_deposit";
        public const string WithdrawDeposit = "withdraw_deposit";
        public const string TransferFunds = "transfer_funds";
        public const string AddCommission = "add_commission";
        public const string AddInvestment = "add_investment";

        // Контакты
        public const string AddContact = "add_contact";
        public const string EditContact = "edit_contact";
        public const string SearchContact = "search_contact";
        public const string AddBankCard = "add_bank_card";
        public const string EditBankCard = "edit_bank_card";
        public const string DeleteBankCard = "delete_bank_card";
        public const string AddCryptoWallet = "add_crypto_wallet";
        public const string EditCryptoWallet = "edit_crypto_wallet";
        public const string DeleteCryptoWallet = "delete_crypto_wallet";

        // Статусы
        public const string WriteStatus = "write_status";
        public const string EditStatus = "edit_status";

        // Реклама
        public const string CreateContentPlan = "create_content_plan";
        public const string CreateCampaign = "create_campaign";
        public const string EditCampaign = "edit_campaign";

        // Настройки
        public const string ManageUsers = "manage_users";
        public const string ManageCategories = "manage_categories";
        public const string ManageNotifications = "manage_notifications";

        // KPI
        public const string ViewKpi = "view_kpi";
        public const string ExportKpi = "export_kpi";

        // База данных
        public const string ExportData = "export_data";
        public const string ImportData = "import_data";
        public const string BackupDatabase = "backup_database";

        // Общие
        public const string ConfirmAction = "confirm_action";
        public const string CancelAction = "cancel_action";
        public const string InputText = "input_text";
        public const string InputNumber = "input_number";
        public const string InputDate = "input_date";
        public const string SelectOption = "select_option";
    }

    // НОВЫЙ: Типы данных для состояний
    public static class UserStateDataKeys
    {
        // Общие
        public const string EntityId = "entity_id";
        public const string EntityName = "entity_name";
        public const string EntityType = "entity_type";

        // Проекты
        public const string ProjectName = "project_name";
        public const string ProjectDescription = "project_description";
        public const string ProjectLink = "project_link";
        public const string ProjectStatus = "project_status";

        // Задачи
        public const string TaskTitle = "task_title";
        public const string TaskDescription = "task_description";
        public const string TaskDueDate = "task_due_date";
        public const string TaskAssignedTo = "task_assigned_to";
        public const string TaskPriority = "task_priority";

        // Финансы
        public const string Amount = "amount";
        public const string Currency = "currency";
        public const string Category = "category";
        public const string Source = "source";
        public const string Description = "description";
        public const string FundStatus = "fund_status";
        public const string Commission = "commission";
        public const string CommissionPaidBy = "commission_paid_by";

        // Контакты
        public const string ContactUsername = "contact_username";
        public const string ContactFullName = "contact_full_name";
        public const string ContactType = "contact_type";
        public const string ContactTags = "contact_tags";
        public const string ContactNotes = "contact_notes";

        // Банковские карты
        public const string CardNumber = "card_number";
        public const string CardBank = "card_bank";
        public const string CardType = "card_type";
        public const string CardHolder = "card_holder";
        public const string CardIsPrimary = "card_is_primary";

        // Крипто-кошельки
        public const string WalletNetwork = "wallet_network";
        public const string WalletAddress = "wallet_address";
        public const string WalletLabel = "wallet_label";
        public const string WalletIsPrimary = "wallet_is_primary";

        // Реклама
        public const string CampaignTitle = "campaign_title";
        public const string CampaignBudget = "campaign_budget";
        public const string CampaignGoal = "campaign_goal";
        public const string CampaignPlatform = "campaign_platform";

        // Поиск
        public const string SearchTerm = "search_term";
        public const string SearchFilters = "search_filters";
        public const string PageNumber = "page_number";
        public const string PageSize = "page_size";

        // Даты
        public const string StartDate = "start_date";
        public const string EndDate = "end_date";
        public const string PeriodType = "period_type";
    }
}