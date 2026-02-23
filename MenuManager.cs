// MenuManager.cs - ИСПРАВЛЕННЫЙ И ДОПОЛНЕННЫЙ
using Microsoft.Extensions.Logging;
using TeamManagerBot.Keyboards;
using TeamManagerBot.Models;
using TeamManagerBot.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeamManagerBot.Handlers
{
    public class MenuManager
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<MenuManager> _logger;
        private readonly IUserService _userService;
        private readonly MenuStateManager _stateManager;
        private readonly Dictionary<long, DateTime> _lastMessageTime = new();

        public MenuManager(
            ITelegramBotClient botClient,
            ILogger<MenuManager> logger,
            IUserService userService,
            MenuStateManager stateManager)
        {
            _botClient = botClient;
            _logger = logger;
            _userService = userService;
            _stateManager = stateManager;
        }

        // Основной метод обновления/отправки меню
        public async Task<Message> ShowMainMenuAsync(long chatId, long userId, bool isAdmin, CancellationToken cancellationToken)
        {
            var text = $"👋 Добро пожаловать, {(isAdmin ? "👑 Администратор" : "👤 Участник")}!\n\n" +
                       $"Используйте кнопки ниже для навигации.";

            // Используем inline-клавиатуру вместо reply
            var keyboard = MainMenuKeyboard.GetMainMenu(isAdmin);

            // Отправляем как обычное сообщение с inline-кнопками
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "main", cancellationToken);
        }

        public async Task<Message> ShowInlineMenuAsync(
            long chatId,
            string text,
            InlineKeyboardMarkup keyboard,
            string menuType,
            CancellationToken cancellationToken,
            bool forceNew = false)
        {
            // Всегда forceNew = false для навигации, чтобы редактировать существующее меню
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, menuType, cancellationToken, false);
        }

        #region Методы для каждого типа меню
        public async Task<Message> ShowProjectsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📂 Модуль проектов\n\nВыберите действие:";
            var keyboard = MainMenuKeyboard.GetProjectsMenu();
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "projects", cancellationToken);
        }

        public async Task<Message> ShowTasksMenuAsync(long chatId, bool isAdmin, CancellationToken cancellationToken)
        {
            var text = "✅ Модуль задач\n\nВыберите действие:";
            var keyboard = MainMenuKeyboard.GetTasksMenu(isAdmin);
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "tasks", cancellationToken);
        }

        public async Task<Message> ShowStatusesMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📊 Модуль статусов\n\nВыберите действие:";
            var keyboard = MainMenuKeyboard.GetStatusesMenu();
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "statuses", cancellationToken);
        }

        public async Task<Message> ShowAdvertisementMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📢 Модуль рекламы\n\nВыберите раздел:";
            var keyboard = MainMenuKeyboard.GetAdvertisementMenu();
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "advertisement", cancellationToken);
        }

        public async Task<Message> ShowContactsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "👤 Управление контактами\n\nВыберите действие:";
            var keyboard = MainMenuKeyboard.GetContactsMenu();
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "contacts", cancellationToken);
        }

        public async Task<Message> ShowDatabaseMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "🗃️ База данных команды\n\nВыберите раздел:";
            var keyboard = MainMenuKeyboard.GetDatabaseMenu();
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "database", cancellationToken);
        }

        public async Task<Message> ShowFinanceMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "💰 Бухгалтерия команды\n\nВыберите раздел:";
            var keyboard = MainMenuKeyboard.GetFinanceMenu();
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "finance", cancellationToken);
        }

        public async Task<Message> ShowKPIMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📈 Модуль KPI\n\nВыберите отчет:";
            var keyboard = MainMenuKeyboard.GetKPIMenu();
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "kpi", cancellationToken);
        }

        public async Task<Message> ShowSettingsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "⚙️ НАСТРОЙКИ СИСТЕМЫ\n\n" +
                       "Управление пользователями, безопасность, отчеты и база данных.";

            var keyboard = MainMenuKeyboard.GetSettingsMenu();

            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "settings", cancellationToken, forceNew: false);
        }
        #endregion

        #region Детальные представления
        public async Task<Message> ShowProjectDetailsAsync(long chatId, Project project, CancellationToken cancellationToken, string returnContext = "projects")
        {
            var statusIcon = project.Status switch
            {
                ProjectStatus.Pending => "🟡 Предстоит",
                ProjectStatus.InProgress => "🟠 В работе",
                ProjectStatus.Completed => "✅ Готово",
                _ => "⚪ Неизвестно"
            };

            var tasks = project.Tasks?.ToList() ?? new List<TeamTask>();
            var activeTasks = tasks.Count(t => t.Status == TeamTaskStatus.Active);
            var completedTasks = tasks.Count(t => t.Status == TeamTaskStatus.Completed);

            var text = $"📂 Проект: {project.Name}\n\n" +
                      $"Описание: {project.Description ?? "Нет описания"}\n" +
                      $"Статус: {statusIcon}\n" +
                      $"Задачи: {activeTasks} активных, {completedTasks} выполнено\n" +
                      $"Создал: @{project.CreatedBy?.Username ?? "Неизвестно"}\n" +
                      $"Дата: {project.CreatedAt:dd.MM.yyyy}";

            if (!string.IsNullOrEmpty(project.Link))
                text += $"\nСсылка: {project.Link}";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"{CallbackData.EditProjectPrefix}{project.Id}") },
        new() { InlineKeyboardButton.WithCallbackData("📊 Сменить статус", $"{CallbackData.ChangeStatusPrefix}{project.Id}") },
        new() { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"{CallbackData.DeleteProjectPrefix}{project.Id}") },
    };

            // Кнопка назад зависит от контекста
            if (returnContext == "statuses")
            {
                buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("◀️ Назад к статусам", CallbackData.BackToStatuses) });
            }
            else
            {
                buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("◀️ Назад к проектам", CallbackData.BackToProjects) });
            }

            var keyboard = new InlineKeyboardMarkup(buttons);
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, $"project_{project.Id}", cancellationToken);
        }

        public async Task<Message> ShowTaskDetailsAsync(long chatId, TeamTask task, CancellationToken cancellationToken)
        {
            var statusText = task.Status switch
            {
                TeamTaskStatus.Active => "🟢 Активна",      // ← ИЗМЕНИЛОСЬ
                TeamTaskStatus.Completed => "✅ Выполнена",  // ← ИЗМЕНИЛОСЬ
                TeamTaskStatus.Archived => "📁 В архиве",   // ← ИЗМЕНИЛОСЬ
                _ => "❓ Неизвестно"
            };

            var text = $"📋 Задача: {task.Title}\n\n" +
                      $"Описание: {task.Description ?? "Нет описания"}\n" +
                      $"Статус: {statusText}\n" +
                      $"Проект: {task.Project?.Name ?? "Не указан"}\n" +
                      $"Назначена: @{task.AssignedTo?.Username ?? "Не назначена"}\n" +
                      $"Создал: @{task.CreatedBy?.Username ?? "Неизвестно"}\n" +
                      $"Создана: {task.CreatedAt:dd.MM.yyyy}";

            if (task.DueDate.HasValue)
            {
                var dueDate = task.DueDate.Value;
                var daysLeft = (dueDate.Date - DateTime.UtcNow.Date).Days;
                text += $"\nСрок: {dueDate:dd.MM.yyyy}";

                if (daysLeft < 0 && task.Status == TeamTaskStatus.Active)
                    text += " ⚠️ Просрочена!";
                else if (daysLeft <= 3 && task.Status == TeamTaskStatus.Active)
                    text += $" ⏰ Осталось {daysLeft} дн.";
            }

            var buttons = new List<List<InlineKeyboardButton>>();

            if (task.Status == TeamTaskStatus.Active)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("✅ Выполнить", $"{CallbackData.TaskCompletePrefix}{task.Id}")
                });
            }
            else if (task.Status == TeamTaskStatus.Completed)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("🔄 Вернуть в работу", $"task_reactivate_{task.Id}")
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks)
            });

            var keyboard = new InlineKeyboardMarkup(buttons);
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, $"task_{task.Id}", cancellationToken);
        }

        public async Task<Message> ShowDeleteConfirmationAsync(
            long chatId,
            string entityName,
            string entityDescription,
            string confirmCallback,
            string cancelCallback,
            CancellationToken cancellationToken)
        {
            var text = $"⚠️ Подтверждение удаления\n\n" +
                      $"Вы уверены, что хотите удалить {entityName}?\n\n" +
                      $"{entityDescription}\n\n" +
                      $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData("✅ Да, удалить", confirmCallback),
                    InlineKeyboardButton.WithCallbackData("❌ Нет, отмена", cancelCallback)
                }
            };

            var keyboard = new InlineKeyboardMarkup(buttons);
            return await UpdateOrSendMenuAsync(chatId, text, keyboard, "delete_confirmation", cancellationToken, true);
        }

        public async Task<Message> ShowContactsListAsync(long chatId, List<TeamContact> contacts, CancellationToken cancellationToken)
        {
            if (contacts.Count == 0)
            {
                return await ShowInlineMenuAsync(
                    chatId,
                    "📭 Список контактов пуст.\n\nДобавьте первый контакт!",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("➕ Добавить контакт", CallbackData.ContactsAdd),
                            InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToContacts)
                        }
                    }),
                    "contacts_list_empty",
                    cancellationToken);
            }

            var text = $"👥 Список контактов ({contacts.Count}):\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var contact in contacts.Take(15))
            {
                var displayName = !string.IsNullOrEmpty(contact.FullName)
                    ? $"{contact.FullName} (@{contact.TelegramUsername})"
                    : $"@{contact.TelegramUsername}";

                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"👤 {displayName}",
                        $"contact_{contact.Id}")
                });
            }

            if (contacts.Count > 15)
            {
                text += $"\n... и еще {contacts.Count - 15} контактов";
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToContacts)
            });

            return await ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "contacts_list",
                cancellationToken);
        }

        public async Task<Message> ShowContactDetailsAsync(long chatId, TeamContact contact, CancellationToken cancellationToken)
        {
            var text = $"👤 Контакт\n\n" +
                      $"Username: @{contact.TelegramUsername}\n";

            if (!string.IsNullOrEmpty(contact.FullName))
                text += $"Имя: {contact.FullName}\n";

            if (!string.IsNullOrEmpty(contact.Nickname))
                text += $"Псевдоним: {contact.Nickname}\n";

            if (!string.IsNullOrEmpty(contact.ContactType))
                text += $"Тип: {contact.ContactType}\n";

            if (!string.IsNullOrEmpty(contact.Tags))
                text += $"Теги: {contact.Tags}\n";

            text += $"\nДобавлен: {contact.CreatedAt:dd.MM.yyyy}";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"contact_edit_{contact.Id}") },
                new() { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"contact_delete_{contact.Id}") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToContacts) }
            };

            return await ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                $"contact_{contact.Id}",
                cancellationToken);
        }
        #endregion

        #region Вспомогательные методы
        private async Task<Message> UpdateOrSendMenuAsync(
    long chatId,
    string text,
    ReplyMarkup keyboard,
    string menuType,
    CancellationToken cancellationToken,
    bool forceNew = false)
        {
            try
            {
                Message message;
                var now = DateTime.UtcNow;

                // Проверяем, можно ли обновить существующее сообщение
                if (!forceNew && _stateManager.TryGetMenuMessage(chatId, out int messageId, out string currentMenuType))
                {
                    try
                    {
                        // Всегда пытаемся редактировать, если есть сообщение
                        if (keyboard is InlineKeyboardMarkup inlineKeyboard)
                        {
                            message = await _botClient.EditMessageText(
                                chatId: chatId,
                                messageId: messageId,
                                text: text,
                                replyMarkup: inlineKeyboard,
                                cancellationToken: cancellationToken);

                            _stateManager.SetMenuMessage(chatId, message.MessageId, menuType);
                            _lastMessageTime[chatId] = now;
                            return message;
                        }
                    }
                    catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
                    {
                        // Текст не изменился - это нормально, просто возвращаем null
                        _logger.LogDebug("Message not modified for chat {ChatId}", chatId);

                        // Получаем существующее сообщение
                        try
                        {
                            var existingMessage = await _botClient.SendMessage(
                                chatId: chatId,
                                text: ".",
                                cancellationToken: cancellationToken);
                            await _botClient.DeleteMessage(chatId, existingMessage.MessageId, cancellationToken);
                        }
                        catch { }

                        return null!;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error editing message, will try to send new");
                        // Продолжаем и отправляем новое
                    }
                }

                // Отправляем новое сообщение (только если forceNew или не удалось отредактировать)
                message = await SendNewMessageAsync(chatId, text, keyboard, cancellationToken);
                _stateManager.SetMenuMessage(chatId, message.MessageId, menuType);
                _lastMessageTime[chatId] = now;
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateOrSendMenuAsync for chat {ChatId}", chatId);
                throw;
            }
        }

        private async Task<Message> SendNewMessageAsync(
            long chatId,
            string text,
            ReplyMarkup keyboard,
            CancellationToken cancellationToken)
        {
            try
            {
                // НЕ УДАЛЯЕМ старое сообщение! Просто отправляем новое
                return await _botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new message to chat {ChatId}", chatId);
                throw;
            }
        }

        public async Task<Message> SendTemporaryMessageAsync(long chatId, string text, CancellationToken cancellationToken, int deleteAfterSeconds = 0)
        {
            try
            {
                var message = await _botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    cancellationToken: cancellationToken);

                // Удаляем ТОЛЬКО если deleteAfterSeconds > 0 (только для уведомлений)
                if (deleteAfterSeconds > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(deleteAfterSeconds * 1000, cancellationToken);
                            await _botClient.DeleteMessage(chatId, message.MessageId, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Could not delete temporary message in chat {ChatId}", chatId);
                        }
                    }, cancellationToken);
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending temporary message to chat {ChatId}", chatId);
                throw;
            }
        }

        public async Task SendTemporaryInlineMessageAsync(
            long chatId,
            string text,
            InlineKeyboardMarkup keyboard,
            CancellationToken cancellationToken,
            int deleteAfterSeconds = 30)
        {
            try
            {
                var message = await _botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);

                // Удаляем временное сообщение через указанное время
                if (deleteAfterSeconds > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(deleteAfterSeconds * 1000, cancellationToken);
                            await _botClient.DeleteMessage(chatId, message.MessageId, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Could not delete temporary message in chat {ChatId}", chatId);
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending temporary inline message to chat {ChatId}", chatId);
            }
        }

        public void ClearMenuState(long chatId)
        {
            _stateManager.ClearMenu(chatId);
            _lastMessageTime.Remove(chatId);
        }

        public bool TryGetCurrentMenu(long chatId, out string menuType)
        {
            if (_stateManager.TryGetMenuMessage(chatId, out _, out menuType))
            {
                return true;
            }

            menuType = string.Empty;
            return false;
        }

        public async Task CleanupOldMessagesAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Здесь можно добавить логику очистки старых сообщений
                // Например, удаление сообщений старше N часов
                ClearMenuState(chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old messages for chat {ChatId}", chatId);
            }
        }
        #endregion

        // Добавь эти методы в класс MenuManager (после существующих методов)

        #region Обработка действий в меню

        // ==================== БУХГАЛТЕРИЯ ====================
        public async Task HandleFinanceActionAsync(long chatId, string action, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "balance":
                    await ShowFinanceBalanceAsync(chatId, cancellationToken);
                    break;
                case "income":
                    await ShowFinanceIncomeAsync(chatId, cancellationToken);
                    break;
                case "expenses":
                    await ShowFinanceExpensesAsync(chatId, cancellationToken);
                    break;
                case "deposits":
                    await ShowFinanceDepositsAsync(chatId, cancellationToken);
                    break;
                case "commissions":
                    await ShowFinanceCommissionsAsync(chatId, cancellationToken);
                    break;
                case "investments":
                    await ShowFinanceInvestmentsAsync(chatId, cancellationToken);
                    break;
                case "add_income":
                    await StartAddIncomeAsync(chatId, cancellationToken);
                    break;
                case "add_expense":
                    await StartAddExpenseAsync(chatId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task ShowFinanceBalanceAsync(long chatId, CancellationToken cancellationToken)
        {
            // TODO: получить реальные данные из FinanceService
            var text = "💰 БАЛАНС\n\n" +
                      "Рабочие средства: 150 000 ₽\n" +
                      "Резерв: 50 000 ₽\n" +
                      "Заблокировано: 0 ₽\n" +
                      "───────────────\n" +
                      "ИТОГО: 200 000 ₽\n\n" +
                      "Выберите действие:";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("📊 Детали", "finance_details") },
        new[] { InlineKeyboardButton.WithCallbackData("📈 График", "finance_chart") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "finance_balance", cancellationToken, true);
        }

        private async Task ShowFinanceIncomeAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "💰 ДОХОДЫ\n\n" +
                      "Категории:\n" +
                      "• Продажи: 120 000 ₽\n" +
                      "• Услуги: 45 000 ₽\n" +
                      "• Партнерские: 15 000 ₽\n" +
                      "• Инвестиции: 20 000 ₽\n" +
                      "───────────────\n" +
                      "ВСЕГО: 200 000 ₽\n\n" +
                      "Последние 5 операций:\n" +
                      "• 15.02 Продажи +50 000 ₽\n" +
                      "• 14.02 Услуги +15 000 ₽\n" +
                      "• 13.02 Партнерские +5 000 ₽\n" +
                      "• 12.02 Продажи +30 000 ₽\n" +
                      "• 11.02 Инвестиции +20 000 ₽";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить доход", "finance_add_income") },
        new[] { InlineKeyboardButton.WithCallbackData("📊 По категориям", "finance_income_categories") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "finance_income", cancellationToken, true);
        }

        private async Task ShowFinanceExpensesAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "💰 РАСХОДЫ\n\n" +
                      "Категории:\n" +
                      "• Аренда: 30 000 ₽\n" +
                      "• Зарплата: 80 000 ₽\n" +
                      "• Реклама: 25 000 ₽\n" +
                      "• Инструменты: 15 000 ₽\n" +
                      "───────────────\n" +
                      "ВСЕГО: 150 000 ₽\n\n" +
                      "Последние 5 операций:\n" +
                      "• 15.02 Аренда -30 000 ₽\n" +
                      "• 14.02 Зарплата -40 000 ₽\n" +
                      "• 13.02 Реклама -10 000 ₽\n" +
                      "• 12.02 Инструменты -5 000 ₽\n" +
                      "• 11.02 Зарплата -40 000 ₽";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить расход", "finance_add_expense") },
        new[] { InlineKeyboardButton.WithCallbackData("📊 По категориям", "finance_expenses_categories") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "finance_expenses", cancellationToken, true);
        }

        private async Task ShowFinanceDepositsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "💰 ДЕПОЗИТЫ\n\n" +
                      "Рабочие депозиты:\n" +
                      "• Проект А: 100 000 ₽ (активен)\n" +
                      "• Проект Б: 50 000 ₽ (активен)\n\n" +
                      "Нерабочие депозиты:\n" +
                      "• Резерв: 30 000 ₽ (заморожен)\n\n" +
                      "История:\n" +
                      "• 10.02 Пополнение +20 000 ₽\n" +
                      "• 05.02 Вывод -10 000 ₽";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить депозит", "finance_add_deposit") },
        new[] { InlineKeyboardButton.WithCallbackData("🔄 Переместить", "finance_move_deposit") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "finance_deposits", cancellationToken, true);
        }

        private async Task ShowFinanceCommissionsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "💰 КОМИССИИ\n\n" +
                      "За последний месяц:\n" +
                      "• Банковские: 2 500 ₽\n" +
                      "• Крипто: 1 200 ₽\n" +
                      "• P2P: 800 ₽\n" +
                      "───────────────\n" +
                      "ВСЕГО: 4 500 ₽\n\n" +
                      "Статистика:\n" +
                      "• Средняя комиссия: 150 ₽\n" +
                      "• Всего операций: 30\n" +
                      "• Самая большая: 500 ₽";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("📊 Детализация", "finance_commission_details") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "finance_commissions", cancellationToken, true);
        }

        private async Task ShowFinanceInvestmentsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "💰 ВКЛАДЫ УЧАСТНИКОВ\n\n" +
                      "Участник А: 50 000 ₽\n" +
                      "Участник Б: 30 000 ₽\n" +
                      "Участник В: 20 000 ₽\n" +
                      "───────────────\n" +
                      "ВСЕГО: 100 000 ₽";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить вклад", "finance_add_investment") },
        new[] { InlineKeyboardButton.WithCallbackData("📊 История", "finance_investment_history") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "finance_investments", cancellationToken, true);
        }

        private async Task StartAddIncomeAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "➕ ДОБАВЛЕНИЕ ДОХОДА\n\n" +
                      "Введите данные в формате:\n" +
                      "категория | сумма | описание\n\n" +
                      "Пример: Продажи | 5000 | Продажа консультации\n\n" +
                      "Категории: Продажи, Услуги, Партнерские, Инвестиции";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Отмена", CallbackData.BackToFinance) }
    }), cancellationToken, 60);
        }

        private async Task StartAddExpenseAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "➕ ДОБАВЛЕНИЕ РАСХОДА\n\n" +
                      "Введите данные в формате:\n" +
                      "категория | сумма | описание\n\n" +
                      "Пример: Реклама | 2000 | Таргет ВК\n\n" +
                      "Категории: Аренда, Зарплата, Реклама, Инструменты, Комиссии";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Отмена", CallbackData.BackToFinance) }
    }), cancellationToken, 60);
        }

        // ==================== КОНТАКТЫ ====================
        public async Task HandleContactActionAsync(long chatId, string action, int contactId, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "edit":
                    await StartContactEditAsync(chatId, contactId, cancellationToken);
                    break;
                case "delete":
                    await ShowDeleteContactConfirmationAsync(chatId, contactId, cancellationToken);
                    break;
                case "add_bank":
                    await StartAddBankCardAsync(chatId, contactId, cancellationToken);
                    break;
                case "add_crypto":
                    await StartAddCryptoWalletAsync(chatId, contactId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task StartContactEditAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var text = "✏️ РЕДАКТИРОВАНИЕ КОНТАКТА\n\n" +
                      "Что хотите изменить?\n" +
                      "• Имя\n" +
                      "• Телефон\n" +
                      "• Email\n" +
                      "• Паспортные данные\n" +
                      "• Банковские карты\n" +
                      "• Крипто-кошельки";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("📝 Имя", $"contact_edit_name_{contactId}") },
        new[] { InlineKeyboardButton.WithCallbackData("📞 Телефон", $"contact_edit_phone_{contactId}") },
        new[] { InlineKeyboardButton.WithCallbackData("✉️ Email", $"contact_edit_email_{contactId}") },
        new[] { InlineKeyboardButton.WithCallbackData("🆔 Паспорт", $"contact_edit_passport_{contactId}") },
        new[] { InlineKeyboardButton.WithCallbackData("💳 Карты", $"contact_banks_{contactId}") },
        new[] { InlineKeyboardButton.WithCallbackData("₿ Крипто", $"contact_crypto_{contactId}") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", $"contact_{contactId}") }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, $"contact_edit_{contactId}", cancellationToken, true);
        }

        private async Task ShowDeleteContactConfirmationAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            await ShowDeleteConfirmationAsync(
                chatId,
                "контакт",
                $"ID: {contactId}",
                $"confirm_delete_contact_{contactId}",
                $"contact_{contactId}",
                cancellationToken);
        }

        private async Task StartAddBankCardAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var text = "💳 ДОБАВЛЕНИЕ БАНКОВСКОЙ КАРТЫ\n\n" +
                      "Введите данные в формате:\n" +
                      "номер|банк|тип|основная\n\n" +
                      "Пример: 1234|Тинькофф|debit|да\n\n" +
                      "Тип: debit/credit\n" +
                      "Основная: да/нет";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Отмена", $"contact_{contactId}") }
    }), cancellationToken, 60);
        }

        private async Task StartAddCryptoWalletAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var text = "₿ ДОБАВЛЕНИЕ КРИПТО-КОШЕЛЬКА\n\n" +
                      "Введите данные в формате:\n" +
                      "сеть|адрес|метка|основной\n\n" +
                      "Пример: TRX|TXYZ123...|Основной|да\n\n" +
                      "Сети: BTC, ETH, TRX, BSC\n" +
                      "Основной: да/нет";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Отмена", $"contact_{contactId}") }
    }), cancellationToken, 60);
        }

        // ==================== ЗАДАЧИ ====================
        public async Task HandleTaskActionAsync(long chatId, string action, int taskId, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "complete":
                    await CompleteTaskAsync(chatId, taskId, cancellationToken);
                    break;
                case "reactivate":
                    await ReactivateTaskAsync(chatId, taskId, cancellationToken);
                    break;
                case "edit":
                    await StartTaskEditAsync(chatId, taskId, cancellationToken);
                    break;
                case "delete":
                    await ShowDeleteTaskConfirmationAsync(chatId, taskId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task CompleteTaskAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            // TODO: отметить задачу выполненной в БД
            await SendTemporaryMessageAsync(chatId, "✅ Задача отмечена как выполненная!", cancellationToken);
            await ShowTasksMenuAsync(chatId, true, cancellationToken); // Вернуться к списку задач
        }

        private async Task ReactivateTaskAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            // TODO: вернуть задачу в работу
            await SendTemporaryMessageAsync(chatId, "🔄 Задача возвращена в работу!", cancellationToken);
            await ShowTasksMenuAsync(chatId, true, cancellationToken);
        }

        private async Task StartTaskEditAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            var text = "✏️ РЕДАКТИРОВАНИЕ ЗАДАЧИ\n\n" +
                      "Что хотите изменить?\n" +
                      "• Название\n" +
                      "• Описание\n" +
                      "• Срок\n" +
                      "• Исполнителя";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("📝 Название", $"task_edit_title_{taskId}") },
        new[] { InlineKeyboardButton.WithCallbackData("📋 Описание", $"task_edit_desc_{taskId}") },
        new[] { InlineKeyboardButton.WithCallbackData("📅 Срок", $"task_edit_date_{taskId}") },
        new[] { InlineKeyboardButton.WithCallbackData("👤 Исполнитель", $"task_edit_user_{taskId}") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", $"task_{taskId}") }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, $"task_edit_{taskId}", cancellationToken, true);
        }

        private async Task ShowDeleteTaskConfirmationAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            await ShowDeleteConfirmationAsync(
                chatId,
                "задачу",
                $"ID: {taskId}",
                $"confirm_delete_task_{taskId}",
                CallbackData.BackToTasks,
                cancellationToken);
        }

        // ==================== ПРОЕКТЫ ====================
        public async Task HandleProjectActionAsync(long chatId, string action, int projectId, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "add_task":
                    await StartAddTaskToProjectAsync(chatId, projectId, cancellationToken);
                    break;
                case "edit":
                    await StartProjectEditAsync(chatId, projectId, cancellationToken);
                    break;
                case "delete":
                    await ShowDeleteProjectConfirmationAsync(chatId, projectId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task StartAddTaskToProjectAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            var text = "➕ ДОБАВЛЕНИЕ ЗАДАЧИ В ПРОЕКТ\n\n" +
                      "Введите данные в формате:\n" +
                      "название | описание | срок(ДД.ММ.ГГГГ) | @исполнитель\n\n" +
                      "Пример: Дизайн | Сделать макет | 01.03.2024 | @username";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Отмена", $"project_{projectId}") }
    }), cancellationToken, 60);
        }

        private async Task StartProjectEditAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            var text = "✏️ РЕДАКТИРОВАНИЕ ПРОЕКТА\n\n" +
                      "Что хотите изменить?\n" +
                      "• Название\n" +
                      "• Описание\n" +
                      "• Статус\n" +
                      "• Ссылку";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("📝 Название", $"project_edit_name_{projectId}") },
        new[] { InlineKeyboardButton.WithCallbackData("📋 Описание", $"project_edit_desc_{projectId}") },
        new[] { InlineKeyboardButton.WithCallbackData("🔄 Статус", $"project_edit_status_{projectId}") },
        new[] { InlineKeyboardButton.WithCallbackData("🔗 Ссылка", $"project_edit_link_{projectId}") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", $"project_{projectId}") }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, $"project_edit_{projectId}", cancellationToken, true);
        }

        private async Task ShowDeleteProjectConfirmationAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            await ShowDeleteConfirmationAsync(
                chatId,
                "проект",
                $"ID: {projectId}",
                $"confirm_delete_project_{projectId}",
                CallbackData.BackToProjects,
                cancellationToken);
        }

        // ==================== РЕКЛАМА ====================
        public async Task HandleAdvertisementActionAsync(long chatId, string action, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "content_plan":
                    await ShowContentPlanAsync(chatId, cancellationToken);
                    break;
                case "ad_campaigns":
                    await ShowAdCampaignsAsync(chatId, cancellationToken);
                    break;
                case "add_campaign":
                    await StartAddCampaignAsync(chatId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task ShowContentPlanAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📢 КОНТЕНТ-ПЛАН\n\n" +
                      "Запланировано на неделю:\n" +
                      "• 20.02 - Пост о проекте\n" +
                      "• 22.02 - Кейс клиента\n" +
                      "• 24.02 - Анонс обновления\n\n" +
                      "Выполнено:\n" +
                      "• 19.02 - Новостной дайджест ✅\n" +
                      "• 18.02 - Интервью ✅";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить пост", "ad_add_post") },
        new[] { InlineKeyboardButton.WithCallbackData("📊 Статистика", "ad_stats") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToAdvertisement) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "ad_content_plan", cancellationToken, true);
        }

        private async Task ShowAdCampaignsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📢 РЕКЛАМНЫЕ КАМПАНИИ\n\n" +
                      "Активные:\n" +
                      "• Таргет ВК - 15 000 ₽ (CTR 2.5%)\n" +
                      "• Telegram Ads - 10 000 ₽ (CTR 3.1%)\n\n" +
                      "Завершенные:\n" +
                      "• Яндекс.Директ - 20 000 ₽ (лидов: 15)";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Новая кампания", "ad_add_campaign") },
        new[] { InlineKeyboardButton.WithCallbackData("📊 Отчеты", "ad_reports") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToAdvertisement) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "ad_campaigns", cancellationToken, true);
        }

        private async Task StartAddCampaignAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "➕ НОВАЯ РЕКЛАМНАЯ КАМПАНИЯ\n\n" +
                      "Введите данные в формате:\n" +
                      "название | бюджет | площадка | цель\n\n" +
                      "Пример: Весна2024 | 15000 | ВК | Лиды";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Отмена", CallbackData.BackToAdvertisement) }
    }), cancellationToken, 60);
        }

        // ==================== KPI ====================
        public async Task HandleKPIActionAsync(long chatId, string action, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "summary":
                    await ShowKPISummaryAsync(chatId, cancellationToken);
                    break;
                case "projects":
                    await ShowKPIProjectsAsync(chatId, cancellationToken);
                    break;
                case "team":
                    await ShowKPITeamAsync(chatId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task ShowKPISummaryAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📈 KPI ОБЩИЙ\n\n" +
                      "Проекты:\n" +
                      "• Активные: 5\n" +
                      "• Завершенные: 3\n" +
                      "• Выполнение плана: 75%\n\n" +
                      "Задачи:\n" +
                      "• Выполнено: 45/60 (75%)\n" +
                      "• Просрочено: 5\n\n" +
                      "Финансы:\n" +
                      "• Доход: 850 000 ₽\n" +
                      "• Расход: 520 000 ₽\n" +
                      "• Прибыль: 330 000 ₽";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("📊 Детализация", "kpi_details") },
        new[] { InlineKeyboardButton.WithCallbackData("📈 График", "kpi_chart") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToKpi) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "kpi_summary", cancellationToken, true);
        }

        private async Task ShowKPIProjectsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📈 KPI ПО ПРОЕКТАМ\n\n" +
                      "• Проект А: 95% (выполнение)\n" +
                      "• Проект Б: 80% (выполнение)\n" +
                      "• Проект В: 60% (выполнение)\n" +
                      "• Проект Г: 45% (выполнение)\n\n" +
                      "Средний: 70%";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToKpi) }
    }), cancellationToken);
        }

        private async Task ShowKPITeamAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📈 KPI КОМАНДЫ\n\n" +
                      "• @user1: 95% (12/13 задач)\n" +
                      "• @user2: 85% (17/20 задач)\n" +
                      "• @user3: 70% (7/10 задач)\n" +
                      "• @user4: 65% (13/20 задач)";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToKpi) }
    }), cancellationToken);
        }

        // ==================== СТАТУСЫ ====================
        public async Task HandleStatusActionAsync(long chatId, string action, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "projects":
                    await ShowStatusProjectsAsync(chatId, cancellationToken);
                    break;
                case "tasks":
                    await ShowStatusTasksAsync(chatId, cancellationToken);
                    break;
                case "graph":
                    await ShowStatusGraphAsync(chatId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task ShowStatusProjectsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📊 СТАТУСЫ ПРОЕКТОВ\n\n" +
                      "🟡 Предстоит:\n" +
                      "• Проект Г (старт 01.03)\n\n" +
                      "🟠 В работе:\n" +
                      "• Проект А (75%)\n" +
                      "• Проект Б (50%)\n" +
                      "• Проект В (30%)\n\n" +
                      "✅ Готово:\n" +
                      "• Проект Д (завершен 15.02)\n" +
                      "• Проект Е (завершен 10.02)";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses) }
    }), cancellationToken);
        }

        private async Task ShowStatusTasksAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📊 СТАТУСЫ ЗАДАЧ\n\n" +
                      "🟢 Активные: 15\n" +
                      "• @user1: 5 задач\n" +
                      "• @user2: 4 задачи\n" +
                      "• @user3: 3 задачи\n\n" +
                      "✅ Выполненные: 45\n\n" +
                      "⚠️ Просроченные: 3\n" +
                      "• Задача 1 (срок 10.02)\n" +
                      "• Задача 2 (срок 12.02)";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses) }
    }), cancellationToken);
        }

        private async Task ShowStatusGraphAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📊 ГРАФИК ВЫПОЛНЕНИЯ\n\n" +
                      "📅 Последние 7 дней:\n" +
                      "ПН: ████████░░ 8/10\n" +
                      "ВТ: ███████░░░ 7/10\n" +
                      "СР: ██████████ 10/10\n" +
                      "ЧТ: ███████░░░ 7/10\n" +
                      "ПТ: █████████░ 9/10\n" +
                      "СБ: ██████░░░░ 6/10\n" +
                      "ВС: ████████░░ 8/10\n\n" +
                      "Средний: 79%";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("📈 Неделя", "status_graph_week") },
        new[] { InlineKeyboardButton.WithCallbackData("📊 Месяц", "status_graph_month") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses) }
    }), cancellationToken);
        }

        // ==================== БАЗА ДАННЫХ ====================
        public async Task HandleDatabaseActionAsync(long chatId, string action, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "stats":
                    await ShowDatabaseStatsAsync(chatId, cancellationToken);
                    break;
                case "backup":
                    await CreateBackupAsync(chatId, cancellationToken);
                    break;
                case "export":
                    await ShowExportOptionsAsync(chatId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task ShowDatabaseStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "🗃️ СТАТИСТИКА БАЗЫ ДАННЫХ\n\n" +
                      "Пользователи: 25\n" +
                      "Контакты: 42\n" +
                      "Проекты: 8\n" +
                      "Задачи: 67\n" +
                      "Фин. записи: 156\n" +
                      "Рекламные кампании: 12\n\n" +
                      "Последнее обновление: сейчас";

            await SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToDatabase) }
    }), cancellationToken);
        }

        private async Task CreateBackupAsync(long chatId, CancellationToken cancellationToken)
        {
            // TODO: реальное создание бэкапа
            await SendTemporaryMessageAsync(chatId, "✅ Бэкап создан!\nФайл: backup_20240220.zip", cancellationToken, 10);
            await ShowDatabaseMenuAsync(chatId, cancellationToken);
        }

        private async Task ShowExportOptionsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📤 ЭКСПОРТ ДАННЫХ\n\n" +
                      "Выберите что экспортировать:";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("👤 Контакты (CSV)", "export_contacts") },
        new[] { InlineKeyboardButton.WithCallbackData("💰 Финансы (CSV)", "export_finance") },
        new[] { InlineKeyboardButton.WithCallbackData("📊 Проекты (CSV)", "export_projects") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToDatabase) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "export_options", cancellationToken, true);
        }

        // ==================== НАСТРОЙКИ ====================
        public async Task HandleSettingsActionAsync(long chatId, string action, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case "categories":
                    await ShowCategoriesSettingsAsync(chatId, cancellationToken);
                    break;
                case "users":
                    await ShowUsersSettingsAsync(chatId, cancellationToken);
                    break;
                case "notifications":
                    await ShowNotificationSettingsAsync(chatId, cancellationToken);
                    break;
                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие", cancellationToken);
                    break;
            }
        }

        private async Task ShowCategoriesSettingsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "⚙️ НАСТРОЙКИ КАТЕГОРИЙ\n\n" +
                      "Доходы:\n" +
                      "• Продажи\n" +
                      "• Услуги\n" +
                      "• Партнерские\n" +
                      "• Инвестиции\n\n" +
                      "Расходы:\n" +
                      "• Аренда\n" +
                      "• Зарплата\n" +
                      "• Реклама\n" +
                      "• Инструменты\n" +
                      "• Комиссии\n\n" +
                      "Выберите действие:";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить категорию", "settings_add_category") },
        new[] { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", "settings_edit_category") },
        new[] { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", "settings_delete_category") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToSettings) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "settings_categories", cancellationToken, true);
        }

        private async Task ShowUsersSettingsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "⚙️ УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ\n\n" +
                      "Администраторы:\n" +
                      "• @admin1\n" +
                      "• @admin2\n\n" +
                      "Участники:\n" +
                      "• @user1\n" +
                      "• @user2\n" +
                      "• @user3\n" +
                      "• @user4\n\n" +
                      "Всего: 6 пользователей";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить админа", "settings_add_admin") },
        new[] { InlineKeyboardButton.WithCallbackData("👥 Все участники", "settings_all_users") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToSettings) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "settings_users", cancellationToken, true);
        }

        private async Task ShowNotificationSettingsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "⚙️ НАСТРОЙКИ УВЕДОМЛЕНИЙ\n\n" +
                      "✓ Уведомления о задачах\n" +
                      "✓ Уведомления о проектах\n" +
                      "✗ Уведомления о финансах\n" +
                      "✓ Ежедневный дайджест\n\n" +
                      "Часовой пояс: UTC+3 (Москва)";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("🔔 Задачи", "settings_notif_tasks") },
        new[] { InlineKeyboardButton.WithCallbackData("💰 Финансы", "settings_notif_finance") },
        new[] { InlineKeyboardButton.WithCallbackData("📊 Дайджест", "settings_notif_digest") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToSettings) }
    });

            await UpdateOrSendMenuAsync(chatId, text, keyboard, "settings_notifications", cancellationToken, true);
        }

        #endregion
    }
}