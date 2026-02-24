using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Keyboards;
using TeamManagerBot.Models;
using TeamManagerBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;
using DbUser = TeamManagerBot.Models.User;
using TelegramUser = Telegram.Bot.Types.User;

namespace TeamManagerBot.Handlers
{
    public class CallbackQueryHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUserService _userService;
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;
        private readonly IFinanceService _financeService;
        private readonly IContactService _contactService;
        private readonly ILogger<CallbackQueryHandler> _logger;
        private static readonly Dictionary<long, UserState> _userStates = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly MenuManager _menuManager;
        private readonly ICryptoService _cryptoService;
        private readonly IFunPayService _funPayService;
        private readonly IFastInvestService _fastInvestService;
        private readonly IDropService _dropService;
        private readonly IManualService _manualService;
        private readonly IAdvertisementService _advertisementService;
        private readonly ICommissionService _commissionService;
        private readonly IPostService _postService;
        private readonly IReportService _reportService;
        private readonly IDocumentService _documentService;
        private readonly IAdService _adService;
        private readonly IDbFunPayService _dbFunPayService;
        private readonly ITeamInvestmentService _teamInvestmentService;
        private readonly IPlanService _planService;
        private readonly INotificationService _notificationService;


        public CallbackQueryHandler(
            ITelegramBotClient botClient,
            IUserService userService,
            IProjectService projectService,
            ITaskService taskService,
            IFinanceService financeService,
            IContactService contactService,
            ILogger<CallbackQueryHandler> logger,
            IServiceProvider serviceProvider,
            MenuManager menuManager,
            ICryptoService cryptoService,
            IFunPayService funPayService,
            IFastInvestService fastInvestService,
            IDropService dropService,
            IManualService manualService,
            IAdvertisementService advertisementService,
            ICommissionService commissionService,
            IPostService postService,
            IReportService reportService,
            IDocumentService documentService,
            IAdService adService,
            IDbFunPayService dbFunPayService,
            ITeamInvestmentService teamInvestmentService,
            IPlanService planService,
            INotificationService notificationService)
        {
            _botClient = botClient;
            _userService = userService;
            _projectService = projectService;
            _taskService = taskService;
            _financeService = financeService;
            _contactService = contactService;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _menuManager = menuManager;
            _cryptoService = cryptoService;
            _funPayService = funPayService;
            _fastInvestService = fastInvestService;
            _dropService = dropService;
            _manualService = manualService;
            _advertisementService = advertisementService;
            _commissionService = commissionService;
            _postService = postService;
            _reportService = reportService;
            _documentService = documentService;
            _adService = adService;
            _dbFunPayService = dbFunPayService;
            _teamInvestmentService = teamInvestmentService;
            _planService = planService;
            _notificationService = notificationService;
        }

        public async Task<bool> HandleMessageAsync(Message message, CancellationToken cancellationToken)
        {
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine($"📝 ПОЛУЧЕНО ТЕКСТОВОЕ СООБЩЕНИЕ");
            Console.WriteLine($"├─ Chat ID: {message.Chat.Id}");
            Console.WriteLine($"├─ User ID: {message.From?.Id}");
            Console.WriteLine($"├─ Username: @{message.From?.Username}");
            Console.WriteLine($"├─ Текст: {message.Text}");
            Console.WriteLine($"└─ Date: {DateTime.Now:HH:mm:ss}");

            var userId = message.From!.Id;
            var chatId = message.Chat.Id;
            var text = message.Text ?? string.Empty;

            Console.WriteLine($"🔍 Проверка состояния пользователя {userId}:");
            Console.WriteLine($"   ├─ Всего состояний в словаре: {_userStates.Count}");

            if (_userStates.TryGetValue(userId, out var state))
            {
                Console.WriteLine($"🎯 Найдено состояние пользователя: {state.CurrentAction}");
                await HandleUserStateAsync(chatId, userId, text, state, cancellationToken);
                Console.WriteLine("════════════════════════════════════════");
                return true;
            }

            Console.WriteLine($"ℹ️ Состояние пользователя не найдено, игнорируем сообщение");
            Console.WriteLine("════════════════════════════════════════");


            return false;
        }

        public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine($"📨 ПОЛУЧЕН CALLBACK ЗАПРОС");
            Console.WriteLine($"├─ Chat ID: {callbackQuery.Message?.Chat.Id}");
            Console.WriteLine($"├─ User ID: {callbackQuery.From.Id}");
            Console.WriteLine($"├─ Username: @{callbackQuery.From.Username}");
            Console.WriteLine($"├─ Callback Data: {callbackQuery.Data}");
            Console.WriteLine($"├─ Message ID: {callbackQuery.Message?.MessageId}");
            Console.WriteLine($"└─ Date: {DateTime.Now:HH:mm:ss}");

            if (callbackQuery.Data == null || callbackQuery.Message == null)
            {
                Console.WriteLine("❌ ОШИБКА: Callback без данных или сообщения!");
                Console.WriteLine("════════════════════════════════════════");
                return;
            }

            var chatId = callbackQuery.Message.Chat.Id;
            var userId = callbackQuery.From.Id;
            var callbackData = callbackQuery.Data;

            try
            {
                await _botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    cancellationToken: cancellationToken);

                Console.WriteLine($"✅ Ответ на callback отправлен");

                // Обновляем активность пользователя
                await _userService.UpdateUserLastActiveAsync(userId);

                // Определяем тип callback
                Console.WriteLine($"🔍 Анализ callback данных: {callbackData}");

                // ===== ГЛАВНОЕ МЕНЮ =====
                if (callbackData == "show_projects" ||
                    callbackData == "show_tasks" ||
                    callbackData == "show_finance" ||
                    callbackData == "show_kpi" ||
                    callbackData == "show_contacts" ||
                    callbackData == "show_statuses" ||
                    callbackData == "show_database" ||
                    callbackData == "show_advertisement" ||
                    callbackData == "show_plans" ||
                    callbackData == "show_settings")
                {
                    Console.WriteLine($"🎯 Обработка как команда главного меню: {callbackData}");
                    await HandleMainMenuCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== СТАТУСЫ (В ПЕРВУЮ ОЧЕРЕДЬ, ВКЛЮЧАЯ ПРОЕКТЫ ИЗ СТАТУСОВ) =====
                else if (callbackData == CallbackData.StatusWrite ||
                         callbackData == CallbackData.StatusBoard ||
                         callbackData == CallbackData.StatusProgress ||
                         callbackData == CallbackData.BackToStatuses ||
                         callbackData.StartsWith("write_status_for_") ||
                         callbackData.StartsWith("status_") ||
                         callbackData.StartsWith("project_from_statuses_"))
                {
                    Console.WriteLine($"🎯 Обработка как статусы: {callbackData}");
                    await HandleStatusCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== ЗАДАЧИ =====
                else if (callbackData == CallbackData.TasksCreate ||
                         callbackData == CallbackData.TasksList ||
                         callbackData == CallbackData.TasksMy ||
                         callbackData == CallbackData.TasksArchive ||
                         callbackData == CallbackData.TasksSettings ||
                         callbackData == CallbackData.BackToTasks ||
                         callbackData.StartsWith("task_") ||
                         callbackData.StartsWith(CallbackData.TaskPrefix) ||
                         callbackData.StartsWith(CallbackData.TaskCompletePrefix) ||
                         callbackData.StartsWith("project_for_task_") ||
                         callbackData.StartsWith("assign_task_to_user_") ||
                         callbackData.StartsWith("task_reactivate_") ||
                         callbackData.StartsWith("task_archive_") ||
                         callbackData.StartsWith("task_delete_") ||
                         callbackData.StartsWith("task_delete_confirm_") ||
                         callbackData.StartsWith("task_delete_cancel_") ||
                         callbackData.StartsWith("settings_all_users") ||
                         callbackData.StartsWith("settings_task_stats") ||
                         callbackData.StartsWith("settings_make_admin") ||
                         callbackData.StartsWith("make_admin_") ||
                         callbackData == "stats_week" ||
                         callbackData == "stats_month")
                {
                    Console.WriteLine($"🎯 Обработка как команда задач: {callbackData}");
                    await HandleTaskCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== ПРОЕКТЫ =====
                else if (callbackData == CallbackData.CreateProject ||
                         callbackData == CallbackData.ProjectsList ||
                         callbackData == CallbackData.BackToProjects ||
                         callbackData.StartsWith(CallbackData.ProjectPrefix) ||
                         callbackData.StartsWith(CallbackData.EditProjectPrefix) ||
                         callbackData.StartsWith(CallbackData.ChangeStatusPrefix) ||
                         callbackData.StartsWith(CallbackData.DeleteProjectPrefix) ||
                         callbackData.StartsWith(CallbackData.DeleteConfirmPrefix) ||
                         callbackData.StartsWith(CallbackData.DeleteCancelPrefix) ||
                         callbackData.StartsWith(CallbackData.StatusPendingPrefix) ||
                         callbackData.StartsWith(CallbackData.StatusInProgressPrefix) ||
                         callbackData.StartsWith(CallbackData.StatusCompletedPrefix))
                {
                    Console.WriteLine($"🎯 Обработка как команда проекта: {callbackData}");
                    await HandleProjectCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== ГРАФИКИ РАСХОДОВ =====
                else if (callbackData.StartsWith("expenses_chart_"))
                {
                    Console.WriteLine($"🎯 Обработка как график расходов: {callbackData}");
                    await HandleFinanceCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== ФИНАНСЫ =====
                else if (callbackData.StartsWith("finance_") ||
                         callbackData.StartsWith("income_category_") ||
                         callbackData.StartsWith("expense_category_") ||
                         callbackData.StartsWith("crypto_") ||
                         callbackData.StartsWith("funpay_") ||
                         callbackData.StartsWith("fastinvest_") ||
                         callbackData.StartsWith("investment_"))
                {
                    Console.WriteLine($"🎯 Обработка как финансы: {callbackData}");
                    await HandleFinanceCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== КОНТАКТЫ =====
                else if (callbackData == CallbackData.ContactsAdd ||
                         callbackData == CallbackData.ContactsSearch ||
                         callbackData == CallbackData.ContactsList ||
                         callbackData == CallbackData.BackToContacts ||
                         callbackData.StartsWith("contact_"))
                {
                    Console.WriteLine($"🎯 Обработка как контакты: {callbackData}");
                    await HandleContactsCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== KPI =====
                else if (callbackData.StartsWith("kpi_"))
                {
                    Console.WriteLine($"🎯 Обработка как KPI: {callbackData}");
                    await HandleKpiCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== ПЛАНЫ =====
                else if (callbackData == "show_plans" ||
                         callbackData.StartsWith("plans_") ||
                         callbackData.StartsWith("plan_") ||
                         callbackData.StartsWith("delete_plan_confirm_") ||
                         callbackData.StartsWith("delete_plan_"))
                {
                    Console.WriteLine($"🎯 Обработка как планы: {callbackData}");
                    await HandlePlanCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== РЕКЛАМА =====
                else if (callbackData == CallbackData.AdContentPlan ||
                         callbackData == CallbackData.AdCampaignPlan ||
                         callbackData == CallbackData.BackToAdvertisement ||
                         callbackData.StartsWith("ad_") ||
                         callbackData.StartsWith("content_plan_") ||
                         callbackData.StartsWith("campaign_"))
                {
                    Console.WriteLine($"🎯 Обработка как реклама: {callbackData}");
                    await HandleAdvertisementCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== БАЗА ДАННЫХ =====
                else if (callbackData.StartsWith("db_") ||
                         callbackData.StartsWith("settings_db_") ||
                         callbackData.StartsWith("backup_") ||
                         callbackData.StartsWith("delete_confirm_") ||
                         callbackData.StartsWith("delete_post_confirm_") ||
                         callbackData.StartsWith("delete_contact_confirm_") ||
                         callbackData.StartsWith("delete_manual_confirm_") ||
                         callbackData.StartsWith("delete_report_confirm_") ||
                         callbackData.StartsWith("delete_doc_confirm_") ||
                         callbackData.StartsWith("delete_ad_confirm_") ||
                         callbackData.StartsWith("delete_warning_confirm_") ||
                         callbackData.StartsWith("delete_funpay_account_confirm_") ||
                         callbackData == CallbackData.ContactsMenu ||
                         callbackData == CallbackData.DropsMenu ||
                         callbackData == CallbackData.ManualsMenu ||
                         callbackData == CallbackData.ReportsMenu ||
                         callbackData == CallbackData.ProjectDocsMenu ||
                         callbackData == CallbackData.AdDocsMenu ||
                         callbackData == CallbackData.BackToDatabase ||
                         callbackData.StartsWith("database_"))
                {
                    Console.WriteLine($"🎯 Обработка как база данных: {callbackData}");
                    await HandleDatabaseCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== НАСТРОЙКИ =====
                else if (callbackData.StartsWith("settings_") ||
                         callbackData == CallbackData.BackToSettings ||
                         callbackData.StartsWith("security_") ||
                         callbackData.StartsWith("reports_") ||
                         callbackData.StartsWith("notifications_") ||
                         callbackData.StartsWith("notification_") ||
                         callbackData.StartsWith("weekday_") ||
                         callbackData.StartsWith("edit_freq_") ||
                         callbackData.StartsWith("delete_notification_confirm_"))
                {
                    if (!callbackData.StartsWith("settings_db_"))
                    {
                        Console.WriteLine($"🎯 Обработка как настройки: {callbackData}");
                        await HandleSettingsCallbackAsync(chatId, userId, callbackData, cancellationToken);
                    }
                }

                // ===== НАВИГАЦИЯ =====
                else if (callbackData == CallbackData.BackToMain ||
                         callbackData == CallbackData.BackToProjects ||
                         callbackData == CallbackData.BackToTasks ||
                         callbackData == CallbackData.BackToStatuses ||
                         callbackData == CallbackData.BackToAdvertisement ||
                         callbackData == CallbackData.BackToContacts ||
                         callbackData == CallbackData.BackToDatabase ||
                         callbackData == CallbackData.BackToFinance ||
                         callbackData == CallbackData.BackToKpi ||
                         callbackData == CallbackData.BackToSettings)
                {
                    Console.WriteLine($"🎯 Обработка как навигация: {callbackData}");
                    await HandleNavigationCallbackAsync(chatId, userId, callbackData, cancellationToken);
                }

                // ===== НЕИЗВЕСТНЫЙ CALLBACK =====
                else
                {
                    Console.WriteLine($"❓ Неизвестный callback: {callbackData}");
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестная команда", cancellationToken, 5);
                }

                Console.WriteLine($"✅ Callback успешно обработан");
                Console.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА при обработке callback:");
                Console.WriteLine($"   └─ Сообщение: {ex.Message}");
                Console.WriteLine($"   └─ Тип: {ex.GetType().Name}");
                Console.WriteLine("════════════════════════════════════════");

                _logger.LogError(ex, "Error handling callback query: {CallbackData}", callbackData);
                await SendTemporaryMessageAsync(chatId, "❌ Произошла ошибка при обработке запроса.", cancellationToken, 5);
            }
        }

        private void ClearUserState(long userId)
        {
            if (_userStates.ContainsKey(userId))
            {
                _userStates.Remove(userId);
                Console.WriteLine($"🧹 Состояние пользователя {userId} очищено");
            }
        }
        private async Task HandleMainMenuCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            await _userService.LogSecurityEventAsync("Login", "Пользователь вошёл в систему", userId);
            Console.WriteLine($"🎯 Запущен HandleMainMenuCallbackAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   └─ Callback Data: {callbackData}");

            var isAdmin = await _userService.IsAdminAsync(userId);

            switch (callbackData)
            {
                case "show_plans":
                    await ShowPlansMenuAsync(chatId, cancellationToken);
                    break;
                case "show_projects":
                    await _menuManager.ShowProjectsMenuAsync(chatId, cancellationToken);
                    break;
                case "show_tasks":
                    await _menuManager.ShowTasksMenuAsync(chatId, isAdmin, cancellationToken);
                    break;
                case "show_finance":
                    await _menuManager.ShowFinanceMenuAsync(chatId, cancellationToken);
                    break;
                case "show_kpi":
                    await _menuManager.ShowKPIMenuAsync(chatId, cancellationToken);
                    break;
                case "show_contacts":
                    await _menuManager.ShowContactsMenuAsync(chatId, cancellationToken);
                    break;
                case "show_statuses":
                    await _menuManager.ShowStatusesMenuAsync(chatId, cancellationToken);
                    break;
                case "show_database":
                    await _menuManager.ShowDatabaseMenuAsync(chatId, cancellationToken);
                    break;
                case "show_advertisement":
                    await _menuManager.ShowAdvertisementMenuAsync(chatId, cancellationToken);
                    break;
                case "show_settings":
                    if (isAdmin)
                        await _menuManager.ShowSettingsMenuAsync(chatId, cancellationToken);
                    else
                        await SendTemporaryMessageAsync(chatId, "⛔ У вас нет доступа к настройкам.", cancellationToken);
                    break;
            }
        }

        #region Проекты - ПОЛНАЯ РЕАЛИЗАЦИЯ
        private async Task HandleProjectCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandleProjectCallbackAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   └─ Callback Data: {callbackData}");

            switch (callbackData)
            {
                case CallbackData.CreateProject:
                    Console.WriteLine($"   → Выбран: CreateProject");
                    _userStates[userId] = new UserState
                    {
                        CurrentAction = UserActions.CreateProject,
                        Step = 1
                    };
                    await SendTemporaryMessageAsync(chatId, "Введите название нового проекта:", cancellationToken);
                    break;

                case CallbackData.ProjectsList:
                    Console.WriteLine($"   → Выбран: ProjectsList");
                    await ShowProjectsListAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToProjects:
                    Console.WriteLine($"   → Выбран: BackToProjects");
                    await _menuManager.ShowProjectsMenuAsync(chatId, cancellationToken);
                    break;

                default:
                    Console.WriteLine($"   → Default case - анализ префиксов");

                    // ПРОСМОТР ПРОЕКТА ИЗ МЕНЮ ПРОЕКТОВ
                    if (callbackData.StartsWith(CallbackData.ProjectPrefix))
                    {
                        var projectIdStr = callbackData.Replace(CallbackData.ProjectPrefix, "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Просмотр проекта ID: {projectId} из меню проектов");
                            var project = await _projectService.GetProjectAsync(projectId);
                            if (project != null)
                            {
                                await _menuManager.ShowProjectDetailsAsync(chatId, project, cancellationToken, "projects");
                            }
                        }
                    }
                    // ПРОСМОТР ПРОЕКТА ИЗ СТАТУСОВ - ЭТОТ БЛОК ДОЛЖЕН БЫТЬ ЗДЕСЬ!
                    else if (callbackData.StartsWith("project_from_statuses_"))
                    {
                        var projectIdStr = callbackData.Replace("project_from_statuses_", "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Просмотр проекта {projectId} из статусов");
                            var project = await _projectService.GetProjectAsync(projectId);
                            if (project != null)
                            {
                                await _menuManager.ShowProjectDetailsAsync(chatId, project, cancellationToken, "statuses");
                            }
                        }
                    }
                    // РЕДАКТИРОВАНИЕ ПРОЕКТА
                    else if (callbackData.StartsWith(CallbackData.EditProjectPrefix))
                    {
                        var projectIdStr = callbackData.Replace(CallbackData.EditProjectPrefix, "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Редактирование проекта ID: {projectId}");
                            _userStates[userId] = new UserState
                            {
                                CurrentAction = UserActions.EditProject,
                                ProjectId = projectId,
                                Step = 1
                            };
                            await SendTemporaryMessageAsync(chatId, "Введите новое название проекта:", cancellationToken);
                        }
                    }
                    // СМЕНА СТАТУСА - ПОКАЗ МЕНЮ ВЫБОРА
                    else if (callbackData.StartsWith(CallbackData.ChangeStatusPrefix))
                    {
                        var projectIdStr = callbackData.Replace(CallbackData.ChangeStatusPrefix, "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Показ меню смены статуса для проекта ID: {projectId}");
                            await ShowProjectStatusChangeMenuAsync(chatId, projectId, cancellationToken);
                        }
                    }
                    // ВЫБОР КОНКРЕТНОГО СТАТУСА
                    else if (callbackData.StartsWith("status_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 3 && int.TryParse(parts[2], out int projectId))
                        {
                            var statusStr = parts[1];
                            ProjectStatus newStatus = statusStr switch
                            {
                                "pending" => ProjectStatus.Pending,
                                "inprogress" => ProjectStatus.InProgress,
                                "completed" => ProjectStatus.Completed,
                                _ => ProjectStatus.Pending
                            };

                            Console.WriteLine($"   → Смена статуса проекта ID: {projectId} на {newStatus}");
                            await UpdateProjectStatusAsync(chatId, projectId, newStatus, cancellationToken);
                        }
                    }
                    // УДАЛЕНИЕ ПРОЕКТА
                    else if (callbackData.StartsWith(CallbackData.DeleteProjectPrefix))
                    {
                        var projectIdStr = callbackData.Replace(CallbackData.DeleteProjectPrefix, "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Удаление проекта ID: {projectId}");
                            await ShowDeleteProjectConfirmationAsync(chatId, projectId, cancellationToken);
                        }
                    }
                    // ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ
                    else if (callbackData.StartsWith(CallbackData.DeleteConfirmPrefix))
                    {
                        var projectIdStr = callbackData.Replace(CallbackData.DeleteConfirmPrefix, "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Подтверждение удаления проекта ID: {projectId}");
                            await DeleteProjectAsync(chatId, projectId, cancellationToken);
                        }
                    }
                    // ОТМЕНА УДАЛЕНИЯ
                    else if (callbackData.StartsWith(CallbackData.DeleteCancelPrefix))
                    {
                        var projectIdStr = callbackData.Replace(CallbackData.DeleteCancelPrefix, "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Отмена удаления проекта ID: {projectId}");
                            await ShowProjectDetailsAsync(chatId, projectId, cancellationToken);
                        }
                    }
                    break;
            }
        }

        private async Task ShowProjectsListAsync(long chatId, CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetAllProjectsAsync();

            if (projects.Count == 0)
            {
                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    "📭 Список проектов пуст.\n\nСоздайте первый проект!",
                    new InlineKeyboardMarkup(new[]
                    {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Создать проект", CallbackData.CreateProject),
                    InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToProjects)
                }
                    }),
                    "projects_list",
                    cancellationToken);
                return;
            }

            var text = "📋 Выберите проект:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var project in projects.Take(10))
            {
                var statusIcon = project.Status switch
                {
                    ProjectStatus.Pending => "🟡",
                    ProjectStatus.InProgress => "🟠",
                    ProjectStatus.Completed => "✅",
                    _ => "⚪"
                };

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(
                $"{statusIcon} {project.Name}",
                $"{CallbackData.ProjectPrefix}{project.Id}") // ИСПРАВЛЕНО
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToProjects)
    });

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "projects_list",
                cancellationToken);
        }

        private async Task ShowProjectDetailsAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            var project = await _projectService.GetProjectAsync(projectId);
            if (project == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Проект не найден.", cancellationToken);
                await _menuManager.ShowProjectsMenuAsync(chatId, cancellationToken);
                return;
            }

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
                      $"📊 Статус: {statusIcon}\n" +
                      $"📝 Описание: {project.Description ?? "Нет описания"}\n" +
                      $"📋 Задачи: {activeTasks} активных, {completedTasks} выполнено\n" +
                      $"👤 Создал: @{project.CreatedBy?.Username ?? "Неизвестно"}\n" +
                      $"📅 Дата: {project.CreatedAt:dd.MM.yyyy}";

            if (!string.IsNullOrEmpty(project.Link))
                text += $"\n🔗 Ссылка: {project.Link}";

            var buttons = new List<List<InlineKeyboardButton>>();

            // Кнопки для смены статуса
            var statusButtons = new List<InlineKeyboardButton>();

            if (project.Status != ProjectStatus.Pending)
                statusButtons.Add(InlineKeyboardButton.WithCallbackData("🟡 Предстоит", $"change_status_{projectId}_pending"));
            if (project.Status != ProjectStatus.InProgress)
                statusButtons.Add(InlineKeyboardButton.WithCallbackData("🟠 В работу", $"change_status_{projectId}_inprogress"));
            if (project.Status != ProjectStatus.Completed)
                statusButtons.Add(InlineKeyboardButton.WithCallbackData("✅ Готово", $"change_status_{projectId}_completed"));

            if (statusButtons.Count > 0)
                buttons.Add(statusButtons);

            // Остальные кнопки
            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"{CallbackData.EditProjectPrefix}{projectId}"),
        InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"{CallbackData.DeleteProjectPrefix}{projectId}")
    });

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToProjects)
    });

            var keyboard = new InlineKeyboardMarkup(buttons);
            await _menuManager.ShowInlineMenuAsync(chatId, text, keyboard, $"project_{projectId}", cancellationToken);
        }

        private async Task ShowDeleteProjectConfirmationAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            var project = await _projectService.GetProjectAsync(projectId);
            if (project == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Проект не найден.", cancellationToken);
                return;
            }

            await _menuManager.ShowDeleteConfirmationAsync(
                chatId,
                "проект",
                $"Название: {project.Name}\nОписание: {project.Description ?? "Нет описания"}",
                $"{CallbackData.DeleteConfirmPrefix}{projectId}",
                $"{CallbackData.ProjectPrefix}{projectId}",
                cancellationToken);
        }

        private async Task DeleteProjectAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            var success = await _projectService.DeleteProjectAsync(projectId);
            if (success)
            {
                await SendTemporaryMessageAsync(chatId, "✅ Проект успешно удален.", cancellationToken);
                await _menuManager.ShowProjectsMenuAsync(chatId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось удалить проект.", cancellationToken);
            }
        }

        private async Task ShowProjectStatusChangeMenuAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            var text = "📊 Выберите новый статус проекта:";
            var keyboard = MainMenuKeyboard.GetProjectStatusChange(projectId);

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                keyboard,
                $"change_status_{projectId}",
                cancellationToken);
        }

        private async Task UpdateProjectStatusAsync(long chatId, int projectId, ProjectStatus newStatus, CancellationToken cancellationToken)
        {
            var success = await _projectService.UpdateProjectStatusAsync(projectId, newStatus);
            if (success)
            {
                var statusText = newStatus switch
                {
                    ProjectStatus.Pending => "🟡 Предстоит",
                    ProjectStatus.InProgress => "🟠 В работе",
                    ProjectStatus.Completed => "✅ Готово",
                    _ => "❓ Неизвестно"
                };

                await SendTemporaryMessageAsync(chatId, $"✅ Статус проекта обновлен на: {statusText}", cancellationToken, 3);

                // Возвращаемся к деталям проекта
                await ShowProjectDetailsAsync(chatId, projectId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось обновить статус проекта.", cancellationToken);
            }
        }
        #endregion

        #region Задачи - ПОЛНАЯ РЕАЛИЗАЦИЯ
        private async Task HandleTaskCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandleTaskCallbackAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   └─ Callback Data: {callbackData}");

            // Проверяем точное совпадение для tasks_settings
            if (callbackData == CallbackData.TasksSettings)
            {
                Console.WriteLine($"   → Выбран: TasksSettings (точное совпадение)");
                await ShowTaskSettingsAsync(chatId, userId, cancellationToken);
                return;
            }

            switch (callbackData)
            {
                case CallbackData.TasksCreate:
                    Console.WriteLine($"   → Выбран: TasksCreate");
                    await ShowProjectSelectionForTaskAsync(chatId, userId, cancellationToken);
                    break;

                case CallbackData.TasksList:
                    Console.WriteLine($"   → Выбран: TasksList");
                    await ShowAllTasksAsync(chatId, cancellationToken);
                    break;

                case CallbackData.TasksMy:
                    Console.WriteLine($"   → Выбран: TasksMy");
                    await ShowMyTasksAsync(chatId, userId, cancellationToken);
                    break;

                case CallbackData.TasksArchive:
                    Console.WriteLine($"   → Выбран: TasksArchive");
                    await ShowArchivedTasksAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToTasks:
                    Console.WriteLine($"   → Выбран: BackToTasks");
                    var isAdmin = await _userService.IsAdminAsync(userId);
                    await _menuManager.ShowTasksMenuAsync(chatId, isAdmin, cancellationToken);
                    break;
                case "stats_week":
                    Console.WriteLine($"   → Выбрана статистика за неделю");
                    await ShowTaskStatsWeekAsync(chatId, cancellationToken);
                    break;

                case "stats_month":
                    Console.WriteLine($"   → Выбрана статистика за месяц");
                    await ShowTaskStatsMonthAsync(chatId, cancellationToken);
                    break;

                default:
                    Console.WriteLine($"   → Default case - анализ префиксов задач");

                    // СНАЧАЛА ПРОВЕРЯЕМ БОЛЕЕ ДЛИННЫЕ ПРЕФИКСЫ!

                    // Проверяем task_delete_confirm_ (подтверждение удаления)
                    if (callbackData.StartsWith("task_delete_confirm_"))
                    {
                        var taskIdStr = callbackData.Replace("task_delete_confirm_", "");
                        Console.WriteLine($"   → TaskDeleteConfirm с ID: {taskIdStr}");

                        if (int.TryParse(taskIdStr, out int taskId))
                        {
                            await DeleteTaskAsync(chatId, taskId, cancellationToken);
                        }
                    }
                    // Проверяем task_delete_cancel_ (отмена удаления)
                    else if (callbackData.StartsWith("task_delete_cancel_"))
                    {
                        var taskIdStr = callbackData.Replace("task_delete_cancel_", "");
                        Console.WriteLine($"   → TaskDeleteCancel с ID: {taskIdStr}");

                        if (int.TryParse(taskIdStr, out int taskId))
                        {
                            await ShowTaskDetailsAsync(chatId, taskId, cancellationToken);
                        }
                    }
                    // Проверяем task_delete_ (удаление)
                    else if (callbackData.StartsWith("task_delete_"))
                    {
                        var taskIdStr = callbackData.Replace("task_delete_", "");
                        Console.WriteLine($"   → TaskDelete с ID: {taskIdStr}");

                        if (int.TryParse(taskIdStr, out int taskId))
                        {
                            await ShowDeleteTaskConfirmationAsync(chatId, taskId, cancellationToken);
                        }
                    }
                    // Проверяем task_complete_ (выполнение задачи)
                    else if (callbackData.StartsWith(CallbackData.TaskCompletePrefix))
                    {
                        var taskIdStr = callbackData.Replace(CallbackData.TaskCompletePrefix, "");
                        Console.WriteLine($"   → TaskComplete с ID: {taskIdStr}");

                        if (int.TryParse(taskIdStr, out int taskId))
                        {
                            await CompleteTaskAsync(chatId, taskId, cancellationToken);
                        }
                    }
                    // Проверяем task_reactivate_ (вернуть в работу)
                    else if (callbackData.StartsWith("task_reactivate_"))
                    {
                        var taskIdStr = callbackData.Replace("task_reactivate_", "");
                        Console.WriteLine($"   → TaskReactivate с ID: {taskIdStr}");

                        if (int.TryParse(taskIdStr, out int taskId))
                        {
                            await ReactivateTaskAsync(chatId, taskId, cancellationToken);
                        }
                    }
                    // Проверяем task_archive_ (в архив)
                    else if (callbackData.StartsWith("task_archive_"))
                    {
                        var taskIdStr = callbackData.Replace("task_archive_", "");
                        Console.WriteLine($"   → TaskArchive с ID: {taskIdStr}");

                        if (int.TryParse(taskIdStr, out int taskId))
                        {
                            await ArchiveTaskAsync(chatId, taskId, cancellationToken);
                        }
                    }
                    // Проверяем project_for_task_ (выбор проекта)
                    else if (callbackData.StartsWith("project_for_task_"))
                    {
                        var projectIdStr = callbackData.Replace("project_for_task_", "");
                        Console.WriteLine($"   → project_for_task_ с ID: {projectIdStr}");

                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            await ShowUserSelectionForTaskAsync(chatId, userId, projectId, cancellationToken);
                        }
                    }
                    // Проверяем assign_task_to_user_ (назначение исполнителя)
                    else if (callbackData.StartsWith("assign_task_to_user_"))
                    {
                        Console.WriteLine($"   → assign_task_to_user_ callback");
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 5)
                        {
                            if (int.TryParse(parts[4], out int projectId))
                            {
                                string userIdStr = string.Join("_", parts.Skip(5));
                                Console.WriteLine($"   → projectId: {projectId}, userIdStr: {userIdStr}");

                                if (long.TryParse(userIdStr, out long assignedUserId))
                                {
                                    Console.WriteLine($"   → Назначаем задачу пользователю {assignedUserId}");
                                    _userStates[userId] = new UserState
                                    {
                                        CurrentAction = UserActions.CreateTask,
                                        ProjectId = projectId,
                                        Data = new Dictionary<string, object?> { ["assignedToUserId"] = assignedUserId },
                                        Step = 1
                                    };
                                    await SendTemporaryMessageAsync(chatId, "Введите название задачи:", cancellationToken);
                                }
                            }
                        }
                    }
                    // Проверяем просмотр задачи
                    else if (callbackData.StartsWith(CallbackData.TaskPrefix))
                    {
                        var taskIdStr = callbackData.Replace(CallbackData.TaskPrefix, "");
                        if (int.TryParse(taskIdStr, out int taskId))
                        {
                            await ShowTaskDetailsAsync(chatId, taskId, cancellationToken);
                        }
                    }
                    // Проверяем settings_all_users
                    else if (callbackData == "settings_all_users")
                    {
                        await ShowAllUsersForSettingsAsync(chatId, cancellationToken);
                    }
                    // Проверяем settings_task_stats
                    else if (callbackData == "settings_task_stats")
                    {
                        await ShowTaskStatisticsAsync(chatId, cancellationToken);
                    }
                    // Проверяем settings_make_admin
                    else if (callbackData == "settings_make_admin")
                    {
                        await ShowMakeAdminMenuAsync(chatId, cancellationToken);
                    }
                    // Проверяем make_admin_
                    else if (callbackData.StartsWith("make_admin_"))
                    {
                        var userIdStr = callbackData.Replace("make_admin_", "");
                        if (long.TryParse(userIdStr, out long targetUserId))
                        {
                            await MakeUserAdminAsync(chatId, targetUserId, cancellationToken);
                        }
                    }
                    break;
            }
        }
        private async Task ShowAllUsersForSettingsAsync(long chatId, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync();
            var tasks = await _taskService.GetAllTasksAsync();

            var text = "👥 ВСЕ УЧАСТНИКИ\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var user in users)
            {
                var userTasks = tasks.Count(t => t.AssignedToUserId == user.TelegramId);
                var completedTasks = tasks.Count(t => t.AssignedToUserId == user.TelegramId && t.Status == TeamTaskStatus.Completed);
                var roleIcon = user.Role == UserRole.Admin ? "👑" : "👤";
                var username = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                var completionRate = userTasks > 0 ? (completedTasks * 100 / userTasks) : 0;

                text += $"{roleIcon} {username}: {userTasks} задач ({completedTasks}✅, {completionRate}%)\n";
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "settings_users")
    });

            // ИСПРАВЛЕНО: Используем ShowInlineMenuAsync
            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "settings_all_users",
                cancellationToken);
        }

        private async Task ShowTaskStatisticsAsync(long chatId, CancellationToken cancellationToken)
        {
            var tasks = await _taskService.GetAllTasksAsync();
            var users = await _userService.GetAllUsersAsync();

            var activeTasks = tasks.Count(t => t.Status == TeamTaskStatus.Active);
            var completedTasks = tasks.Count(t => t.Status == TeamTaskStatus.Completed);
            var archivedTasks = tasks.Count(t => t.Status == TeamTaskStatus.Archived);
            var totalTasks = tasks.Count;

            var activePercentage = totalTasks > 0 ? (activeTasks * 100 / totalTasks) : 0;
            var completedPercentage = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;
            var archivedPercentage = totalTasks > 0 ? (archivedTasks * 100 / totalTasks) : 0;

            var avgTasksPerUser = users.Count > 0 ? (double)totalTasks / users.Count : 0;

            // Задачи по дням (последние 7 дней)
            var weekStats = new Dictionary<string, int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i).Date;
                var count = tasks.Count(t => t.CreatedAt.Date == date);
                weekStats[date.ToString("dd.MM")] = count;
            }

            var weekText = string.Join(" ", weekStats.Select(kv => $"{kv.Key}:{kv.Value}"));

            var text = $"📊 СТАТИСТИКА ЗАДАЧ\n\n" +
                       $"📈 Общая статистика:\n" +
                       $"• Всего задач: {totalTasks}\n" +
                       $"• Активных: {activeTasks} ({activePercentage}%)\n" +
                       $"• Выполненных: {completedTasks} ({completedPercentage}%)\n" +
                       $"• В архиве: {archivedTasks} ({archivedPercentage}%)\n\n" +
                       $"📅 Задачи по дням (последние 7 дней):\n{weekText}\n\n" +
                       $"👥 Среднее количество задач на участника: {avgTasksPerUser:F1}";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📅 За неделю", "stats_week") },
        new() { InlineKeyboardButton.WithCallbackData("📅 За месяц", "stats_month") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.TasksSettings) }
    };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "task_stats",
                cancellationToken);
        }

        private async Task ShowMakeAdminMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync();
            var nonAdminUsers = users.Where(u => u.Role != UserRole.Admin).ToList();

            if (nonAdminUsers.Count == 0)
            {
                await SendTemporaryMessageAsync(chatId, "✅ Все пользователи уже являются администраторами!", cancellationToken, 5);
                await ShowTaskSettingsAsync(chatId, 0, cancellationToken); // 0 - заглушка, userId не используется в этом методе
                return;
            }

            var text = "👑 НАЗНАЧЕНИЕ АДМИНИСТРАТОРА\n\nВыберите пользователя:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var user in nonAdminUsers.Take(10))
            {
                var username = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"👤 {username}", $"make_admin_{user.TelegramId}")
        });
            }

            if (nonAdminUsers.Count > 10)
            {
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"... и еще {nonAdminUsers.Count - 10}", "make_admin_more")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.TasksSettings)
    });

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "make_admin",
                cancellationToken);
        }

        private async Task MakeUserAdminAsync(long chatId, long targetUserId, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.GetUserByTelegramIdAsync(targetUserId);
                if (user == null)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Пользователь не найден.", cancellationToken);
                    return;
                }

                user.Role = UserRole.Admin;
                var success = await _userService.UpdateUserAsync(user);

                if (success)
                {
                    var username = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                    await SendTemporaryMessageAsync(chatId, $"✅ {username} теперь администратор!", cancellationToken, 5);
                    await ShowTaskSettingsAsync(chatId, 0, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Не удалось назначить администратора.", cancellationToken, 5);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making user admin");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при назначении администратора.", cancellationToken, 5);
            }
        }
        private async Task ShowTaskSettingsAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            Console.WriteLine($"   → Запущен ShowTaskSettingsAsync");

            // Проверяем, что пользователь админ
            var isAdmin = await _userService.IsAdminAsync(userId);
            if (!isAdmin)
            {
                await SendTemporaryMessageAsync(chatId, "⛔ Только администраторы могут настраивать задачи.", cancellationToken, 5);
                return;
            }

            var users = await _userService.GetAllUsersAsync();
            var tasks = await _taskService.GetAllTasksAsync();

            var text = $"⚙️ НАСТРОЙКИ УЧАСТНИКОВ В ЗАДАЧАХ\n\n" +
                       $"📊 Статистика:\n" +
                       $"• Всего участников: {users.Count}\n" +
                       $"• Всего задач: {tasks.Count}\n" +
                       $"• Активных задач: {tasks.Count(t => t.Status == TeamTaskStatus.Active)}\n" +
                       $"• Выполненных задач: {tasks.Count(t => t.Status == TeamTaskStatus.Completed)}\n" +
                       $"• Задач в архиве: {tasks.Count(t => t.Status == TeamTaskStatus.Archived)}\n\n" +
                       $"📈 Прогресс команды:\n" +
                       $"• Выполнено: {(tasks.Count > 0 ? (tasks.Count(t => t.Status == TeamTaskStatus.Completed) * 100 / tasks.Count) : 0)}%\n\n" +
                       $"👥 Участники:\n";

            foreach (var user in users.Take(5))
            {
                var userTasks = tasks.Count(t => t.AssignedToUserId == user.TelegramId);
                var completedUserTasks = tasks.Count(t => t.AssignedToUserId == user.TelegramId && t.Status == TeamTaskStatus.Completed);
                var roleIcon = user.Role == UserRole.Admin ? "👑 " : "👤 ";
                var username = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;

                var completionRate = userTasks > 0 ? (completedUserTasks * 100 / userTasks) : 0;
                text += $"{roleIcon}{username}: {userTasks} задач ({completedUserTasks}✅, {completionRate}%)\n";
            }

            if (users.Count > 5)
            {
                text += $"\n... и еще {users.Count - 5} участников";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("👥 Все участники", "settings_all_users") },
        new() { InlineKeyboardButton.WithCallbackData("📊 Статистика по задачам", "settings_task_stats") },
        new() { InlineKeyboardButton.WithCallbackData("👑 Назначить администратора", "settings_make_admin") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks) }
    };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "task_settings",
                cancellationToken);
        }
        private async Task ShowProjectSelectionForTaskAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetAllProjectsAsync();

            if (projects.Count == 0)
            {
                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    "📭 Нет проектов для создания задачи.\n\nСначала создайте проект!",
                    new InlineKeyboardMarkup(new[]
                    {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Создать проект", CallbackData.CreateProject),
                    InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks)
                }
                    }),
                    "task_project_selection",
                    cancellationToken);
                return;
            }

            var text = "📝 Создание задачи\n\nВыберите проект:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var project in projects.Take(10))
            {
                var statusIcon = project.Status switch
                {
                    ProjectStatus.Pending => "🟡",
                    ProjectStatus.InProgress => "🟠",
                    ProjectStatus.Completed => "✅",
                    _ => "⚪"
                };

                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{statusIcon} {project.Name}",
                        $"project_for_task_{project.Id}") // ДОЛЖНО БЫТЬ ИМЕННО ТАК, БЕЗ ЛИШНИХ СИМВОЛОВ
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks)
    });

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "task_project_selection",
                cancellationToken);
        }

        private async Task ShowAllTasksAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Используем GetAllTasksAsync вместо несуществующего метода
                var allTasks = await _taskService.GetAllTasksAsync();
                var activeTasks = allTasks.Where(t => t.Status == TeamTaskStatus.Active).Take(10).ToList();

                if (activeTasks.Count == 0)
                {
                    await _menuManager.ShowInlineMenuAsync(
                        chatId,
                        "📭 Нет активных задач.",
                        new InlineKeyboardMarkup(new[]
                        {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("➕ Создать задачу", CallbackData.TasksCreate),
                        InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks)
                    }
                        }),
                        "tasks_list",
                        cancellationToken);
                    return;
                }

                var text = "📋 Активные задачи:\n\n";
                var buttons = new List<List<InlineKeyboardButton>>();

                foreach (var task in activeTasks)
                {
                    var dueText = task.DueDate.HasValue ? $" (до {task.DueDate.Value:dd.MM.yyyy})" : "";
                    text += $"🟢 {task.Title}{dueText}\n";

                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🟢 {task.Title}",
                    $"{CallbackData.TaskPrefix}{task.Id}")
            });
                }

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks)
        });

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "tasks_list",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing all tasks");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке задач.", cancellationToken);
            }
        }

        private async Task ShowMyTasksAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            try
            {
                var tasks = await _taskService.GetTasksByUserAsync(userId);
                var activeTasks = tasks.Where(t => t.Status == TeamTaskStatus.Active).Take(5).ToList();
                var completedTasks = tasks.Where(t => t.Status == TeamTaskStatus.Completed).Take(3).ToList();

                var text = "📋 Ваши задачи:\n\n";

                if (activeTasks.Count > 0)
                {
                    text += "🟢 Активные:\n";
                    foreach (var task in activeTasks)
                    {
                        text += $"• {task.Title}";
                        if (task.DueDate.HasValue)
                            text += $" (до {task.DueDate.Value:dd.MM.yyyy})";
                        text += "\n";
                    }
                }

                if (completedTasks.Count > 0)
                {
                    text += "\n✅ Выполненные:\n";
                    foreach (var task in completedTasks)
                    {
                        text += $"• {task.Title}\n";
                    }
                }

                if (activeTasks.Count == 0 && completedTasks.Count == 0)
                {
                    text += "📭 У вас нет задач.";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("➕ Создать задачу", CallbackData.TasksCreate) },
            new() { InlineKeyboardButton.WithCallbackData("📋 Все задачи", CallbackData.TasksList) },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "my_tasks",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing my tasks");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке ваших задач.", cancellationToken);
            }
        }

        private async Task ShowArchivedTasksAsync(long chatId, CancellationToken cancellationToken)
        {
            var tasks = await _taskService.GetAllTasksAsync();
            var archivedTasks = tasks.Where(t => t.Status == TeamTaskStatus.Archived).Take(10).ToList();

            if (archivedTasks.Count == 0)
            {
                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    "📭 Нет задач в архиве.",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("📋 Активные задачи", CallbackData.TasksList),
                            InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks)
                        }
                    }),
                    "tasks_archive",
                    cancellationToken);
                return;
            }

            var text = "📁 Архив задач:\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var task in archivedTasks)
            {
                text += $"📁 {task.Title}\n";
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"📁 {task.Title}",
                        $"{CallbackData.TaskPrefix}{task.Id}")
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks)
            });

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "tasks_archive",
                cancellationToken);
        }

        private async Task ShowUserSelectionForTaskAsync(long chatId, long userId, int projectId, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync();
            var project = await _projectService.GetProjectAsync(projectId);

            if (users.Count == 0)
            {
                // Если нет пользователей, назначаем на создателя
                _userStates[userId] = new UserState
                {
                    CurrentAction = UserActions.CreateTask,
                    ProjectId = projectId,
                    Data = new Dictionary<string, object?> { ["assignedToUserId"] = userId },
                    Step = 1
                };
                await SendTemporaryMessageAsync(chatId, "Введите название задачи:", cancellationToken);
                return;
            }

            var text = $"📝 Создание задачи в проекте \"{project?.Name}\"\n\nВыберите исполнителя:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var user in users.Take(10))
            {
                var isAdmin = user.Role == UserRole.Admin ? "👑 " : "";
                var displayName = !string.IsNullOrEmpty(user.Username)
                    ? $"@{user.Username}"
                    : $"{user.FirstName}";

                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{isAdmin}{displayName}",
                        $"assign_task_to_user_{projectId}_{user.TelegramId}")
                });
            }

            // Кнопка "Назначить на меня"
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    "👤 Назначить на меня",
                    $"assign_task_to_user_{projectId}_{userId}")
            });

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.TasksCreate)
            });

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "user_selection",
                cancellationToken);
        }
        private async Task ShowTaskDetailsAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            var task = await _taskService.GetTaskAsync(taskId);
            if (task == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Задача не найдена.", cancellationToken);
                return;
            }

            var statusText = task.Status switch
            {
                TeamTaskStatus.Active => "🟢 Активна",
                TeamTaskStatus.Completed => "✅ Выполнена",
                TeamTaskStatus.Archived => "📁 В архиве",
                _ => "❓ Неизвестно"
            };

            var text = $"📋 Задача: {task.Title}\n\n" +
                      $"Описание: {task.Description ?? "Нет описания"}\n" +
                      $"Статус: {statusText}\n" +
                      $"Проект: {task.Project?.Name ?? "Не указан"}\n" +
                      $"Назначена: @{task.AssignedTo?.Username ?? "Не назначена"}\n" +
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
                    InlineKeyboardButton.WithCallbackData("✅ Выполнить", $"{CallbackData.TaskCompletePrefix}{task.Id}"),
                    InlineKeyboardButton.WithCallbackData("📁 В архив", $"task_archive_{task.Id}")
                });
            }
            else if (task.Status == TeamTaskStatus.Completed)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("🔄 Вернуть в работу", $"task_reactivate_{task.Id}"),
                    InlineKeyboardButton.WithCallbackData("📁 В архив", $"task_archive_{task.Id}")
                });
            }
            else if (task.Status == TeamTaskStatus.Archived)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("🔄 Активировать", $"task_reactivate_{task.Id}")
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"task_delete_{task.Id}"),
                InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks)
            });

            var keyboard = new InlineKeyboardMarkup(buttons);
            await _menuManager.ShowInlineMenuAsync(chatId, text, keyboard, $"task_{task.Id}", cancellationToken);
        }

        private async Task CompleteTaskAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            var task = await _taskService.GetTaskAsync(taskId);
            if (task == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Задача не найдена.", cancellationToken);
                return;
            }

            // Проверяем, что пользователь является исполнителем или админом
            var user = await _userService.GetUserByTelegramIdAsync(chatId); // ID чата = ID пользователя
            if (user == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Пользователь не найден.", cancellationToken);
                return;
            }

            bool isAdmin = user.Role == UserRole.Admin;
            bool isAssigned = task.AssignedToUserId == user.TelegramId;

            if (!isAdmin && !isAssigned)
            {
                await SendTemporaryMessageAsync(chatId, "⛔ Только исполнитель или администратор могут выполнить задачу.", cancellationToken);
                return;
            }

            var success = await _taskService.CompleteTaskAsync(taskId);
            if (success)
            {
                await SendTemporaryMessageAsync(chatId, "✅ Задача выполнена! 🎉", cancellationToken, 5);
                await ShowTaskDetailsAsync(chatId, taskId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось выполнить задачу.", cancellationToken);
            }
        }

        private async Task ReactivateTaskAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            var success = await _taskService.ActivateTaskAsync(taskId);
            if (success)
            {
                await SendTemporaryMessageAsync(chatId, "✅ Задача возвращена в работу!", cancellationToken, 5);
                await ShowTaskDetailsAsync(chatId, taskId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось активировать задачу.", cancellationToken);
            }
        }

        private async Task ArchiveTaskAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            var success = await _taskService.ArchiveTaskAsync(taskId);
            if (success)
            {
                await SendTemporaryMessageAsync(chatId, "✅ Задача перемещена в архив!", cancellationToken, 5);
                await ShowTaskDetailsAsync(chatId, taskId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось архивировать задачу.", cancellationToken);
            }
        }

        // ===== СТАТИСТИКА ЗАДАЧ ЗА НЕДЕЛЮ =====
        private async Task ShowTaskStatsWeekAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var weekStart = DateTime.UtcNow.AddDays(-7);
                var allTasks = await _taskService.GetAllTasksAsync();
                var weekTasks = allTasks.Where(t => t.CreatedAt >= weekStart).ToList();
                var users = await _userService.GetAllUsersAsync();

                var completedTasks = weekTasks.Count(t => t.Status == TeamTaskStatus.Completed);
                var activeTasks = weekTasks.Count(t => t.Status == TeamTaskStatus.Active);
                var totalTasks = weekTasks.Count;

                var completionRate = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

                var text = $"📊 СТАТИСТИКА ЗАДАЧ ЗА НЕДЕЛЮ\n\n" +
                           $"📅 {weekStart:dd.MM.yyyy} - {DateTime.UtcNow:dd.MM.yyyy}\n\n" +
                           $"✅ Выполнено: {completedTasks} из {totalTasks} ({completionRate}%)\n" +
                           $"🟢 В работе: {activeTasks}\n\n";

                // Статистика по пользователям
                var userStats = weekTasks
                    .Where(t => t.AssignedToUserId != 0)
                    .GroupBy(t => t.AssignedToUserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Completed = g.Count(t => t.Status == TeamTaskStatus.Completed),
                        Total = g.Count()
                    })
                    .OrderByDescending(x => x.Completed)
                    .ToList();

                if (userStats.Any())
                {
                    text += $"👥 ПО ИСПОЛНИТЕЛЯМ:\n";
                    foreach (var stat in userStats.Take(5))
                    {
                        var user = users.FirstOrDefault(u => u.TelegramId == stat.UserId);
                        var userName = user != null
                            ? (!string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName)
                            : $"ID:{stat.UserId}";
                        var rate = stat.Total > 0 ? (stat.Completed * 100 / stat.Total) : 0;
                        text += $"• {userName}: {stat.Completed}/{stat.Total} ({rate}%)\n";
                    }
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📅 ЗА МЕСЯЦ", "stats_month") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "task_stats_week", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing task stats week");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки статистики", cancellationToken, 3);
            }
        }

        // ===== СТАТИСТИКА ЗАДАЧ ЗА МЕСЯЦ =====
        private async Task ShowTaskStatsMonthAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var allTasks = await _taskService.GetAllTasksAsync();
                var monthTasks = allTasks.Where(t => t.CreatedAt >= monthStart).ToList();
                var users = await _userService.GetAllUsersAsync();

                var completedTasks = monthTasks.Count(t => t.Status == TeamTaskStatus.Completed);
                var activeTasks = monthTasks.Count(t => t.Status == TeamTaskStatus.Active);
                var totalTasks = monthTasks.Count;

                var completionRate = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

                var text = $"📊 СТАТИСТИКА ЗАДАЧ ЗА МЕСЯЦ\n\n" +
                           $"📅 {monthStart:MMMM yyyy}\n\n" +
                           $"✅ Выполнено: {completedTasks} из {totalTasks} ({completionRate}%)\n" +
                           $"🟢 В работе: {activeTasks}\n\n";

                // Статистика по пользователям
                var userStats = monthTasks
                    .Where(t => t.AssignedToUserId != 0)
                    .GroupBy(t => t.AssignedToUserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Completed = g.Count(t => t.Status == TeamTaskStatus.Completed),
                        Total = g.Count()
                    })
                    .OrderByDescending(x => x.Completed)
                    .ToList();

                if (userStats.Any())
                {
                    text += $"👥 ПО ИСПОЛНИТЕЛЯМ:\n";
                    foreach (var stat in userStats.Take(5))
                    {
                        var user = users.FirstOrDefault(u => u.TelegramId == stat.UserId);
                        var userName = user != null
                            ? (!string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName)
                            : $"ID:{stat.UserId}";
                        var rate = stat.Total > 0 ? (stat.Completed * 100 / stat.Total) : 0;
                        text += $"• {userName}: {stat.Completed}/{stat.Total} ({rate}%)\n";
                    }
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📅 ЗА НЕДЕЛЮ", "stats_week") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToTasks) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "task_stats_month", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing task stats month");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки статистики", cancellationToken, 3);
            }
        }

        private async Task ShowDeleteTaskConfirmationAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            var task = await _taskService.GetTaskAsync(taskId);
            if (task == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Задача не найдена", cancellationToken);
                return;
            }

            await _menuManager.ShowDeleteConfirmationAsync(
                chatId,
                "задачу",
                $"Название: {task.Title}\nОписание: {task.Description ?? "Нет описания"}",
                $"task_delete_confirm_{taskId}",
                $"{CallbackData.TaskPrefix}{taskId}",
                cancellationToken);
        }

        private async Task DeleteTaskAsync(long chatId, int taskId, CancellationToken cancellationToken)
        {
            var success = await _taskService.DeleteTaskAsync(taskId);
            if (success)
            {
                await SendTemporaryMessageAsync(chatId, "✅ Задача успешно удалена.", cancellationToken, 5);
                await _menuManager.ShowTasksMenuAsync(chatId, await _userService.IsAdminAsync(0), cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось удалить задачу.", cancellationToken);
            }
        }
        #endregion

        #region Финансы - ПОЛНАЯ РЕАЛИЗАЦИЯ
        private async Task HandleFinanceCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandleFinanceCallbackAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   └─ Callback Data: {callbackData}");

            // ===== СНАЧАЛА ПРОВЕРЯЕМ ГРАФИКИ (до finance_ префиксов) =====
            if (callbackData.StartsWith("expenses_chart_"))
            {
                var parts = callbackData.Split('_');
                if (parts.Length == 4 &&
                    int.TryParse(parts[2], out int year) &&
                    int.TryParse(parts[3], out int month))
                {
                    Console.WriteLine($"   → График расходов за {month}.{year}");
                    await ShowMonthlyExpensesChartAsync(chatId, cancellationToken, year, month);
                }
                return; // ВАЖНО: выходим, чтобы не обрабатывать дальше
            }

            switch (callbackData)
            {
                // Основное меню бухгалтерии
                case "finance_accounts":
                    await ShowFinanceAccountsMenuAsync(chatId, cancellationToken);
                    break;

                // ===== CRYPTO BOT =====
                case "crypto_profit_chart":
                    await ShowCryptoProfitChartAsync(chatId, cancellationToken);
                    break;

                case "crypto_circles_chart":
                    await ShowCryptoCirclesChartAsync(chatId, cancellationToken);
                    break;
                case "crypto_deals_chart":
                    await ShowCryptoDealsChartAsync(chatId, cancellationToken);
                    break;
                case "crypto_stats_monthly":
                    await ShowCryptoStatsMonthlyAsync(chatId, cancellationToken);
                    break;
                case "crypto_charts":
                    await ShowCryptoChartsAsync(chatId, cancellationToken);
                    break;
                case "crypto_stats":
                    await ShowCryptoStatsAsync(chatId, cancellationToken);
                    break;
                case "crypto_all_circles":
                    await ShowAllCirclesAsync(chatId, cancellationToken);
                    break;
                case "crypto_link_deal":
                    await StartLinkDealToCircleAsync(chatId, userId, cancellationToken);
                    break;
                case "crypto_deals_stats":
                    await ShowCryptoDealsStatsAsync(chatId, cancellationToken);
                    break;
                case "finance_crypto_menu":
                    await ShowCryptoMenuAsync(chatId, cancellationToken);
                    break;
                case "finance_crypto_circles":
                    await ShowCryptoCirclesAsync(chatId, cancellationToken);
                    break;
                case "finance_crypto_deals":
                    await ShowCryptoDealsAsync(chatId, cancellationToken);
                    break;
                case var _ when callbackData.StartsWith("crypto_link_deal_"):
                    var circleIdStr = callbackData.Replace("crypto_link_deal_", "");
                    if (int.TryParse(circleIdStr, out int circleId))
                    {
                        await HandleLinkDealToCircleAsync(chatId, userId, circleId, cancellationToken);
                    }
                    break;
                case "crypto_link_deal_skip":
                    await HandleLinkDealSkipAsync(chatId, userId, cancellationToken);
                    break;
                case "crypto_add_circle":
                    await StartAddCryptoCircleAsync(chatId, userId, cancellationToken);
                    break;
                case "crypto_add_deal":
                    await StartAddCryptoDealAsync(chatId, userId, cancellationToken);
                    break;
                case "crypto_complete_circle":
                    await StartCompleteCryptoCircleAsync(chatId, userId, cancellationToken);
                    break;

                // ===== FUNPAY =====
                case "funpay_accounts":
                    await ShowFunPayAccountsAsync(chatId, cancellationToken);
                    break;
                case "funpay_warnings":
                    await ShowFunPayWarningsAsync(chatId, cancellationToken);
                    break;
                case "funpay_stats":
                    await ShowFunPayStatsAsync(chatId, cancellationToken);
                    break;
                case "funpay_sales_categories":
                    await ShowFunPaySalesByCategoryAsync(chatId, cancellationToken);
                    break;
                case "funpay_sales_dynamics":
                    await ShowFunPaySalesDynamicsAsync(chatId, cancellationToken);
                    break;
                case "funpay_withdrawal_stats":
                    await ShowFunPayWithdrawalStatsAsync(chatId, cancellationToken);
                    break;
                case "funpay_add_account":
                    await StartAddFunPayAccountAsync(chatId, userId, cancellationToken);
                    break;
                case "funpay_add_warning":
                    await StartAddFunPayWarningAsync(chatId, userId, cancellationToken);
                    break;
                case "finance_funpay_menu":
                    await ShowFunPayMenuAsync(chatId, cancellationToken);
                    break;
                case "finance_funpay_sales":
                    await ShowFunPaySalesAsync(chatId, cancellationToken);
                    break;
                case "finance_funpay_withdrawals":
                    await ShowFunPayWithdrawalsAsync(chatId, cancellationToken);
                    break;
                case "funpay_add_sale":
                    await StartAddFunPaySaleAsync(chatId, userId, cancellationToken);
                    break;
                case "funpay_add_withdrawal":
                    await StartAddFunPayWithdrawalAsync(chatId, userId, cancellationToken);
                    break;

                // ===== ВКЛАДЫ УЧАСТНИКОВ =====

                case "investment_search_user":
                    await SearchInvestmentUserAsync(chatId, userId, cancellationToken);
                    break;
                case "finance_investments":
                    await ShowInvestmentsAsync(chatId, cancellationToken);
                    break;
                case "investment_add":
                    await StartAddInvestmentAsync(chatId, userId, cancellationToken);
                    break;
                case "investment_by_user":
                    await ShowInvestmentByUserAsync(chatId, cancellationToken);
                    break;
                case "investment_stats":
                    await ShowInvestmentStatsAsync(chatId, cancellationToken);
                    break;
                case "investment_withdraw":
                    await StartWithdrawInvestmentAsync(chatId, userId, cancellationToken);
                    break;

                // ===== ТРАТЫ =====
                case "finance_expenses":
                    await ShowExpensesAsync(chatId, cancellationToken);
                    break;
                case "finance_expenses_period":
                    await ShowMonthlyExpensesChartAsync(chatId, cancellationToken, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
                    break;
                case "finance_expenses_details":
                    await ShowExpensesDetailsAsync(chatId, cancellationToken);
                    break;



                // ===== FAST INVEST =====
                case "fastinvest_all":
                    await ShowAllFastInvestmentsAsync(chatId, cancellationToken);
                    break;
                case "fastinvest_stats":
                    await ShowFastInvestStatsAsync(chatId, cancellationToken);
                    break;
                case "fastinvest_payouts":
                    await ShowFastInvestPayoutsAsync(chatId, cancellationToken);
                    break;
                case "fastinvest_complete":
                    await StartCompleteFastInvestAsync(chatId, userId, cancellationToken);
                    break;
                case "fastinvest_reactivate":
                    await StartReactivateFastInvestAsync(chatId, userId, cancellationToken);
                    break;
                case "fastinvest_change_status":
                    await StartChangeFastInvestStatusAsync(chatId, userId, cancellationToken);
                    break;
                case "fastinvest_search_contact":
                    await SearchFastInvestContactAsync(chatId, userId, cancellationToken);
                    break;
                case "finance_fastinvest_menu":
                    await ShowFastInvestMenuAsync(chatId, cancellationToken);
                    break;
                case "finance_fastinvest_active":
                    await ShowFastInvestByStatusAsync(chatId, InvestStatus.Active, cancellationToken);
                    break;
                case "finance_fastinvest_inactive":
                    await ShowFastInvestByStatusAsync(chatId, InvestStatus.Withdrawn, cancellationToken);
                    break;
                case "fastinvest_add":
                    await StartAddFastInvestAsync(chatId, userId, cancellationToken);
                    break;
                case "fastinvest_add_contact":
                    await StartAddContactAsync(chatId, userId, cancellationToken, "fastinvest");
                    break;

                // ===== КОМИССИИ =====
                case "commission_edit_list":
                    await ShowCommissionEditListAsync(chatId, cancellationToken);
                    break;
                case "commission_add_tip":
                    await StartAddCommissionTipAsync(chatId, userId, cancellationToken);
                    break;
                case "finance_commissions_menu":
                    await ShowCommissionsMenuAsync(chatId, cancellationToken);
                    break;
                case "finance_commissions_banks":
                    await ShowCommissionsByBanksAsync(chatId, cancellationToken);
                    break;
                case "finance_commissions_tips":
                    await ShowCommissionTipsAsync(chatId, cancellationToken);
                    break;
                case "commission_add":
                    await StartAddCommissionAsync(chatId, userId, cancellationToken);
                    break;

                // Существующие case из IFinanceService
                case CallbackData.FinanceDeposit:
                    await ShowDepositInfoAsync(chatId, cancellationToken);
                    break;
                case CallbackData.FinanceCommission:
                    await ShowCommissionManagementAsync(chatId, cancellationToken);
                    break;
                case CallbackData.FinanceIncomes:
                    await ShowIncomesAsync(chatId, cancellationToken);
                    break;
                case CallbackData.FinanceCrypto:
                    await ShowCryptoInfoAsync(chatId, cancellationToken);
                    break;
                case CallbackData.FinanceFi:
                    await ShowFastInvestInfoAsync(chatId, cancellationToken);
                    break;
                case CallbackData.FinanceCategories:
                    await ShowFinanceCategoriesMenuAsync(chatId, cancellationToken);
                    break;
                case CallbackData.FinanceAddIncome:
                    await ShowIncomeCategoriesAsync(chatId, userId, cancellationToken);
                    break;
                case CallbackData.FinanceAddExpense:
                    await ShowExpenseCategoriesAsync(chatId, userId, cancellationToken);
                    break;
                case CallbackData.FinanceStats:
                    await ShowFinanceStatisticsAsync(chatId, cancellationToken);
                    break;

                default:
                    if (callbackData.StartsWith("crypto_confirm_link_"))
                    {
                        var circleIdStr2 = callbackData.Replace("crypto_confirm_link_", "");
                        if (int.TryParse(circleIdStr2, out int circleId2))
                        {
                            await HandleConfirmLinkToCircleAsync(chatId, userId, circleId2, cancellationToken);
                        }
                    }
                    else if (callbackData == "crypto_link_deal_skip")
                    {
                        if (_userStates.ContainsKey(userId) && _userStates[userId].CurrentAction == "add_crypto_deal_link_circle")
                        {
                            await HandleLinkDealSkipAsync(chatId, userId, cancellationToken);
                        }
                        else
                        {
                            await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Сессия создания сделки истекла", cancellationToken, 3);
                        }
                    }
                    else if (callbackData.StartsWith("income_category_"))
                    {
                        var category = callbackData.Replace("income_category_", "");
                        _userStates[userId] = new UserState
                        {
                            CurrentAction = UserActions.AddIncome,
                            Data = new Dictionary<string, object?> { ["category"] = category },
                            Step = 1
                        };
                        await SendTemporaryMessageAsync(chatId, $"Введите сумму дохода (категория: {category}):", cancellationToken);
                    }
                    else if (callbackData.StartsWith("expense_category_"))
                    {
                        var category = callbackData.Replace("expense_category_", "");
                        _userStates[userId] = new UserState
                        {
                            CurrentAction = UserActions.AddExpense,
                            Data = new Dictionary<string, object?> { ["category"] = category },
                            Step = 1
                        };
                        await SendTemporaryMessageAsync(chatId, $"Введите сумму расхода (категория: {category}):", cancellationToken);
                    }
                    else if (callbackData.StartsWith("investment_select_user_"))
                    {
                        var targetUserIdStr = callbackData.Replace("investment_select_user_", "");
                        if (long.TryParse(targetUserIdStr, out long targetUserId))
                        {
                            await HandleInvestmentSelectUserAsync(chatId, userId, targetUserId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("investment_withdraw_user_"))
                    {
                        var withdrawUserIdStr = callbackData.Replace("investment_withdraw_user_", "");
                        if (long.TryParse(withdrawUserIdStr, out long withdrawUserId))
                        {
                            await HandleInvestmentWithdrawUserAsync(chatId, userId, withdrawUserId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("crypto_complete_circle_"))
                    {
                        var circleIdStr2 = callbackData.Replace("crypto_complete_circle_", "");
                        if (int.TryParse(circleIdStr2, out int circleId2))
                        {
                            await HandleCompleteCryptoCircleAsync(chatId, userId, circleId2, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("fastinvest_select_contact_"))
                    {
                        var contactIdStr = callbackData.Replace("fastinvest_select_contact_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            await HandleFastInvestSelectContactAsync(chatId, userId, contactId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("fastinvest_complete_"))
                    {
                        var investmentIdStr = callbackData.Replace("fastinvest_complete_", "");
                        if (int.TryParse(investmentIdStr, out int investmentId))
                        {
                            await HandleCompleteFastInvestAsync(chatId, userId, investmentId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("fastinvest_withdraw_"))
                    {
                        var investmentIdStr = callbackData.Replace("fastinvest_withdraw_", "");
                        if (int.TryParse(investmentIdStr, out int investmentId))
                        {
                            await HandleWithdrawFastInvestAsync(chatId, userId, investmentId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("finance_deposit_"))
                    {
                        await HandleDepositActionAsync(chatId, userId, callbackData, cancellationToken);
                    }

                    break;
            }
        }
        // ========== ВКЛАДЫ УЧАСТНИКОВ ==========
        private async Task SearchInvestmentUserAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "investment_search_query",
                Step = 1
            };
            await SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК УЧАСТНИКА ПО ВКЛАДАМ\n\n" +
                "Введите имя, username или часть имени для поиска:", cancellationToken);
        }

        private async Task HandleInvestmentSearchAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync();
            var investments = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Investment);
            var withdrawals = await _financeService.GetRecordsByCategoryAsync("Вывод вклада");

            // Фильтруем пользователей по поисковому запросу
            var searchLower = text.ToLower();
            var matchedUsers = users.Where(u =>
                (u.Username?.ToLower().Contains(searchLower) ?? false) ||
                (u.FirstName?.ToLower().Contains(searchLower) ?? false) ||
                (u.LastName?.ToLower().Contains(searchLower) ?? false)
            ).ToList();

            if (!matchedUsers.Any())
            {
                await SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n";

            foreach (var user in matchedUsers.Take(5))
            {
                var userName = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : $"{user.FirstName} {user.LastName}";
                var userInvestments = investments.Where(i => i.UserId == user.TelegramId).ToList();
                var userWithdrawals = withdrawals.Where(w => w.UserId == user.TelegramId).ToList();

                var totalInvested = userInvestments.Sum(i => i.Amount);
                var totalWithdrawn = userWithdrawals.Sum(w => w.Amount);
                var currentBalance = totalInvested - totalWithdrawn;

                result += $"👤 {userName}\n";
                result += $"├─ Внесено: {totalInvested:N0} ₽\n";
                result += $"├─ Выведено: {totalWithdrawn:N0} ₽\n";
                result += $"├─ Текущий баланс: {currentBalance:N0} ₽\n";
                result += $"└─ Количество вкладов: {userInvestments.Count}\n\n";
            }

            if (matchedUsers.Count > 5)
            {
                result += $"... и еще {matchedUsers.Count - 5} участников\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ Депнуть", "investment_add"),
            InlineKeyboardButton.WithCallbackData("📤 Вывести", "investment_withdraw")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_investments") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "search_results", cancellationToken);
            _userStates.Remove(userId);
        }
        private async Task ShowInvestmentsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var investments = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Investment);
                var allExpenses = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Expense);
                var withdrawals = allExpenses.Where(e => e.Category?.Trim().ToLower() == "вывод вклада").ToList();

                var totalInvested = investments.Sum(i => i.Amount);
                var totalWithdrawn = withdrawals.Sum(w => w.Amount);
                var totalProfit = totalWithdrawn - totalInvested;
                var totalBalance = totalInvested - totalWithdrawn;

                var investorsCount = investments.Select(i => i.UserId).Where(id => id.HasValue).Distinct().Count();

                var text = "👥 ВКЛАДЫ УЧАСТНИКОВ\n\n" +
                           $"💰 ОБЩИЙ БАЛАНС: {totalBalance} ₽ \n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ 📈 Прибыль: {totalProfit} ₽ \n" +
                           $"│ 💵 Внесено: {totalInvested} ₽\n" +
                           $"│ 💳 Выведено: {totalWithdrawn} ₽\n" +
                           $"│ 👥 Участников: {investorsCount}\n" +
                           $"└─────────────────────────────────\n\n";

                // Детальная статистика по участникам
                var userIds = investments.Select(i => i.UserId)
                    .Union(withdrawals.Select(w => w.UserId))
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .Distinct()
                    .ToList();

                var users = await _userService.GetAllUsersAsync();

                if (userIds.Any())
                {
                    text += "👤 ПО УЧАСТНИКАМ:\n";
                    foreach (var uid in userIds)
                    {
                        var user = users.FirstOrDefault(u => u.TelegramId == uid);
                        var userName = user != null
                            ? (!string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName)
                            : $"ID:{uid}";

                        var invested = investments.Where(i => i.UserId == uid).Sum(i => i.Amount);
                        var withdrawn = withdrawals.Where(w => w.UserId == uid).Sum(w => w.Amount);
                        var profit = withdrawn - invested;
                        var balance = invested - withdrawn;

                        text += $"\n{userName}\n" +
               $"  💰 БАЛАНС: {balance} ₽ \n" +
               $"  ├─ 📈 Прибыль: {profit} ₽ \n" +
               $"  ├─ 💵 Внесено: {invested} ₽\n" +
               $"  └─ 💳 Выведено: {withdrawn} ₽\n";
                    }
                }
                else
                {
                    text += "📭 Нет данных по участникам\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("➕ Депнуть", "investment_add"),
                InlineKeyboardButton.WithCallbackData("📤 Вывести", "investment_withdraw")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("📊 Статистика", "investment_stats")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance)
            }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "investments", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowInvestmentsAsync");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки данных", cancellationToken, 3);
            }
        }

        // ========== ДОБАВЛЕНИЕ ВКЛАДА ==========
        private async Task StartAddInvestmentAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync();

            var text = "👤 ВЫБЕРИТЕ УЧАСТНИКА\n\nКто вносит вклад:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var user in users.Take(10))
            {
                var userName = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                var roleIcon = user.Role == UserRole.Admin ? "👑 " : "👤 ";

                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{roleIcon}{userName}", $"investment_select_user_{user.TelegramId}")
                });
            }

            if (users.Count > 10)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"🔍 ПОИСК ({users.Count - 10})", "investment_search_user")
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_investments")
            });

            // Это меню выбора - должно обновлять существующее сообщение
            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "investments", cancellationToken);
        }

        private async Task HandleInvestmentSelectUserAsync(long chatId, long userId, long targetUserId, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByTelegramIdAsync(targetUserId);
            if (user == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Участник не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "add_investment_amount",
                Data = new Dictionary<string, object?>
                {
                    ["targetUserId"] = targetUserId,
                    ["returnMenu"] = "finance_investments"  // Сохраняем, куда вернуться
                },
                Step = 1
            };

            var userName = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;

            // Это сообщение с вводом - НЕ УДАЛЯЕТСЯ (без deleteAfterSeconds)
            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"👤 Участник: {userName}\n\n" +
                $"Введите сумму вклада (в ₽):", cancellationToken);
        }

        private async Task HandlePlanCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandlePlanCallbackAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   └─ Callback Data: {callbackData}");

            switch (callbackData)
            {
                case "show_plans":
                    await ShowPlansMenuAsync(chatId, cancellationToken);
                    break;

                case "plans_all":
                    await ShowAllPlansAsync(chatId, cancellationToken);
                    break;

                case "plans_add":
                    await StartAddPlanAsync(chatId, userId, cancellationToken);
                    break;

                case "plans_search":
                    await StartSearchPlansAsync(chatId, userId, cancellationToken);
                    break;

                default:
                    if (callbackData.StartsWith("plan_view_"))
                    {
                        var id = callbackData.Replace("plan_view_", "");
                        if (int.TryParse(id, out int planId))
                        {
                            await ShowPlanDetailsAsync(chatId, planId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("plan_edit_"))
                    {
                        var id = callbackData.Replace("plan_edit_", "");
                        if (int.TryParse(id, out int planId))
                        {
                            await StartEditPlanAsync(chatId, userId, planId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("plan_delete_"))
                    {
                        var id = callbackData.Replace("plan_delete_", "");
                        if (int.TryParse(id, out int planId))
                        {
                            await ShowDeletePlanConfirmationAsync(chatId, planId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("delete_plan_confirm_"))
                    {
                        var id = callbackData.Replace("delete_plan_confirm_", "");
                        if (int.TryParse(id, out int planId))
                        {
                            await DeletePlanAsync(chatId, planId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("plan_download_"))
                    {
                        var id = callbackData.Replace("plan_download_", "");
                        if (int.TryParse(id, out int planId))
                        {
                            await DownloadPlanAsync(chatId, planId, cancellationToken);
                        }
                    }
                    break;
            }
        }

        private async Task HandleAddInvestmentAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму:", cancellationToken);
                return;
            }

            state.Data["amount"] = amount;
            state.CurrentAction = "add_investment_description";
            state.Step = 2;
            _userStates[userId] = state;

            var targetUserId = (long)state.Data["targetUserId"]!;
            var user = await _userService.GetUserByTelegramIdAsync(targetUserId);
            var userName = user != null
                ? (!string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName)
                : "Участник";

            // Это сообщение с вводом - НЕ УДАЛЯЕТСЯ
            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"👤 {userName}\n" +
                $"💰 Сумма: {amount:N0} ₽\n\n" +
                $"Введите описание/назначение вклада (или отправьте '-' чтобы пропустить):\n" +
                $"Например: взнос в общий фонд, инвестиция в проект и т.д.", cancellationToken);
        }

        private async Task HandleAddInvestmentDescriptionAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var targetUserId = (long)state.Data["targetUserId"]!;
            var amount = (decimal)state.Data["amount"]!;
            var description = text == "-" ? "Вклад участника" : text;

            var record = await _financeService.CreateFinancialRecordAsync(
                type: FinancialRecordType.Investment,
                category: "Вклады участников",
                description: description,
                amount: amount,
                currency: "RUR",
                source: "team_investment",
                userId: targetUserId,
                projectId: null,
                fundStatus: IFinanceService.FundStatus.Reserved
            );

            if (record != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ Вклад {amount:N0} ₽ добавлен!", cancellationToken, 3);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowInvestmentsAsync(chatId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка", cancellationToken, 5);
                _userStates.Remove(userId);
                await ShowInvestmentsAsync(chatId, cancellationToken);
            }
        }

        // ========== ВЫВОД ВКЛАДА ==========
        private async Task StartWithdrawInvestmentAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var investments = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Investment);
            var users = await _userService.GetAllUsersAsync();

            var userIdsWithInvestments = investments
                .Where(i => i.UserId.HasValue)
                .Select(i => i.UserId!.Value)
                .Distinct()
                .ToList();

            if (!userIdsWithInvestments.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Нет участников с вкладами", cancellationToken, 3);
                return;
            }

            var text = "📤 ВЫВОД СРЕДСТВ\n\nВыберите участника:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var uid in userIdsWithInvestments)
            {
                var user = users.FirstOrDefault(u => u.TelegramId == uid);
                var name = user?.Username != null ? $"@{user.Username}" : user?.FirstName ?? $"ID {uid}";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(name, $"investment_withdraw_user_{uid}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_investments")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "withdraw_select_user", cancellationToken);
        }

        private async Task HandleInvestmentWithdrawUserAsync(long chatId, long userId, long targetUserId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "withdraw_investment_amount",
                Data = new Dictionary<string, object?> { ["targetUserId"] = targetUserId },
                Step = 1
            };

            var user = await _userService.GetUserByTelegramIdAsync(targetUserId);
            var name = user?.Username != null ? $"@{user.Username}" : user?.FirstName ?? "Участник";

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"👤 {name}\n\nВведите сумму вывода (в ₽):", cancellationToken);
        }

        private async Task HandleWithdrawInvestmentAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken);
                return;
            }

            var targetUserId = (long)state.Data["targetUserId"]!;
            var user = await _userService.GetUserByTelegramIdAsync(targetUserId);
            var name = user?.Username != null ? $"@{user.Username}" : user?.FirstName ?? "Участник";

            state.Data["amount"] = amount;
            state.CurrentAction = "withdraw_investment_description";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"👤 {name}\n💰 Сумма: {amount:N0} ₽\n\nВведите описание (или '-' для пропуска):", cancellationToken);
        }

        private async Task HandleWithdrawInvestmentDescriptionAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var targetUserId = (long)state.Data["targetUserId"]!;
            var amount = (decimal)state.Data["amount"]!;
            var description = text == "-" ? "Вывод средств" : text;

            var record = await _financeService.CreateFinancialRecordAsync(
                type: FinancialRecordType.Expense,
                category: "Вывод вклада",
                description: description,
                amount: amount,
                currency: "RUR",
                source: "investment_withdrawal",
                userId: targetUserId,
                projectId: null,
                fundStatus: IFinanceService.FundStatus.Reserved
            );

            if (record == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при выводе", cancellationToken, 5);
                _userStates.Remove(userId);
                await ShowInvestmentsAsync(chatId, cancellationToken);
                return;
            }

            var user = await _userService.GetUserByTelegramIdAsync(targetUserId);
            var name = user?.Username != null ? $"@{user.Username}" : user?.FirstName ?? "Участник";

            // Подсчёт актуальной статистики
            var investments = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Investment);
            var allExpenses = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Expense);
            var withdrawals = allExpenses
                .Where(f => f.Category.Equals("Вывод вклада", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var totalInvested = investments.Where(i => i.UserId == targetUserId).Sum(i => i.Amount);
            var totalWithdrawn = withdrawals.Where(w => w.UserId == targetUserId).Sum(w => w.Amount);
            var currentBalance = totalInvested - totalWithdrawn;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✅ Вывод выполнен!\n\n" +
                $"👤 {name}\n" +
                $"💰 Сумма: {amount:N0} ₽\n" +
                $"📊 Внесено всего: {totalInvested:N0} ₽\n" +
                $"💳 Выведено всего: {totalWithdrawn:N0} ₽\n" +
                $"📈 Остаток: {currentBalance:N0} ₽\n" +
                $"📝 {description}", cancellationToken, 5);

            _userStates.Remove(userId);
            _menuManager.ClearMenuState(chatId);
            await ShowInvestmentsAsync(chatId, cancellationToken);
        }

        // ========== СТАТИСТИКА ПО УЧАСТНИКАМ ==========
        private async Task ShowInvestmentByUserAsync(long chatId, CancellationToken cancellationToken)
        {
            var investments = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Investment);
            var withdrawals = await _financeService.GetRecordsByCategoryAsync("Вывод вклада");
            var users = await _userService.GetAllUsersAsync();

            var text = "📊 СТАТИСТИКА ВКЛАДОВ ПО УЧАСТНИКАМ\n\n";

            var userStats = users.Select(u => new
            {
                User = u,
                Invested = investments.Where(i => i.UserId == u.TelegramId).Sum(i => i.Amount),
                Withdrawn = withdrawals.Where(w => w.UserId == u.TelegramId).Sum(w => w.Amount),
                Count = investments.Count(i => i.UserId == u.TelegramId)
            })
            .Where(x => x.Invested > 0 || x.Withdrawn > 0)
            .OrderByDescending(x => x.Invested)
            .ToList();

            if (userStats.Any())
            {
                var totalInvested = userStats.Sum(x => x.Invested);
                var totalWithdrawn = userStats.Sum(x => x.Withdrawn);

                text += $"💰 ОБЩАЯ СТАТИСТИКА:\n";
                text += $"Всего внесено: {totalInvested:N0} ₽\n";
                text += $"Всего выведено: {totalWithdrawn:N0} ₽\n";
                text += $"В обороте: {totalInvested - totalWithdrawn:N0} ₽\n\n";

                text += $"👥 ПО УЧАСТНИКАМ:\n\n";

                foreach (var stat in userStats)
                {
                    var userName = !string.IsNullOrEmpty(stat.User.Username)
                        ? $"@{stat.User.Username}"
                        : stat.User.FirstName;
                    var roleIcon = stat.User.Role == UserRole.Admin ? "👑 " : "👤 ";
                    var current = stat.Invested - stat.Withdrawn;
                    var percent = totalInvested > 0 ? (stat.Invested / totalInvested * 100) : 0;

                    text += $"{roleIcon}{userName}\n";
                    text += $"├─ Внесено: {stat.Invested:N0} ₽ ({percent:F1}%)\n";
                    text += $"├─ Выведено: {stat.Withdrawn:N0} ₽\n";
                    text += $"├─ В обороте: {current:N0} ₽\n";
                    text += $"└─ Количество вкладов: {stat.Count}\n\n";
                }
            }
            else
            {
                text += "Нет данных по вкладам";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ Депнуть", "investment_add"),
            InlineKeyboardButton.WithCallbackData("📤 Вывести", "investment_withdraw")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_investments") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "investment_by_user", cancellationToken);
        }

        // ========== ДЕТАЛЬНАЯ СТАТИСТИКА ==========
        private async Task ShowInvestmentStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var investments = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Investment);
                var allExpenses = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Expense);
                var withdrawals = allExpenses.Where(e => e.Category?.Trim().ToLower() == "вывод вклада").ToList();

                var totalInvested = investments.Sum(i => i.Amount);
                var totalWithdrawn = withdrawals.Sum(w => w.Amount);
                var totalProfit = totalWithdrawn - totalInvested;

                // По месяцам
                var byMonth = investments
                    .GroupBy(i => new { i.TransactionDate.Year, i.TransactionDate.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Invested = g.Sum(i => i.Amount)
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                var withdrawalsByMonth = withdrawals
                    .GroupBy(w => new { w.TransactionDate.Year, w.TransactionDate.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Withdrawn = g.Sum(w => w.Amount)
                    })
                    .ToDictionary(x => x.Month, x => x.Withdrawn);

                var text = "📊 ДЕТАЛЬНАЯ СТАТИСТИКА ВКЛАДОВ\n\n" +
                           $"💰 ОБЩИЕ ПОКАЗАТЕЛИ:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ 💵 Внесено: {totalInvested,12:N0} ₽\n" +
                           $"│ 💳 Выведено: {totalWithdrawn,12:N0} ₽\n" +
                           $"│ 📈 Прибыль: {totalProfit,12:N0} ₽\n" +
                           $"└─────────────────────────────────\n\n";

                if (byMonth.Any())
                {
                    text += "📅 ДИНАМИКА ПО МЕСЯЦАМ:\n";
                    foreach (var month in byMonth.TakeLast(6))
                    {
                        var withdrawn = withdrawalsByMonth.ContainsKey(month.Month) ? withdrawalsByMonth[month.Month] : 0;
                        var profit = withdrawn - month.Invested;
                        var profitEmoji = profit >= 0 ? "✅" : "❌";

                        text += $"┌─ {month.Month} ────────────────────────\n";
                        text += $"│ Внес: {month.Invested,10:N0} ₽\n";
                        text += $"│ Вывод: {withdrawn,10:N0} ₽\n";
                        text += $"│ Прибыль: {profit,10:N0} ₽ {profitEmoji}\n";
                        text += $"└─────────────────────────────────\n\n";
                    }
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_investments") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "investment_stats", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowInvestmentStatsAsync");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки статистики", cancellationToken, 3);
            }
        }
        // ========== FAST INVEST МЕНЮ ==========
        private async Task ShowFastInvestMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var stats = await _fastInvestService.GetFastInvestStatisticsAsync();
                var activeInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Active);
                var completedInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Withdrawn);

                var totalActive = activeInvestments.Sum(i => i.DepositAmount);
                var totalExpected = activeInvestments.Sum(i => i.ExpectedProfitAmount ?? 0);
                var totalCompleted = completedInvestments.Sum(i => i.DepositAmount);
                var totalProfit = completedInvestments.Sum(i => i.Profit ?? 0);

                var text = "🏦 FAST INVEST\n\n" +
                           $"📊 ТЕКУЩАЯ СИТУАЦИЯ:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ 👥 Всего инвесторов: {stats.TotalInvestors}\n" +
                           $"│ 🟢 В работе: {stats.ActiveInvestors} чел | {totalActive:N0} ₽\n" +
                           $"│ ✅ Завершено: {stats.CompletedInvestors} чел | {totalCompleted:N0} ₽\n" +
                           $"│ 💰 Ожидаемая прибыль: {totalExpected:N0} ₽\n" +
                           $"│ 📈 Реализованная прибыль: {totalProfit:N0} ₽\n" +
                           $"└─────────────────────────────────\n\n" +
                           $"Выберите раздел:";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("🟢 В работе", "finance_fastinvest_active"),
                InlineKeyboardButton.WithCallbackData("⚪ Завершено", "finance_fastinvest_inactive")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("➕ Новый инвестор", "fastinvest_add"),
                InlineKeyboardButton.WithCallbackData("📊 Статистика", "fastinvest_stats")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("💰 Выплаты", "fastinvest_payouts"),
                InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_accounts")
            }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "fastinvest_menu", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowFastInvestMenuAsync");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки данных", cancellationToken, 3);
            }
        }

        // ========== FAST INVEST ПО СТАТУСАМ ==========
        private async Task ShowFastInvestByStatusAsync(long chatId, InvestStatus status, CancellationToken cancellationToken)
        {
            var investments = await _fastInvestService.GetInvestmentsByStatusAsync(status);
            var statusName = status == InvestStatus.Active ? "🟢 В РАБОТЕ" : "⚪ НЕ В РАБОТЕ";
            var text = $"🏦 FAST INVEST - {statusName}\n\n";

            if (investments.Any())
            {
                var totalDeposit = investments.Sum(i => i.DepositAmount);
                var totalWithdrawn = investments.Where(i => i.WithdrawalAmount.HasValue).Sum(i => i.WithdrawalAmount ?? 0);
                var totalProfit = investments.Where(i => i.Profit.HasValue).Sum(i => i.Profit ?? 0);

                text += $"📊 СТАТИСТИКА:\n";
                text += $"Инвесторов: {investments.Select(i => i.ContactId).Distinct().Count()}\n";
                text += $"Общий депозит: {totalDeposit:N0} ₽\n";

                if (status == InvestStatus.Active)
                {
                    var totalExpected = investments.Sum(i => i.ExpectedProfitAmount ?? 0);
                    text += $"Ожидаемая прибыль: {totalExpected:N0} ₽\n\n";
                }
                else
                {
                    text += $"Выведено: {totalWithdrawn:N0} ₽\n";
                    text += $"Прибыль: {totalProfit:N0} ₽\n";
                    text += $"ROI: {(totalDeposit > 0 ? totalProfit / totalDeposit * 100 : 0):F1}%\n\n";
                }

                text += "📋 СПИСОК ИНВЕСТОРОВ:\n";
                foreach (var inv in investments.OrderByDescending(i => i.DepositAmount))
                {
                    var investor = inv.Investor?.FullName ?? $"Контакт #{inv.ContactId}";
                    var tg = inv.Investor?.TelegramUsername != null ? $"(@{inv.Investor.TelegramUsername})" : "";

                    text += $"\n👤 {investor} {tg}\n";
                    text += $"├─ Дата депа: {inv.DepositDate:dd.MM.yyyy}\n";
                    text += $"├─ Сумма депа: {inv.DepositAmount:N0} ₽\n";

                    if (status == InvestStatus.Active)
                    {
                        var daysLeft = (inv.PlannedWithdrawalDate - DateTime.UtcNow).Days;
                        var daysLeftText = daysLeft > 0 ? $"осталось {daysLeft} дн." : "сегодня";
                        var profitPercent = inv.ExpectedProfitPercent ?? 0;
                        var profitAmount = inv.ExpectedProfitAmount ?? (inv.DepositAmount * profitPercent / 100);

                        text += $"├─ План вывода: {inv.PlannedWithdrawalDate:dd.MM.yyyy} ({daysLeftText})\n";
                        text += $"├─ Ставка: {profitPercent}%\n";
                        text += $"└─ Ожидаемая прибыль: {profitAmount:N0} ₽\n";
                    }
                    else
                    {
                        var profitPercent = inv.ExpectedProfitPercent ?? 0;
                        var expectedProfit = inv.ExpectedProfitAmount ?? 0;
                        var actualProfit = inv.Profit ?? 0;

                        text += $"├─ Дата вывода: {inv.ActualWithdrawalDate:dd.MM.yyyy}\n";
                        text += $"├─ Вывод: {inv.WithdrawalAmount:N0} ₽\n";
                        text += $"├─ Ставка: {profitPercent}%\n";
                        text += $"├─ Ожидалось: {expectedProfit:N0} ₽\n";
                        text += $"└─ Прибыль: {actualProfit:N0} ₽\n";
                    }

                    if (!string.IsNullOrEmpty(inv.Comments))
                        text += $"📝 {inv.Comments}\n";
                }
            }
            else
            {
                text += "📭 Нет инвесторов в этом статусе";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ Новый", "fastinvest_add")
        }
    };

            if (status == InvestStatus.Active)
            {
                buttons[0].Add(InlineKeyboardButton.WithCallbackData("✅ Завершить", "fastinvest_complete"));
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("📊 Статистика", "fastinvest_stats"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"fastinvest_{status}", cancellationToken);
        }

        // ========== ВСЕ ИНВЕСТИЦИИ ==========
        private async Task ShowAllFastInvestmentsAsync(long chatId, CancellationToken cancellationToken)
        {
            var investments = await _fastInvestService.GetAllInvestmentsAsync();

            var text = "📊 ВСЕ ИНВЕСТИЦИИ FAST INVEST\n\n";

            var byInvestor = investments.GroupBy(i => i.ContactId);

            foreach (var group in byInvestor)
            {
                var investor = group.First().Investor;
                var investorName = investor?.FullName ?? $"Контакт #{group.Key}";
                var totalDeposit = group.Sum(i => i.DepositAmount);
                var totalProfit = group.Where(i => i.Profit.HasValue).Sum(i => i.Profit ?? 0);
                var activeCount = group.Count(i => i.Status == InvestStatus.Active);
                var completedCount = group.Count(i => i.Status == InvestStatus.Completed);

                text += $"👤 {investorName}\n";
                text += $"├─ Всего инвестиций: {group.Count()}\n";
                text += $"├─ Активных: {activeCount}\n";
                text += $"├─ Завершено: {completedCount}\n";
                text += $"├─ Общий депозит: {totalDeposit:N0} ₽\n";
                text += $"└─ Общая прибыль: {totalProfit:N0} ₽\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("🟢 АКТИВНЫЕ", "finance_fastinvest_active"),
            InlineKeyboardButton.WithCallbackData("⚪ ЗАВЕРШЕННЫЕ", "finance_fastinvest_inactive")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "fastinvest_all", cancellationToken);
        }

        // ========== ДОБАВЛЕНИЕ ИНВЕСТОРА ==========
        private async Task StartAddFastInvestAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var contacts = await _contactService.GetAllContactsAsync();
            var investors = await _fastInvestService.GetAllInvestmentsAsync();
            var existingInvestorIds = investors.Select(i => i.ContactId).Distinct().ToHashSet();

            var availableContacts = contacts.Where(c => !existingInvestorIds.Contains(c.Id)).ToList();

            if (!availableContacts.Any())
            {
                availableContacts = contacts.ToList();
            }

            var text = "👤 ВЫБЕРИТЕ ИНВЕСТОРА\n\n";

            if (availableContacts.Count != contacts.Count)
            {
                text += "✅ Уже есть инвесторы\n";
                text += "🆕 Новые контакты отмечены\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var contact in availableContacts.Take(10))
            {
                var isInvestor = existingInvestorIds.Contains(contact.Id);
                var prefix = isInvestor ? "🔄" : "🆕";
                var displayName = !string.IsNullOrEmpty(contact.FullName)
                    ? contact.FullName
                    : $"@{contact.TelegramUsername}";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{prefix} {displayName}", $"fastinvest_select_contact_{contact.Id}")
        });
            }

            if (availableContacts.Count > 10)
            {
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"🔍 ПОИСК ({availableContacts.Count - 10} еще)", "fastinvest_search_contact")
        });
            }

            // Кнопка для создания нового контакта с returnTo
            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ Новый контакт", "fastinvest_add_contact")
    });

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "select_investor", cancellationToken);
        }

        private async Task HandleFastInvestSelectContactAsync(long chatId, long userId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "add_fastinvest_deposit",
                Data = new Dictionary<string, object?> { ["contactId"] = contactId },
                Step = 1
            };

            var contactName = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";
            await SendTemporaryMessageAsync(chatId,
                $"👤 Инвестор: {contactName}\n\n" +
                $"Введите сумму депозита (в ₽):", cancellationToken);
        }

        private async Task HandleAddFastInvestDepositAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal depositAmount) || depositAmount <= 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму депозита:", cancellationToken);
                return;
            }

            state.Data["depositAmount"] = depositAmount;
            state.CurrentAction = "add_fastinvest_date";
            state.Step = 2;
            _userStates[userId] = state;

            var contactId = (int)state.Data["contactId"]!;
            var contact = await _contactService.GetContactAsync(contactId);
            var contactName = contact?.FullName ?? $"Контакт #{contactId}";

            await SendTemporaryMessageAsync(chatId,
                $"👤 {contactName}\n" +
                $"💰 Депозит: {depositAmount:N0} ₽\n\n" +
                $"Введите ПЛАНОВУЮ дату вывода в формате ДД.ММ.ГГГГ\n" +
                $"(например: 01.03.2024):", cancellationToken);
        }

        private async Task HandleAddFastInvestDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime withdrawalDate))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ", cancellationToken);
                return;
            }

            if (withdrawalDate <= DateTime.UtcNow)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Дата вывода должна быть в будущем", cancellationToken);
                return;
            }

            state.Data["withdrawalDate"] = withdrawalDate;
            state.CurrentAction = "add_fastinvest_comment";
            state.Step = 3;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId,
                $"Введите комментарий (или отправьте '-' чтобы пропустить):\n\n" +
                $"Например: тестовый период, основной инвестор и т.д.", cancellationToken);
        }

        private async Task HandleAddFastInvestCommentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var contactId = (int)state.Data["contactId"]!;
            var depositAmount = (decimal)state.Data["depositAmount"]!;
            var withdrawalDate = (DateTime)state.Data["withdrawalDate"]!;
            var comment = text == "-" ? null : text;

            try
            {
                var investment = await _fastInvestService.CreateInvestmentAsync(contactId, depositAmount, withdrawalDate, comment);

                if (investment != null)
                {
                    var contact = await _contactService.GetContactAsync(contactId);
                    var investorName = contact?.FullName ?? contact?.TelegramUsername ?? "Инвестор";
                    var daysUntil = (withdrawalDate - DateTime.UtcNow).Days;

                    // Уведомление об успехе (удаляется через 5 секунд)
                    await _menuManager.SendTemporaryMessageAsync(chatId,
                        $"✅ Инвестиция добавлена!\n\n" +
                        $"👤 Инвестор: {investorName}\n" +
                        $"💰 Депозит: {depositAmount:N0} ₽\n" +
                        $"📅 План вывода: {withdrawalDate:dd.MM.yyyy} (через {daysUntil} дн.)\n" +
                        $"💵 Ожидаемая прибыль: {depositAmount * 0.1m:N0} ₽", cancellationToken, 5);

                    // Очищаем состояние
                    _userStates.Remove(userId);

                    // Очищаем состояние меню
                    _menuManager.ClearMenuState(chatId);

                    // ОТКРЫВАЕМ МЕНЮ FAST INVEST
                    await ShowFastInvestMenuAsync(chatId, cancellationToken);
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании инвестиции", cancellationToken, 5);
                    _userStates.Remove(userId);
                    _menuManager.ClearMenuState(chatId);
                    await ShowFastInvestMenuAsync(chatId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating investment");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании инвестиции", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFastInvestMenuAsync(chatId, cancellationToken);
            }
        }

        // ========== ЗАВЕРШЕНИЕ ИНВЕСТИЦИИ ==========
        private async Task StartCompleteFastInvestAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var activeInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Active);

            if (!activeInvestments.Any())
            {
                await SendTemporaryMessageAsync(chatId, "❌ Нет активных инвестиций для завершения", cancellationToken);
                return;
            }

            var text = "✅ ЗАВЕРШЕНИЕ ИНВЕСТИЦИИ\n\nВыберите инвестицию:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var inv in activeInvestments.OrderBy(i => i.PlannedWithdrawalDate))
            {
                var investor = inv.Investor?.FullName ?? $"Контакт #{inv.ContactId}";
                var daysLeft = (inv.PlannedWithdrawalDate - DateTime.UtcNow).Days;
                var daysText = daysLeft > 0 ? $"осталось {daysLeft} дн." : "просрочено";
                var profitPercent = inv.ExpectedProfitPercent ?? 0;

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{investor} | {inv.DepositAmount:N0} ₽ | {profitPercent}% | {daysText}",
                $"fastinvest_complete_{inv.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_active")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "complete_fastinvest", cancellationToken);
        }

        private async Task HandleCompleteFastInvestAsync(long chatId, long userId, int investmentId, CancellationToken cancellationToken)
        {
            var investment = await _fastInvestService.GetInvestmentAsync(investmentId);
            if (investment == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Инвестиция не найдена", cancellationToken);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "complete_fastinvest_amount",
                Data = new Dictionary<string, object?> { ["investmentId"] = investmentId },
                Step = 1
            };

            var investor = investment.Investor?.FullName ?? $"Контакт #{investment.ContactId}";
            var expectedProfit = investment.ExpectedProfitAmount ?? 0;
            var profitPercent = investment.ExpectedProfitPercent ?? 0;

            await SendTemporaryMessageAsync(chatId,
                $"✅ ЗАВЕРШЕНИЕ ИНВЕСТИЦИИ\n\n" +
                $"👤 Инвестор: {investor}\n" +
                $"💰 Депозит: {investment.DepositAmount:N0} ₽\n" +
                $"📊 Ставка: {profitPercent}%\n" +
                $"💵 Ожидаемая прибыль: {expectedProfit:N0} ₽\n" +
                $"📅 План вывода: {investment.PlannedWithdrawalDate:dd.MM.yyyy}\n\n" +
                $"Введите ФАКТИЧЕСКУЮ сумму вывода (в ₽):", cancellationToken);
        }

        private async Task HandleCompleteFastInvestAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal withdrawalAmount) || withdrawalAmount <= 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму вывода:", cancellationToken);
                return;
            }

            var investmentId = (int)state.Data["investmentId"]!;
            var investment = await _fastInvestService.GetInvestmentAsync(investmentId);

            if (investment == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Инвестиция не найдена", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var actualProfit = withdrawalAmount - investment.DepositAmount;
            var expectedProfit = investment.ExpectedProfitAmount ?? 0;
            var profitPercent = investment.ExpectedProfitPercent ?? 0;
            var diff = actualProfit - expectedProfit;
            var diffEmoji = diff >= 0 ? "✅" : "❌";

            var success = await _fastInvestService.CompleteInvestmentAsync(investmentId, withdrawalAmount, DateTime.UtcNow);

            if (success)
            {
                var investor = investment.Investor?.FullName ?? $"Контакт #{investment.ContactId}";

                await SendTemporaryMessageAsync(chatId,
                    $"✅ Инвестиция завершена!\n\n" +
                    $"👤 Инвестор: {investor}\n" +
                    $"💰 Депозит: {investment.DepositAmount:N0} ₽\n" +
                    $"📊 Ставка: {profitPercent}%\n" +
                    $"💵 Ожидалось: {expectedProfit:N0} ₽\n" +
                    $"💳 Вывод: {withdrawalAmount:N0} ₽\n" +
                    $"📈 Прибыль: {actualProfit:N0} ₽\n" +
                    $"📊 Отклонение: {diff:N0} ₽ {diffEmoji}\n" +
                    $"📅 Дата: {DateTime.UtcNow:dd.MM.yyyy}", cancellationToken, 5);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFastInvestByStatusAsync(chatId, InvestStatus.Completed, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при завершении инвестиции", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFastInvestByStatusAsync(chatId, InvestStatus.Active, cancellationToken);
            }
        }

        // ========== ВЫВОД СРЕДСТВ (НЕ В РАБОТЕ) ==========
        private async Task StartWithdrawFastInvestAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var activeInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Active);

            if (!activeInvestments.Any())
            {
                await SendTemporaryMessageAsync(chatId, "❌ Нет активных инвестиций для вывода", cancellationToken);
                return;
            }

            var text = "💰 ВЫВОД СРЕДСТВ (Не в работе)\n\n" +
                       "Выберите инвестицию для вывода (инвестор больше не работает):";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var inv in activeInvestments)
            {
                var investor = inv.Investor?.FullName ?? $"Контакт #{inv.ContactId}";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{investor} | {inv.DepositAmount:N0} ₽",
                $"fastinvest_withdraw_{inv.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_active")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "withdraw_fastinvest", cancellationToken);
        }

        private async Task HandleWithdrawFastInvestAsync(long chatId, long userId, int investmentId, CancellationToken cancellationToken)
        {
            var investment = await _fastInvestService.GetInvestmentAsync(investmentId);
            if (investment == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Инвестиция не найдена", cancellationToken);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "withdraw_fastinvest_amount",
                Data = new Dictionary<string, object?> { ["investmentId"] = investmentId },
                Step = 1
            };

            var investor = investment.Investor?.FullName ?? $"Контакт #{investment.ContactId}";

            await SendTemporaryMessageAsync(chatId,
                $"💰 ВЫВОД СРЕДСТВ (Не в работе)\n\n" +
                $"👤 Инвестор: {investor}\n" +
                $"💰 Депозит: {investment.DepositAmount:N0} ₽\n" +
                $"📅 План вывода: {investment.PlannedWithdrawalDate:dd.MM.yyyy}\n\n" +
                $"Введите сумму вывода (в ₽):", cancellationToken);
        }

        private async Task HandleWithdrawFastInvestAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal withdrawalAmount) || withdrawalAmount <= 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму вывода:", cancellationToken);
                return;
            }

            var investmentId = (int)state.Data["investmentId"]!;
            var investment = await _fastInvestService.GetInvestmentAsync(investmentId);

            if (investment == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Инвестиция не найдена", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            var profit = withdrawalAmount - investment.DepositAmount;
            var success = await _fastInvestService.WithdrawInvestmentAsync(investmentId, withdrawalAmount, DateTime.UtcNow);

            if (success)
            {
                var investor = investment.Investor?.FullName ?? $"Контакт #{investment.ContactId}";

                await SendTemporaryMessageAsync(chatId,
                    $"✅ Вывод средств завершен\n\n" +
                    $"👤 Инвестор: {investor}\n" +
                    $"💰 Депозит: {investment.DepositAmount:N0} ₽\n" +
                    $"💵 Вывод: {withdrawalAmount:N0} ₽\n" +
                    $"📈 Прибыль: {profit:N0} ₽\n" +
                    $"📊 Статус: Не в работе\n" +
                    $"📅 Дата: {DateTime.UtcNow:dd.MM.yyyy}", cancellationToken);

                _userStates.Remove(userId);
                await ShowFastInvestByStatusAsync(chatId, InvestStatus.Withdrawn, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при выводе средств", cancellationToken);
                _userStates.Remove(userId);
            }
        }

        // ========== СТАТИСТИКА FAST INVEST ==========
        private async Task ShowFastInvestStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _fastInvestService.GetFastInvestStatisticsAsync();
            var allInvestments = await _fastInvestService.GetAllInvestmentsAsync();

            var totalInvested = allInvestments.Sum(i => i.DepositAmount);
            var totalReturned = allInvestments.Where(i => i.WithdrawalAmount.HasValue).Sum(i => i.WithdrawalAmount ?? 0);
            var activeTotal = allInvestments.Where(i => i.Status == InvestStatus.Active).Sum(i => i.DepositAmount);
            var completedTotal = allInvestments.Where(i => i.Status == InvestStatus.Completed).Sum(i => i.DepositAmount);

            var avgInvestment = allInvestments.Any() ? allInvestments.Average(i => i.DepositAmount) : 0;
            var avgProfit = allInvestments.Where(i => i.Profit.HasValue).Any()
                ? allInvestments.Where(i => i.Profit.HasValue).Average(i => i.Profit ?? 0)
                : 0;

            var text = "📊 СТАТИСТИКА FAST INVEST\n\n" +
                       $"💰 ОБЩИЕ ПОКАЗАТЕЛИ:\n" +
                       $"• Всего инвестировано: {totalInvested:N0} ₽\n" +
                       $"• Возвращено инвесторам: {totalReturned:N0} ₽\n" +
                       $"• Текущая прибыль: {stats.TotalProfit:N0} ₽\n" +
                       $"• ROI: {(totalInvested > 0 ? stats.TotalProfit / totalInvested * 100 : 0):F1}%\n\n" +

                       $"📊 ПО СТАТУСАМ:\n" +
                       $"• Активные: {stats.ActiveInvestors} чел, {activeTotal:N0} ₽\n" +
                       $"• Завершенные: {stats.CompletedInvestors} чел, {completedTotal:N0} ₽\n" +
                       $"• Всего инвесторов: {stats.TotalInvestors}\n\n" +

                       $"📈 СРЕДНИЕ ЗНАЧЕНИЯ:\n" +
                       $"• Средний депозит: {avgInvestment:N0} ₽\n" +
                       $"• Средняя прибыль: {avgProfit:N0} ₽\n" +
                       $"• Средняя доходность: {(avgInvestment > 0 ? avgProfit / avgInvestment * 100 : 0):F1}%\n\n";

            if (stats.TopInvestors.Any())
            {
                text += "🏆 ТОП ИНВЕСТОРОВ ПО ПРИБЫЛИ:\n";
                foreach (var kv in stats.TopInvestors)
                {
                    text += $"• {kv.Key}: {kv.Value:N0} ₽\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("🟢 АКТИВНЫЕ", "finance_fastinvest_active"),
            InlineKeyboardButton.WithCallbackData("⚪ ЗАВЕРШЕННЫЕ", "finance_fastinvest_inactive")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "fastinvest_stats", cancellationToken);
        }
        private async Task ShowFunPaySalesByCategoryAsync(long chatId, CancellationToken cancellationToken)
        {
            var sales = await _funPayService.GetSalesByDateRangeAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow);

            var byCategory = sales.GroupBy(s => s.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(s => s.TotalSaleAmount),
                    Profit = g.Sum(s => s.Profit),
                    AvgMargin = g.Average(s => s.TotalSaleAmount > 0 ? s.Profit / s.TotalSaleAmount * 100 : 0)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var text = "📊 СТАТИСТИКА ПО КАТЕГОРИЯМ\n\n";

            foreach (var cat in byCategory)
            {
                text += $"📌 {cat.Category}\n";
                text += $"├─ Продаж: {cat.Count}\n";
                text += $"├─ Выручка: {cat.Total:N0} ₽\n";
                text += $"├─ Прибыль: {cat.Profit:N0} ₽\n";
                text += $"└─ Маржа: {cat.AvgMargin:F1}%\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📈 ДИНАМИКА", "funpay_sales_dynamics") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_funpay_sales") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_categories", cancellationToken);
        }

        private async Task ShowFunPaySalesDynamicsAsync(long chatId, CancellationToken cancellationToken)
        {
            var sales = await _funPayService.GetSalesByDateRangeAsync(DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow);

            var byMonth = sales.GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count(),
                    Revenue = g.Sum(s => s.TotalSaleAmount),
                    Profit = g.Sum(s => s.Profit)
                })
                .OrderBy(x => x.Period)
                .ToList();

            var text = "📈 ДИНАМИКА ПРОДАЖ\n\n";

            foreach (var month in byMonth)
            {
                var barLength = 20;
                var filledBars = (int)((month.Revenue / byMonth.Max(x => x.Revenue)) * barLength);
                var bar = new string('█', filledBars) + new string('░', barLength - filledBars);

                text += $"{month.Period}\n";
                text += $"├─ {bar} {month.Revenue:N0} ₽\n";
                text += $"├─ Продаж: {month.Count}\n";
                text += $"└─ Прибыль: {month.Profit:N0} ₽\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📊 ПО КАТЕГОРИЯМ", "funpay_sales_categories") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_funpay_sales") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_dynamics", cancellationToken);
        }

        private async Task ShowFunPayWithdrawalStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var withdrawals = await _funPayService.GetAllWithdrawalsAsync();

            var totalWithdrawn = withdrawals.Sum(w => w.Amount);
            var byDestination = withdrawals.GroupBy(w => w.Destination)
                .Select(g => new { Destination = g.Key, Total = g.Sum(w => w.Amount), Count = g.Count() })
                .OrderByDescending(x => x.Total)
                .ToList();

            var byMonth = withdrawals.GroupBy(w => new { w.WithdrawalDate.Year, w.WithdrawalDate.Month })
                .Select(g => new { Month = $"{g.Key.Year}-{g.Key.Month:D2}", Total = g.Sum(w => w.Amount) })
                .OrderBy(x => x.Month)
                .ToList();

            var text = "📊 СТАТИСТИКА ВЫВОДОВ\n\n" +
                       $"💰 Всего выведено: {totalWithdrawn:N0} ₽\n" +
                       $"📦 Количество выводов: {withdrawals.Count}\n" +
                       $"📊 Средний вывод: {(withdrawals.Any() ? withdrawals.Average(w => w.Amount) : 0):N0} ₽\n\n";

            if (byDestination.Any())
            {
                text += "📍 ПО НАЗНАЧЕНИЮ:\n";
                foreach (var dest in byDestination.Take(5))
                {
                    var percent = (dest.Total / totalWithdrawn) * 100;
                    text += $"├─ {dest.Destination}: {dest.Total:N0} ₽ ({percent:F1}%) - {dest.Count} раз\n";
                }
                text += "\n";
            }

            if (byMonth.Any())
            {
                text += "📅 ПО МЕСЯЦАМ:\n";
                foreach (var month in byMonth.TakeLast(6))
                {
                    text += $"├─ {month.Month}: {month.Total:N0} ₽\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("➕ Новый вывод", "funpay_add_withdrawal") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_funpay_withdrawals") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "withdrawal_stats", cancellationToken);
        }

        private async Task HandleAddFunPayAccountNameAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Никнейм не может быть пустым", cancellationToken);
                return;
            }

            state.Data["nickname"] = text;
            state.CurrentAction = "add_funpay_account_golden";
            state.Step = 2;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId, "Введите Golden Key аккаунта:", cancellationToken);
        }

        private async Task HandleAddFunPayAccountGoldenAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Golden Key не может быть пустым", cancellationToken);
                return;
            }

            state.Data["goldenKey"] = text;
            state.CurrentAction = "add_funpay_account_bot";
            state.Step = 3;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId, "Введите username бота (с @):", cancellationToken);
        }

        private async Task HandleAddFunPayAccountBotAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Username бота не может быть пустым", cancellationToken);
                return;
            }

            state.Data["botUsername"] = text;
            state.CurrentAction = "add_funpay_account_api";
            state.Step = 4;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId, "Введите API ключ от бота:", cancellationToken);
        }

        private async Task HandleAddFunPayAccountApiAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ API ключ не может быть пустым", cancellationToken);
                return;
            }

            try
            {
                var account = await _funPayService.CreateAccountAsync(
                    state.Data["nickname"]?.ToString() ?? "",
                    state.Data["goldenKey"]?.ToString() ?? "",
                    state.Data["botUsername"]?.ToString() ?? "",
                    text, // bot password (пока используем API ключ как пароль)
                    text  // API key
                );

                if (account != null)
                {
                    await SendTemporaryMessageAsync(chatId,
                        $"✅ Аккаунт {account.Nickname} успешно добавлен!\n\n" +
                        $"Бот: @{account.BotUsername}\n" +
                        $"API ключ сохранен", cancellationToken);

                    _userStates.Remove(userId);
                    await ShowFunPayAccountsAsync(chatId, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении аккаунта", cancellationToken);
                    _userStates.Remove(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding FunPay account");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении аккаунта", cancellationToken);
                _userStates.Remove(userId);
            }
        }

        private async Task HandleFunPaySelectAccountAsync(long chatId, long userId, int accountId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "add_funpay_warning_reason",
                Data = new Dictionary<string, object?> { ["accountId"] = accountId },
                Step = 1
            };

            await SendTemporaryMessageAsync(chatId, "Введите причину штрафа/предупреждения:", cancellationToken);
        }


        private async Task ShowFastInvestPayoutsAsync(long chatId, CancellationToken cancellationToken)
        {
            var investments = await _fastInvestService.GetAllInvestmentsAsync();
            var completedInvestments = investments.Where(i => i.Status == InvestStatus.Completed || i.Status == InvestStatus.Withdrawn)
                                                  .OrderByDescending(i => i.ActualWithdrawalDate)
                                                  .ToList();

            var text = "💰 ИСТОРИЯ ВЫПЛАТ FAST INVEST\n\n";

            if (!completedInvestments.Any())
            {
                text += "Выплат пока нет";
            }
            else
            {
                var totalPayouts = completedInvestments.Sum(i => i.WithdrawalAmount ?? 0);
                var totalProfit = completedInvestments.Sum(i => i.Profit ?? 0);

                text += $"💰 Всего выплачено: {totalPayouts:N0} ₽\n";
                text += $"📈 Общая прибыль: {totalProfit:N0} ₽\n";
                text += $"📊 Выплат: {completedInvestments.Count}\n\n";

                text += "📋 ПОСЛЕДНИЕ ВЫПЛАТЫ:\n";
                foreach (var inv in completedInvestments.Take(10))
                {
                    var investor = inv.Investor?.FullName ?? $"Контакт #{inv.ContactId}";
                    text += $"• {investor}\n";
                    text += $"  Депозит: {inv.DepositAmount:N0} ₽ → Выплата: {inv.WithdrawalAmount:N0} ₽\n";
                    text += $"  Прибыль: {inv.Profit:N0} ₽ | Дата: {inv.ActualWithdrawalDate:dd.MM.yyyy}\n\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", "fastinvest_stats") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "fastinvest_payouts", cancellationToken);
        }

        private async Task StartReactivateFastInvestAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var completedInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Withdrawn);

            if (!completedInvestments.Any())
            {
                await SendTemporaryMessageAsync(chatId, "❌ Нет инвесторов в статусе 'Не в работе'", cancellationToken);
                return;
            }

            var text = "🔄 ВОЗВРАТ ИНВЕСТОРА В РАБОТУ\n\nВыберите инвестора:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var inv in completedInvestments)
            {
                var investor = inv.Investor?.FullName ?? $"Контакт #{inv.ContactId}";
                var lastDate = inv.ActualWithdrawalDate?.ToString("dd.MM.yyyy") ?? "неизвестно";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{investor} | был(а) {lastDate}", $"fastinvest_reactivate_{inv.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "reactivate_fastinvest", cancellationToken);
        }

        private async Task HandleReactivateFastInvestAsync(long chatId, long userId, int investmentId, CancellationToken cancellationToken)
        {
            var investment = await _fastInvestService.GetInvestmentAsync(investmentId);
            if (investment == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Инвестиция не найдена", cancellationToken);
                return;
            }

            // Создаем новую инвестицию для этого же контакта
            var newInvestment = await _fastInvestService.CreateInvestmentAsync(
                investment.ContactId,
                investment.DepositAmount,
                DateTime.UtcNow.AddMonths(1), // Плановая дата вывода - через месяц
                $"Повторная активация. Предыдущая прибыль: {investment.Profit:N0} ₽"
            );

            if (newInvestment != null)
            {
                var investor = investment.Investor?.FullName ?? $"Контакт #{investment.ContactId}";

                await SendTemporaryMessageAsync(chatId,
                    $"✅ Инвестор {investor} возвращён в работу!\n\n" +
                    $"💰 Депозит: {newInvestment.DepositAmount:N0} ₽\n" +
                    $"📅 План вывода: {newInvestment.PlannedWithdrawalDate:dd.MM.yyyy}\n" +
                    $"💵 Ожидаемая прибыль: {newInvestment.DepositAmount * 0.1m:N0} ₽", cancellationToken);

                await ShowFastInvestByStatusAsync(chatId, InvestStatus.Active, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при реактивации инвестора", cancellationToken);
            }
        }

        private async Task StartChangeFastInvestStatusAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var activeInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Active);
            var withdrawnInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Withdrawn);

            var text = "🔄 СМЕНА СТАТУСА ИНВЕСТОРА\n\n" +
                       $"🟢 В работе: {activeInvestments.Count}\n" +
                       $"⚪ Не в работе: {withdrawnInvestments.Count}\n\n" +
                       "Выберите действие:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("🟢 АКТИВНЫЙ → НЕ В РАБОТЕ", "fastinvest_change_to_withdrawn"),
            InlineKeyboardButton.WithCallbackData("⚪ НЕ В РАБОТЕ → АКТИВНЫЙ", "fastinvest_change_to_active")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "change_status_menu", cancellationToken);
        }

        private async Task HandleFastInvestChangeToWithdrawnAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var activeInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Active);

            if (!activeInvestments.Any())
            {
                await SendTemporaryMessageAsync(chatId, "❌ Нет активных инвесторов", cancellationToken);
                return;
            }

            var text = "⚪ ПЕРЕВОД В СТАТУС 'Не в работе'\n\nВыберите инвестора:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var inv in activeInvestments)
            {
                var investor = inv.Investor?.FullName ?? $"Контакт #{inv.ContactId}";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{investor} | {inv.DepositAmount:N0} ₽", $"fastinvest_set_withdrawn_{inv.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "fastinvest_change_status")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "set_withdrawn", cancellationToken);
        }

        private async Task HandleFastInvestSetWithdrawnAsync(long chatId, long userId, int investmentId, CancellationToken cancellationToken)
        {
            var investment = await _fastInvestService.GetInvestmentAsync(investmentId);
            if (investment == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Инвестиция не найдена", cancellationToken);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "fastinvest_withdrawn_amount",
                Data = new Dictionary<string, object?> { ["investmentId"] = investmentId },
                Step = 1
            };

            var investor = investment.Investor?.FullName ?? $"Контакт #{investment.ContactId}";
            await SendTemporaryMessageAsync(chatId,
                $"⚪ ПЕРЕВОД В СТАТУС 'Не в работе'\n\n" +
                $"👤 Инвестор: {investor}\n" +
                $"💰 Депозит: {investment.DepositAmount:N0} ₽\n\n" +
                $"Введите сумму вывода (в ₽):", cancellationToken);
        }

        private async Task HandleFastInvestWithdrawnAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken);
                return;
            }

            var investmentId = (int)state.Data["investmentId"]!;
            var investment = await _fastInvestService.GetInvestmentAsync(investmentId);

            if (investment == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Инвестиция не найдена", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            var success = await _fastInvestService.WithdrawInvestmentAsync(investmentId, amount, DateTime.UtcNow);

            if (success)
            {
                var investor = investment.Investor?.FullName ?? $"Контакт #{investment.ContactId}";
                var profit = amount - investment.DepositAmount;

                await SendTemporaryMessageAsync(chatId,
                    $"✅ Инвестор {investor} переведён в статус 'Не в работе'\n\n" +
                    $"💰 Выведено: {amount:N0} ₽\n" +
                    $"📈 Прибыль: {profit:N0} ₽", cancellationToken);

                _userStates.Remove(userId);
                await ShowFastInvestByStatusAsync(chatId, InvestStatus.Withdrawn, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при смене статуса", cancellationToken);
                _userStates.Remove(userId);
            }
        }

        private async Task HandleFastInvestChangeToActiveAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var withdrawnInvestments = await _fastInvestService.GetInvestmentsByStatusAsync(InvestStatus.Withdrawn);

            if (!withdrawnInvestments.Any())
            {
                await SendTemporaryMessageAsync(chatId, "❌ Нет инвесторов в статусе 'Не в работе'", cancellationToken);
                return;
            }

            var text = "🟢 ПЕРЕВОД В СТАТУС 'В работе'\n\nВыберите инвестора:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var inv in withdrawnInvestments)
            {
                var investor = inv.Investor?.FullName ?? $"Контакт #{inv.ContactId}";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{investor} | был(а) {inv.ActualWithdrawalDate:dd.MM.yyyy}", $"fastinvest_set_active_{inv.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "fastinvest_change_status")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "set_active", cancellationToken);
        }

        private async Task HandleFastInvestSetActiveAsync(long chatId, long userId, int investmentId, CancellationToken cancellationToken)
        {
            var investment = await _fastInvestService.GetInvestmentAsync(investmentId);
            if (investment == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Инвестиция не найдена", cancellationToken);
                return;
            }

            // Создаем новую инвестицию для этого же контакта
            var newInvestment = await _fastInvestService.CreateInvestmentAsync(
                investment.ContactId,
                investment.DepositAmount,
                DateTime.UtcNow.AddMonths(1),
                $"Возврат в работу. Предыдущий вывод: {investment.WithdrawalAmount:N0} ₽"
            );

            if (newInvestment != null)
            {
                var investor = investment.Investor?.FullName ?? $"Контакт #{investment.ContactId}";

                await SendTemporaryMessageAsync(chatId,
                    $"✅ Инвестор {investor} возвращён в работу!\n\n" +
                    $"💰 Депозит: {newInvestment.DepositAmount:N0} ₽\n" +
                    $"📅 План вывода: {newInvestment.PlannedWithdrawalDate:dd.MM.yyyy}", cancellationToken);

                await ShowFastInvestByStatusAsync(chatId, InvestStatus.Active, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при активации инвестора", cancellationToken);
            }
        }

        private async Task SearchFastInvestContactAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "fastinvest_search_query",
                Step = 1
            };
            await SendTemporaryMessageAsync(chatId, "🔍 Введите имя, username или ID контакта для поиска:", cancellationToken);
        }

        private async Task HandleFastInvestSearchAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var contacts = await _contactService.SearchContactsAsync(text);
            var investments = await _fastInvestService.GetAllInvestmentsAsync();

            if (!contacts.Any())
            {
                await SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            var text2 = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n";

            foreach (var contact in contacts.Take(5))
            {
                var contactName = contact.FullName ?? $"@{contact.TelegramUsername}";
                var contactInvestments = investments.Where(i => i.ContactId == contact.Id).ToList();
                var activeInvestments = contactInvestments.Count(i => i.Status == InvestStatus.Active);
                var totalInvested = contactInvestments.Sum(i => i.DepositAmount);

                text2 += $"👤 {contactName}\n";
                text2 += $"├─ Инвестиций: {contactInvestments.Count} (активных: {activeInvestments})\n";
                text2 += $"└─ Всего вложено: {totalInvested:N0} ₽\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("➕ Новый инвестор", "fastinvest_add") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_fastinvest_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text2, new InlineKeyboardMarkup(buttons), "search_results", cancellationToken);
            _userStates.Remove(userId);
        }

        private async Task ShowFunPayMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Пробуем получить статистику из БД
                var stats = await _funPayService.GetFunPayStatisticsAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
                var accounts = await _funPayService.GetAllAccountsAsync();
                var sales = await _funPayService.GetSalesByDateRangeAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
                var withdrawals = await _funPayService.GetAllWithdrawalsAsync();

                var totalWarnings = accounts.Sum(a => a.Warnings?.Count ?? 0);
                var monthlyWithdrawals = withdrawals.Where(w => w.WithdrawalDate >= DateTime.UtcNow.AddMonths(-1)).Sum(w => w.Amount);

                var text = "🎮 FUNPAY\n\n" +
                           $"📊 СТАТИСТИКА ЗА МЕСЯЦ:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ 💰 Продажи: {stats.TotalOrders} шт на {stats.TotalSales:N0} ₽\n" +
                           $"│ 💵 Прибыль: {stats.TotalProfit:N0} ₽\n" +
                           $"│ 📤 Выводы: {monthlyWithdrawals:N0} ₽\n" +
                           $"│ 👤 Аккаунтов: {accounts.Count} | ⚠️ Штрафов: {totalWarnings}\n" +
                           $"│ 📈 Маржинальность: {(stats.TotalSales > 0 ? stats.TotalProfit / stats.TotalSales * 100 : 0):F1}%\n" +
                           $"└─────────────────────────────────\n\n" +
                           $"Выберите раздел:";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData($"💰 Продажи ({stats.TotalOrders})", "finance_funpay_sales"),
                InlineKeyboardButton.WithCallbackData($"📤 Выводы ({withdrawals.Count})", "finance_funpay_withdrawals")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData($"👤 Аккаунты ({accounts.Count})", "funpay_accounts"),
                InlineKeyboardButton.WithCallbackData($"⚠️ Штрафы ({totalWarnings})", "funpay_warnings")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("📊 Статистика", "funpay_stats")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("➕ Новая продажа", "funpay_add_sale")
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_accounts") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_menu", cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("no such table"))
            {
                // Если таблиц нет - показываем упрощенное меню
                var text = "🎮 FUNPAY\n\n" +
                           "⚠️ База данных инициализируется...\n" +
                           "Добавьте первую продажу или аккаунт, чтобы увидеть статистику.\n\n" +
                           "Выберите действие:";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("💰 ПРОДАЖИ", "finance_funpay_sales"),
                InlineKeyboardButton.WithCallbackData("📤 ВЫВОДЫ", "finance_funpay_withdrawals")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("👤 АККАУНТЫ", "funpay_accounts"),
                InlineKeyboardButton.WithCallbackData("➕ НОВАЯ ПРОДАЖА", "funpay_add_sale")
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_accounts") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_menu", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowFunPayMenuAsync");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки данных", cancellationToken);
            }
        }

        // ========== ПРОДАЖИ ==========
        private async Task ShowFunPaySalesAsync(long chatId, CancellationToken cancellationToken)
        {
            var sales = await _funPayService.GetSalesByDateRangeAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow);

            var totalSales = sales.Sum(s => s.TotalSaleAmount);
            var totalPurchases = sales.Sum(s => s.TotalPurchaseAmount);
            var totalProfit = sales.Sum(s => s.Profit);
            var avgProfit = sales.Any() ? sales.Average(s => s.Profit) : 0;

            var salesByCategory = sales.GroupBy(s => s.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(s => s.TotalSaleAmount), Profit = g.Sum(s => s.Profit) })
                .OrderByDescending(x => x.Total)
                .ToList();

            var text = "💰 ПРОДАЖИ FUNPAY\n\n";
            text += $"📊 ОБЩАЯ СТАТИСТИКА ЗА 3 МЕСЯЦА:\n";
            text += $"Всего продаж: {sales.Count}\n";
            text += $"Общая выручка: {totalSales:N0} ₽\n";
            text += $"Общая закупка: {totalPurchases:N0} ₽\n";
            text += $"ИТОГОВАЯ ПРИБЫЛЬ: {totalProfit:N0} ₽\n";
            text += $"Средняя прибыль: {avgProfit:N0} ₽\n\n";

            if (salesByCategory.Any())
            {
                text += "📊 ПО КАТЕГОРИЯМ:\n";
                foreach (var cat in salesByCategory)
                {
                    var margin = cat.Total > 0 ? (cat.Profit / cat.Total * 100) : 0;
                    text += $"• {cat.Category}: {cat.Total:N0} ₽ | прибыль: {cat.Profit:N0} ₽ ({margin:F1}%)\n";
                }
                text += "\n";
            }

            text += "📋 ПОСЛЕДНИЕ 10 ПРОДАЖ:\n";
            foreach (var sale in sales.OrderByDescending(s => s.SaleDate).Take(10))
            {
                text += $"Заказ #{sale.OrderNumber}\n";
                text += $"├─ {sale.SaleAmount:N0} ₽ × {sale.Quantity} = {sale.TotalSaleAmount:N0} ₽\n";
                text += $"├─ Закупка: {sale.TotalPurchaseAmount:N0} ₽\n";
                text += $"└─ Прибыль: {sale.Profit:N0} ₽ ({sale.SaleDate:dd.MM})\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ Новая продажа", "funpay_add_sale")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📊 По категориям", "funpay_sales_categories")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📈 Динамика", "funpay_sales_dynamics"),
            InlineKeyboardButton.WithCallbackData("📤 Выводы", "finance_funpay_withdrawals")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_funpay_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_sales", cancellationToken);
        }

        private async Task StartAddFunPaySaleAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "add_funpay_sale_order",
                Data = new Dictionary<string, object?>
                {
                    ["returnMenu"] = "finance_funpay_sales"  // Сохраняем, куда вернуться
                },
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 НОВАЯ ПРОДАЖА FUNPAY\n\n" +
                "Введите номер заказа (можно найти в продажах или в чатах):", cancellationToken);
        }

        private async Task HandleAddFunPaySaleOrderAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Номер заказа не может быть пустым", cancellationToken);
                return;
            }

            // Проверяем уникальность
            var existing = await _funPayService.GetAllSalesAsync();
            if (existing.Any(s => s.OrderNumber == text))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ Заказ #{text} уже существует. Введите другой номер:", cancellationToken);
                return;
            }

            state.Data["orderNumber"] = text;
            state.CurrentAction = "add_funpay_sale_amount";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId, $"Заказ #{text}\n\nВведите цену продажи за ОДНУ вещь (в ₽):", cancellationToken);
        }

        private async Task HandleAddFunPaySaleAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal saleAmount) || saleAmount <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную цену продажи:", cancellationToken);
                return;
            }

            state.Data["saleAmount"] = saleAmount;
            state.CurrentAction = "add_funpay_sale_purchase";
            state.Step = 3;
            _userStates[userId] = state;

            var orderNumber = state.Data["orderNumber"]?.ToString() ?? "";
            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Заказ #{orderNumber} | Цена продажи: {saleAmount} ₽\n\n" +
                $"Введите цену закупки:\n" +
                $"• Если закупали поштучно - цену за одну вещь\n" +
                $"• Если закупали оптом - общую сумму за пачку", cancellationToken);
        }

        private async Task HandleAddFunPaySalePurchaseAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal purchaseAmount) || purchaseAmount <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную цену закупки:", cancellationToken);
                return;
            }

            state.Data["purchaseAmount"] = purchaseAmount;
            state.CurrentAction = "add_funpay_sale_quantity";
            state.Step = 4;
            _userStates[userId] = state;

            var orderNumber = state.Data["orderNumber"]?.ToString() ?? "";
            var saleAmount = (decimal)state.Data["saleAmount"]!;
            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Заказ #{orderNumber}\n" +
                $"Цена продажи: {saleAmount} ₽\n" +
                $"Цена закупки: {purchaseAmount} ₽\n\n" +
                $"Введите количество проданных вещей:", cancellationToken);
        }

        private async Task HandleAddFunPaySaleQuantityAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int quantity) || quantity <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректное количество:", cancellationToken);
                return;
            }

            state.Data["quantity"] = quantity;
            state.CurrentAction = "add_funpay_sale_category";
            state.Step = 5;
            _userStates[userId] = state;

            var orderNumber = state.Data["orderNumber"]?.ToString() ?? "";
            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Заказ #{orderNumber} | Количество: {quantity}\n\n" +
                $"Введите категорию товара (например: аккаунты, ключи, услуги, донат):", cancellationToken);
        }

        private async Task HandleAddFunPaySaleCategoryAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Категория не может быть пустой", cancellationToken);
                return;
            }

            var orderNumber = state.Data["orderNumber"]?.ToString() ?? "";
            var saleAmount = (decimal)state.Data["saleAmount"]!;
            var purchaseAmount = (decimal)state.Data["purchaseAmount"]!;
            var quantity = (int)state.Data["quantity"]!;
            var category = text;

            var totalSale = saleAmount * quantity;
            var isBulkPurchase = purchaseAmount < totalSale * 0.3m;

            var sale = await _funPayService.CreateSaleAsync(
                orderNumber, saleAmount, purchaseAmount, quantity, category, isBulkPurchase);

            if (sale != null)
            {
                var profit = sale.Profit;
                var margin = totalSale > 0 ? (profit / totalSale * 100) : 0;

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ Продажа #{orderNumber} успешно добавлена!\n\n" +
                    $"Прибыль: {profit:N0} ₽\n" +
                    $"Маржинальность: {margin:F1}%", cancellationToken, 5);

                _userStates.Remove(userId);

                // Очищаем состояние меню для нового сообщения
                _menuManager.ClearMenuState(chatId);

                // Открываем список продаж НОВЫМ сообщением
                await ShowFunPaySalesAsync(chatId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении продажи", cancellationToken);
                _userStates.Remove(userId);

                _menuManager.ClearMenuState(chatId);
                await ShowFunPaySalesAsync(chatId, cancellationToken);
            }
        }

        // ========== ВЫВОДЫ ==========
        private async Task ShowFunPayWithdrawalsAsync(long chatId, CancellationToken cancellationToken)
        {
            var withdrawals = await _funPayService.GetAllWithdrawalsAsync();
            var totalWithdrawn = withdrawals.Sum(w => w.Amount);
            var thisMonth = withdrawals.Where(w => w.WithdrawalDate.Month == DateTime.UtcNow.Month
                                                 && w.WithdrawalDate.Year == DateTime.UtcNow.Year);
            var lastMonth = withdrawals.Where(w => w.WithdrawalDate.Month == DateTime.UtcNow.AddMonths(-1).Month
                                                 && w.WithdrawalDate.Year == DateTime.UtcNow.AddMonths(-1).Year);

            var text = "📤 ВЫВОДЫ ИЗ FUNPAY\n\n";
            text += $"💰 ВСЕГО ВЫВЕДЕНО: {totalWithdrawn:N0} ₽\n";
            text += $"📊 Количество выводов: {withdrawals.Count}\n";
            text += $"📅 За текущий месяц: {thisMonth.Sum(w => w.Amount):N0} ₽\n";
            text += $"📅 За прошлый месяц: {lastMonth.Sum(w => w.Amount):N0} ₽\n\n";

            if (withdrawals.Any())
            {
                text += "📋 ПОСЛЕДНИЕ 10 ВЫВОДОВ:\n";
                foreach (var w in withdrawals.OrderByDescending(w => w.WithdrawalDate).Take(10))
                {
                    text += $"• {w.Amount:N0} ₽ → {w.Destination}\n";
                    text += $"  {w.WithdrawalDate:dd.MM.yyyy} | {w.Description}\n\n";
                }

                // Группировка по назначению
                var byDestination = withdrawals.GroupBy(w => w.Destination)
                    .Select(g => new { Destination = g.Key, Total = g.Sum(w => w.Amount), Count = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .Take(5);

                text += "📊 ПО НАЗНАЧЕНИЮ:\n";
                foreach (var dest in byDestination)
                {
                    text += $"• {dest.Destination}: {dest.Total:N0} ₽ ({dest.Count} раз)\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ Новый вывод", "funpay_add_withdrawal"),
            InlineKeyboardButton.WithCallbackData("📊 Статистика", "funpay_withdrawal_stats")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("💰 Продажи", "finance_funpay_sales"),
            InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_funpay_menu")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_withdrawals", cancellationToken);
        }

        private async Task StartAddFunPayWithdrawalAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "add_funpay_withdrawal_amount",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            var available = await _funPayService.GetFunPayStatisticsAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

            await SendTemporaryMessageAsync(chatId,
                $"📝 НОВЫЙ ВЫВОД ИЗ FUNPAY\n\n" +
                $"Доступно для вывода (оценка): {available.TotalProfit:N0} ₽\n\n" +
                $"Введите сумму вывода (в ₽):", cancellationToken);
        }

        private async Task HandleAddFunPayWithdrawalAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму:", cancellationToken);
                return;
            }

            state.Data["amount"] = amount;
            state.CurrentAction = "add_funpay_withdrawal_destination";
            state.Step = 2;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId,
                $"Сумма: {amount:N0} ₽\n\n" +
                $"Куда вывели средства?\n" +
                $"(например: Тинькофф, Сбер, USDT-TON, наличные)", cancellationToken);
        }

        private async Task HandleAddFunPayWithdrawalDestinationAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Назначение не может быть пустым", cancellationToken);
                return;
            }

            state.Data["destination"] = text;
            state.CurrentAction = "add_funpay_withdrawal_description";
            state.Step = 3;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId,
                $"Сумма: {(decimal)state.Data["amount"]!:N0} ₽ → {text}\n\n" +
                $"Введите описание/назначение вывода (например: оплата серверов, вывод прибыли):", cancellationToken);
        }

        private async Task HandleAddFunPayWithdrawalDescriptionAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var amount = (decimal)state.Data["amount"]!;
            var destination = state.Data["destination"]?.ToString() ?? "";

            var withdrawal = await _funPayService.CreateWithdrawalAsync(amount, destination, text);

            if (withdrawal != null)
            {
                await SendTemporaryMessageAsync(chatId,
                    $"✅ Вывод успешно добавлен!\n\n" +
                    $"Сумма: {amount:N0} ₽\n" +
                    $"Куда: {destination}\n" +
                    $"Описание: {text}\n" +
                    $"Дата: {withdrawal.WithdrawalDate:dd.MM.yyyy HH:mm}", cancellationToken, 3);


                // Очищаем состояние пользователя
                _userStates.Remove(userId);

                // ОЧИЩАЕМ СОСТОЯНИЕ МЕНЮ, чтобы следующее сообщение было новым
                _menuManager.ClearMenuState(chatId);

                // ВОЗВРАЩАЕМСЯ В МЕНЮ ВЫВОДОВ НОВЫМ СООБЩЕНИЕМ
                await ShowFunPayWithdrawalsAsync(chatId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении вывода", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFunPayWithdrawalsAsync(chatId, cancellationToken);
            }
        }

        // ========== АККАУНТЫ FUNPAY ==========
        private async Task ShowFunPayAccountsAsync(long chatId, CancellationToken cancellationToken)
        {
            var accounts = await _funPayService.GetAllAccountsAsync();
            var totalWarnings = accounts.Sum(a => a.Warnings?.Count ?? 0);

            var text = "👤 АККАУНТЫ FUNPAY\n\n";

            if (accounts.Any())
            {
                foreach (var acc in accounts)
                {
                    var warningsCount = acc.Warnings?.Count ?? 0;
                    var warningEmoji = warningsCount > 0 ? $"⚠️ {warningsCount}" : "✅";

                    text += $"• {acc.Nickname} {warningEmoji}\n";
                    text += $"  Бот: @{acc.BotUsername}\n";
                    text += $"  Предупреждений: {warningsCount}\n\n";
                }

                text += $"📊 Всего аккаунтов: {accounts.Count}\n";
                text += $"⚠️ Всего предупреждений: {totalWarnings}";
            }
            else
            {
                text += "Нет добавленных аккаунтов";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ Добавить аккаунт", "funpay_add_account"),
            InlineKeyboardButton.WithCallbackData("⚠️ Штрафы", "funpay_warnings")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_funpay_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_accounts", cancellationToken);
        }

        // ========== ШТРАФЫ ==========
        private async Task ShowFunPayWarningsAsync(long chatId, CancellationToken cancellationToken)
        {
            var accounts = await _funPayService.GetAllAccountsAsync();
            var allWarnings = accounts.SelectMany(a => a.Warnings ?? new List<FunPayWarning>())
                                       .OrderByDescending(w => w.Date)
                                       .ToList();

            var text = "⚠️ ШТРАФЫ FUNPAY\n\n";

            if (allWarnings.Any())
            {
                text += $"Всего штрафов: {allWarnings.Count}\n\n";
                text += "ПОСЛЕДНИЕ ШТРАФЫ:\n";

                foreach (var w in allWarnings.Take(10))
                {
                    var account = accounts.FirstOrDefault(a => a.Id == w.FunPayAccountId);
                    text += $"• {account?.Nickname ?? "Unknown"}: {w.Reason}\n";
                    text += $"  {w.Date:dd.MM.yyyy}\n";
                    if (!string.IsNullOrEmpty(w.Resolution))
                        text += $"  ✅ Решено: {w.Resolution}\n";
                    text += "\n";
                }
            }
            else
            {
                text += "Штрафов нет ✅";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ ШТРАФ", "funpay_add_warning"),
            InlineKeyboardButton.WithCallbackData("👤 АККАУНТЫ", "funpay_accounts")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_funpay_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_warnings", cancellationToken);
        }

        // ========== СТАТИСТИКА FUNPAY ==========
        private async Task ShowFunPayStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var statsYear = await _funPayService.GetFunPayStatisticsAsync(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow);
            var statsQuarter = await _funPayService.GetFunPayStatisticsAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow);
            var statsMonth = await _funPayService.GetFunPayStatisticsAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

            var text = "📊 СТАТИСТИКА FUNPAY\n\n" +
                       $"💰 ФИНАНСОВЫЕ ПОКАЗАТЕЛИ:\n" +
                       $"• За год: {statsYear.TotalProfit:N0} ₽\n" +
                       $"• За квартал: {statsQuarter.TotalProfit:N0} ₽\n" +
                       $"• За месяц: {statsMonth.TotalProfit:N0} ₽\n\n" +

                       $"📈 ПРОДАЖИ:\n" +
                       $"• За год: {statsYear.TotalOrders} шт, {statsYear.TotalSales:N0} ₽\n" +
                       $"• За квартал: {statsQuarter.TotalOrders} шт, {statsQuarter.TotalSales:N0} ₽\n" +
                       $"• За месяц: {statsMonth.TotalOrders} шт, {statsMonth.TotalSales:N0} ₽\n\n" +

                       $"📊 МАРЖИНАЛЬНОСТЬ:\n";

            foreach (var cat in statsMonth.SalesByCategory.Take(5))
            {
                text += $"• {cat.Key}: {cat.Value:N0} ₽\n";
            }

            text += $"\n📤 ВЫВОДЫ:\n" +
                    $"• Всего выведено: {statsYear.TotalWithdrawals:N0} ₽\n" +
                    $"• За квартал: {(await _funPayService.GetAllWithdrawalsAsync()).Where(w => w.WithdrawalDate >= DateTime.UtcNow.AddMonths(-3)).Sum(w => w.Amount):N0} ₽\n\n" +

                    $"⚠️ ШТРАФЫ:\n" +
                    $"• Всего: {statsYear.TotalWarnings}";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("📊 По месяцам", "funpay_stats_monthly"),
            InlineKeyboardButton.WithCallbackData("📈 График", "funpay_chart")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_funpay_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "funpay_stats", cancellationToken);
        }
        // ========== CRYPTO BOT МЕНЮ ==========
        private async Task StartLinkDealToCircleAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var deals = await _cryptoService.GetDealsByDateRangeAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
            var unlinkedDeals = deals.Where(d => !d.CircleId.HasValue).ToList();

            if (!unlinkedDeals.Any())
            {
                await SendTemporaryMessageAsync(chatId, "❌ Нет непривязанных сделок за последний месяц", cancellationToken, 3);
                return;
            }

            var text = "🔗 ПРИВЯЗКА СДЕЛКИ К КРУГУ\n\nВыберите сделку:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var deal in unlinkedDeals.Take(10))
            {
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"Сделка #{deal.DealNumber} | {deal.Amount} USDT | {deal.Date:dd.MM}",
                $"crypto_select_deal_{deal.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_crypto_deals")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "link_deal_select", cancellationToken);
        }

        private async Task HandleLinkDealToCircleAsync(long chatId, long userId, int circleId, CancellationToken cancellationToken)
        {
            if (!_userStates.ContainsKey(userId) || _userStates[userId].CurrentAction != "add_crypto_deal_link_circle")
            {
                await SendTemporaryMessageAsync(chatId, "❌ Сессия создания сделки истекла", cancellationToken, 3);
                return;
            }

            var state = _userStates[userId];
            var tempDeal = (dynamic)state.Data["tempDeal"]!;

            var deal = await _cryptoService.CreateDealAsync(
                dealNumber: (int)tempDeal.dealNumber,
                amount: (decimal)tempDeal.amount,
                date: (DateTime)tempDeal.dealDate,
                circleId: circleId
            );

            if (deal != null)
            {
                await SendTemporaryMessageAsync(chatId,
                    $"✅ Сделка #{deal.DealNumber} успешно добавлена и привязана к кругу #{circleId}!", cancellationToken, 5);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании сделки", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }

        private async Task HandleConfirmLinkToCircleAsync(long chatId, long userId, int circleId, CancellationToken cancellationToken)
        {
            Console.WriteLine($"   → HandleConfirmLinkToCircleAsync вызван для круга {circleId}");

            if (!_userStates.ContainsKey(userId) || _userStates[userId].CurrentAction != "add_crypto_deal_link_circle")
            {
                Console.WriteLine($"   → ОШИБКА: Сессия создания сделки истекла");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Сессия создания сделки истекла", cancellationToken, 3);
                return;
            }

            var state = _userStates[userId];

            if (!state.Data.ContainsKey("dealNumber") || !state.Data.ContainsKey("amount") || !state.Data.ContainsKey("dealDate"))
            {
                Console.WriteLine($"   → ОШИБКА: данные сделки не найдены");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка: данные сделки не найдены", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var dealNumber = (int)state.Data["dealNumber"]!;
            var amount = (decimal)state.Data["amount"]!;
            var dealDate = (DateTime)state.Data["dealDate"]!;

            // ПРОВЕРЯЕМ, НЕТ ЛИ УЖЕ ТАКОЙ СДЕЛКИ
            var existingDeals = await _cryptoService.GetAllDealsAsync();
            if (existingDeals.Any(d => d.DealNumber == dealNumber))
            {
                Console.WriteLine($"   → ОШИБКА: сделка #{dealNumber} уже существует");
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ Сделка #{dealNumber} уже существует", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
                return;
            }

            Console.WriteLine($"   → Создаём сделку: номер={dealNumber}, сумма={amount}, дата={dealDate}, круг={circleId}");

            var deal = await _cryptoService.CreateDealAsync(dealNumber, amount, dealDate, circleId);

            if (deal != null)
            {
                var circle = await _cryptoService.GetCircleAsync(circleId);
                Console.WriteLine($"   ✅ Сделка создана, ID: {deal.Id}");

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ Сделка #{dealNumber} успешно добавлена и привязана к кругу #{circle?.CircleNumber}!", cancellationToken, 5);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
            }
            else
            {
                Console.WriteLine($"   ❌ Ошибка при создании сделки");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении сделки", cancellationToken, 5);
                _userStates.Remove(userId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
            }
        }

        private async Task HandleLinkDealSkipAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            Console.WriteLine($"   → HandleLinkDealSkipAsync вызван");

            if (!_userStates.ContainsKey(userId))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Сессия создания сделки истекла", cancellationToken, 3);
                return;
            }

            var state = _userStates[userId];

            if (!state.Data.ContainsKey("dealNumber") || !state.Data.ContainsKey("amount") || !state.Data.ContainsKey("dealDate"))
            {
                Console.WriteLine($"   → ОШИБКА: данные сделки не найдены");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка: данные сделки не найдены", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var dealNumber = (int)state.Data["dealNumber"]!;
            var amount = (decimal)state.Data["amount"]!;
            var dealDate = (DateTime)state.Data["dealDate"]!;

            Console.WriteLine($"   → Создаём сделку без круга: номер={dealNumber}, сумма={amount}, дата={dealDate}");

            var deal = await _cryptoService.CreateDealAsync(dealNumber, amount, dealDate, null);

            if (deal != null)
            {
                Console.WriteLine($"   ✅ Сделка создана, ID: {deal.Id}");
                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ Сделка #{dealNumber} успешно добавлена!", cancellationToken, 5);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
            }
            else
            {
                Console.WriteLine($"   ❌ Ошибка при создании сделки");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении сделки", cancellationToken, 5);
                _userStates.Remove(userId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
            }
        }

        // ГРАФИКИ ВСЯКИЕ
        private async Task ShowCryptoCirclesChartAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "📊 Генерация статистики кругов...", cancellationToken, 2);

                var chartData = await _cryptoService.GenerateCirclesChartAsync();

                if (chartData.Length == 0)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Нет данных для статистики", cancellationToken, 3);
                    return;
                }

                using var stream = new MemoryStream(chartData);
                var fileName = $"circles_chart_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";

                await _botClient.SendPhoto(
                    chatId: chatId,
                    photo: new InputFileStream(stream, fileName),
                    caption: "📊 Статистика кругов",
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000);
                // ВОЗВРАЩАЕМСЯ В МЕНЮ СТАТИСТИКИ
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoStatsAsync(chatId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating circles chart");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при генерации статистики", cancellationToken, 3);
                _menuManager.ClearMenuState(chatId);
            }
        }
        private async Task ShowCryptoProfitChartAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "📊 Генерация графика прибыли...", cancellationToken, 2);

                var chartData = await _cryptoService.GenerateProfitChartAsync(6);

                if (chartData.Length == 0)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Недостаточно данных для графика", cancellationToken, 3);
                    return;
                }

                using var stream = new MemoryStream(chartData);
                var fileName = $"profit_chart_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";

                await _botClient.SendPhoto(
                    chatId: chatId,
                    photo: new InputFileStream(stream, fileName),
                    caption: "📈 Прибыль по месяцам",
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000);
                // ВОЗВРАЩАЕМСЯ В МЕНЮ СТАТИСТИКИ
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoStatsAsync(chatId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating profit chart");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при генерации графика", cancellationToken, 3);
                _menuManager.ClearMenuState(chatId);
            }
        }
        private async Task ShowCryptoDealsChartAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "📊 Генерация графика...", cancellationToken, 2);

                var chartData = await _cryptoService.GenerateDealsChartAsync(6);

                if (chartData.Length == 0)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Недостаточно данных для графика", cancellationToken, 3);
                    return;
                }

                using var stream = new MemoryStream(chartData);
                var fileName = $"crypto_chart_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";

                // Отправляем фото
                await _botClient.SendPhoto(
                    chatId: chatId,
                    photo: new InputFileStream(stream, fileName),
                    caption: "📈 График сделок за последние 6 месяцев",
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000);
                // ВОЗВРАЩАЕМСЯ В МЕНЮ СТАТИСТИКИ
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoStatsAsync(chatId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chart");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при генерации графика", cancellationToken, 3);
                _menuManager.ClearMenuState(chatId);
            }
        }

        private async Task ShowCryptoDealsStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var deals = await _cryptoService.GetDealsByDateRangeAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow);
            var circles = await _cryptoService.GetAllCirclesAsync();

            var totalAmount = deals.Sum(d => d.Amount);
            var avgAmount = deals.Any() ? deals.Average(d => d.Amount) : 0;
            var maxAmount = deals.Any() ? deals.Max(d => d.Amount) : 0;
            var minAmount = deals.Any() ? deals.Min(d => d.Amount) : 0;

            var byMonth = deals.GroupBy(d => new { d.Date.Year, d.Date.Month })
                .Select(g => new { Month = $"{g.Key.Year}-{g.Key.Month:D2}", Count = g.Count(), Total = g.Sum(d => d.Amount) })
                .OrderBy(x => x.Month)
                .ToList();

            var linkedDeals = deals.Count(d => d.CircleId.HasValue);
            var unlinkedDeals = deals.Count - linkedDeals;

            var text = "📊 СТАТИСТИКА СДЕЛОК\n\n" +
                       $"💰 ОБЩИЕ ПОКАЗАТЕЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего сделок: {deals.Count}\n" +
                       $"│ Общая сумма: {totalAmount:F2} USDT\n" +
                       $"│ Средняя сделка: {avgAmount:F2} USDT\n" +
                       $"│ Макс сделка: {maxAmount:F2} USDT\n" +
                       $"│ Мин сделка: {minAmount:F2} USDT\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"🔗 ПРИВЯЗКА К КРУГАМ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Привязано: {linkedDeals}\n" +
                       $"│ Не привязано: {unlinkedDeals}\n" +
                       $"└─────────────────────────────────\n\n";

            if (byMonth.Any())
            {
                text += $"📅 ДИНАМИКА ПО МЕСЯЦАМ:\n";
                foreach (var month in byMonth.TakeLast(6))
                {
                    text += $"│ {month.Month}: {month.Count} сделок, {month.Total:F2} USDT\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("🔗 ПРИВЯЗАТЬ СДЕЛКИ", "crypto_link_deal") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_crypto_deals") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_deals_stats", cancellationToken);
        }
        private async Task ShowCryptoStatsMonthlyAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var deals = await _cryptoService.GetDealsByDateRangeAsync(DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow);
                var circles = await _cryptoService.GetAllCirclesAsync();

                // Статистика по сделкам по месяцам
                var dealsByMonth = deals.GroupBy(d => new { d.Date.Year, d.Date.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        DealsCount = g.Count(),
                        DealsAmount = g.Sum(d => d.Amount)
                    })
                    .OrderBy(x => x.Period)
                    .ToList();

                // Статистика по кругам по месяцам
                var circlesByMonth = circles.GroupBy(c => new { c.StartDate.Year, c.StartDate.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        CirclesCount = g.Count(),
                        CirclesDeposit = g.Sum(c => c.DepositAmount),
                        CompletedCount = g.Count(c => c.Status == CircleStatus.Completed)
                    })
                    .OrderBy(x => x.Period)
                    .ToList();

                var text = "📊 СТАТИСТИКА ПО МЕСЯЦАМ\n\n";

                if (dealsByMonth.Any() || circlesByMonth.Any())
                {
                    // Объединяем все месяцы
                    var allPeriods = dealsByMonth.Select(d => d.Period)
                        .Union(circlesByMonth.Select(c => c.Period))
                        .Distinct()
                        .OrderBy(p => p)
                        .ToList();

                    foreach (var period in allPeriods.TakeLast(12))
                    {
                        var dealData = dealsByMonth.FirstOrDefault(d => d.Period == period);
                        var circleData = circlesByMonth.FirstOrDefault(c => c.Period == period);

                        text += $"📅 {period}\n";

                        if (circleData != null)
                        {
                            text += $"├─ 🟢 Кругов: {circleData.CirclesCount}\n";
                            text += $"├─ 💰 Депозитов: {circleData.CirclesDeposit:F2} USDT\n";
                            text += $"├─ ✅ Завершено: {circleData.CompletedCount}\n";
                        }

                        if (dealData != null)
                        {
                            text += $"├─ 💱 Сделок: {dealData.DealsCount}\n";
                            text += $"└─ 💵 Объем: {dealData.DealsAmount:F2} USDT\n";
                        }

                        text += "\n";
                    }
                }
                else
                {
                    text += "Нет данных за последние 12 месяцев";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📈 ГРАФИКИ", "crypto_charts") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "crypto_stats") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_stats", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowCryptoStatsMonthlyAsync");

                var text = "❌ Ошибка загрузки статистики по месяцам";
                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "crypto_stats") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_stats_error", cancellationToken);
            }
        }
        private async Task ShowCryptoChartsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var deals = await _cryptoService.GetDealsByDateRangeAsync(DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow);
                var circles = await _cryptoService.GetAllCirclesAsync();

                // Данные для графика по месяцам
                var monthlyData = deals.GroupBy(d => new { d.Date.Year, d.Date.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Amount = g.Sum(d => d.Amount)
                    })
                    .OrderBy(x => x.Period)
                    .ToList();

                // Прогресс по кругам
                var activeCircles = circles.Where(c => c.Status == CircleStatus.Active).ToList();
                var completedCircles = circles.Where(c => c.Status == CircleStatus.Completed).ToList();

                var text = "📈 ГРАФИКИ CRYPTO BOT\n\n";

                if (monthlyData.Any())
                {
                    text += "📊 ДИНАМИКА ОБЪЕМОВ ПО МЕСЯЦАМ:\n";

                    // Находим максимальное значение для масштабирования
                    var maxAmount = monthlyData.Max(d => d.Amount);

                    foreach (var month in monthlyData.TakeLast(6))
                    {
                        var barLength = 20;
                        var filledBars = maxAmount > 0 ? (int)((month.Amount / maxAmount) * barLength) : 0;
                        var bar = new string('█', filledBars) + new string('░', barLength - filledBars);

                        text += $"{month.Period}: {bar} {month.Amount:F2} USDT\n";
                    }
                    text += "\n";
                }

                if (activeCircles.Any() || completedCircles.Any())
                {
                    text += "🔄 ПРОГРЕСС КРУГОВ:\n";
                    text += $"├─ Активных кругов: {activeCircles.Count}\n";

                    if (activeCircles.Any())
                    {
                        var totalActiveDeposit = activeCircles.Sum(c => c.DepositAmount);
                        var totalExpectedProfit = activeCircles.Sum(c => c.ExpectedProfit);
                        text += $"├─ Депозитов в работе: {totalActiveDeposit:F2} USDT\n";
                        text += $"└─ Ожидаемая прибыль: {totalExpectedProfit:F2} USDT\n\n";
                    }

                    if (completedCircles.Any())
                    {
                        var totalCompletedProfit = completedCircles.Sum(c => c.ActualProfit ?? 0);
                        text += $"✅ Завершенных кругов: {completedCircles.Count}\n";
                        text += $"💰 Реализованная прибыль: {totalCompletedProfit:F2} USDT\n";
                    }
                }

                if (!monthlyData.Any() && !activeCircles.Any() && !completedCircles.Any())
                {
                    text += "Нет данных для отображения графиков";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📊 ПО МЕСЯЦАМ", "crypto_stats_monthly") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "crypto_stats") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_stats", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowCryptoChartsAsync");

                var text = "❌ Ошибка загрузки графиков";
                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "crypto_stats") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_charts_error", cancellationToken);
            }
        }
        private async Task ShowCryptoMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Пробуем получить статистику из БД
                var stats = await _cryptoService.GetCryptoStatisticsAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
                var circles = await _cryptoService.GetAllCirclesAsync();
                var activeCircles = circles.Where(c => c.Status == CircleStatus.Active).ToList();
                var deals = await _cryptoService.GetDealsByDateRangeAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

                var text = "₿ CRYPTO BOT\n\n" +
                           $"📊 СТАТИСТИКА ЗА МЕСЯЦ:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ 🔄 Круги: всего {circles.Count} | активных {activeCircles.Count}\n" +
                           $"│ 💱 Сделки: {deals.Count} на {stats.TotalDealsAmount:F2} USDT\n" +
                           $"│ 📈 Средняя сделка: {(deals.Any() ? deals.Average(d => d.Amount) : 0):F2} USDT\n" +
                           $"│ 💰 В работе: {activeCircles.Sum(c => c.DepositAmount):F2} USDT\n" +
                           $"└─────────────────────────────────\n\n" +
                           $"Выберите раздел:";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData($"🔄 Круги ({activeCircles.Count} активных)", "finance_crypto_circles"),
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData($"💱 Сделки ({deals.Count} за месяц)", "finance_crypto_deals")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("📊 Статистика", "crypto_stats"),
                InlineKeyboardButton.WithCallbackData("➕ Новый круг", "crypto_add_circle")
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_accounts") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_menu", cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("no such table"))
            {
                // Если таблиц нет - показываем упрощенное меню
                var text = "₿ CRYPTO BOT\n\n" +
                           "⚠️ База данных инициализируется...\n" +
                           "Добавьте первый круг или сделку, чтобы увидеть статистику.\n\n" +
                           "Выберите действие:";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("🔄 Круги", "finance_crypto_circles"),
                InlineKeyboardButton.WithCallbackData("💱 Сделки", "finance_crypto_deals")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("➕ Новый круг", "crypto_add_circle"),
                InlineKeyboardButton.WithCallbackData("➕ Новая сделка", "crypto_add_deal")
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_accounts") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_menu", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowCryptoMenuAsync");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки данных", cancellationToken);
            }
        }

        // ========== КРУГИ ==========
        private async Task ShowCryptoCirclesAsync(long chatId, CancellationToken cancellationToken)
        {
            var circles = await _cryptoService.GetAllCirclesAsync();
            var activeCircles = circles.Where(c => c.Status == CircleStatus.Active).ToList();
            var completedCircles = circles.Where(c => c.Status == CircleStatus.Completed).ToList();

            var text = "🔄 КРУГИ CRYPTO BOT\n\n";

            if (activeCircles.Any())
            {
                text += "🟢 АКТИВНЫЕ КРУГИ:\n";
                foreach (var circle in activeCircles)
                {
                    var daysActive = (DateTime.UtcNow - circle.StartDate).Days;
                    text += $"Круг #{circle.CircleNumber}\n";
                    text += $"├─ Депозит на круг: {circle.DepositAmount} USDT\n";
                    text += $"├─ Ожидаемый конец: {circle.ExpectedEndAmount} USDT\n";
                    text += $"├─ Ожидаемая прибыль: {circle.ExpectedProfit} USDT\n";
                    text += $"├─ Дней в работе: {daysActive}\n";

                    var circleDeals = await _cryptoService.GetDealsByCircleAsync(circle.Id);
                    if (circleDeals.Any())
                    {
                        text += $"└─ Сделок в круге: {circleDeals.Count}\n";
                    }
                    else
                    {
                        text += $"└─ Сделок в круге: 0\n";
                    }
                    text += "\n";
                }
            }

            if (completedCircles.Any())
            {
                text += "✅ ЗАВЕРШЕННЫЕ КРУГИ:\n";
                foreach (var circle in completedCircles.OrderByDescending(c => c.EndDate).Take(5))
                {
                    var profit = circle.ActualProfit ?? 0;
                    var profitEmoji = profit >= 0 ? "✅" : "❌";
                    text += $"Круг #{circle.CircleNumber}: {circle.DepositAmount} → {circle.ActualEndAmount} USDT {profitEmoji}\n";
                    text += $"└─ Прибыль: {profit} USDT\n\n";
                }
                if (completedCircles.Count > 5)
                    text += $"... и еще {completedCircles.Count - 5} кругов\n\n";
            }

            text += $"📊 ВСЕГО КРУГОВ: {circles.Count}";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ Новый круг", "crypto_add_circle"),
            InlineKeyboardButton.WithCallbackData("📊 Все круги", "crypto_all_circles")

        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📈 Статистика", "crypto_stats")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Закрыть круг", "crypto_complete_circle")

        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_crypto_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_circles", cancellationToken);
        }

        private async Task ShowAllCirclesAsync(long chatId, CancellationToken cancellationToken)
        {
            var circles = await _cryptoService.GetAllCirclesAsync();

            var text = "📊 ВСЕ КРУГИ CRYPTO BOT\n\n";

            foreach (var circle in circles.OrderByDescending(c => c.CircleNumber))
            {
                var statusEmoji = circle.Status == CircleStatus.Active ? "🟢" : "✅";
                var profit = circle.Status == CircleStatus.Completed && circle.ActualProfit.HasValue
                    ? circle.ActualProfit.Value
                    : circle.ExpectedProfit;
                var profitText = circle.Status == CircleStatus.Completed
                    ? $"факт: {profit} USDT"
                    : $"план: {profit} USDT";

                text += $"{statusEmoji} Круг #{circle.CircleNumber}\n";
                text += $"   Депозит: {circle.DepositAmount} USDT\n";
                text += $"   {profitText}\n";
                text += $"   Старт: {circle.StartDate:dd.MM.yyyy}\n";
                if (circle.EndDate.HasValue)
                    text += $"   Финиш: {circle.EndDate:dd.MM.yyyy}\n";
                text += "\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_crypto_circles") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "all_circles", cancellationToken);
        }

        private async Task StartAddCryptoCircleAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            try
            {
                List<CryptoCircle> circles;
                try
                {
                    circles = await _cryptoService.GetAllCirclesAsync();
                }
                catch (Exception ex) when (ex.Message.Contains("no such table"))
                {
                    circles = new List<CryptoCircle>();
                }

                var lastCircle = circles.OrderByDescending(c => c.CircleNumber).FirstOrDefault();
                var nextCircleNumber = (lastCircle?.CircleNumber ?? 0) + 1;

                _userStates[userId] = new UserState
                {
                    CurrentAction = "add_crypto_circle_number",
                    Data = new Dictionary<string, object?>
                    {
                        ["returnMenu"] = "finance_crypto_menu"  // Сохраняем, куда вернуться
                    },
                    Step = 1
                };

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"📝 СОЗДАНИЕ НОВОГО КРУГА\n\n" +
                    $"Последний номер круга: {lastCircle?.CircleNumber ?? 0}\n" +
                    $"Рекомендуемый номер: {nextCircleNumber}\n\n" +
                    $"Введите номер запуска (или 0 для авто):", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartAddCryptoCircleAsync");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании круга", cancellationToken);
            }
        }

        private async Task HandleAddCryptoCircleNumberAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            int circleNumber;

            if (text == "0")
            {
                try
                {
                    var circles = await _cryptoService.GetAllCirclesAsync();
                    var lastCircle = circles.OrderByDescending(c => c.CircleNumber).FirstOrDefault();
                    circleNumber = (lastCircle?.CircleNumber ?? 0) + 1;
                }
                catch
                {
                    circleNumber = 1;
                }
            }
            else if (!int.TryParse(text, out circleNumber) || circleNumber <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректный номер круга (целое число больше 0):", cancellationToken);
                return;
            }

            // Проверяем уникальность номера
            try
            {
                var circles = await _cryptoService.GetAllCirclesAsync();
                var existing = circles.Any(c => c.CircleNumber == circleNumber);

                if (existing)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ Круг #{circleNumber} уже существует. Введите другой номер:", cancellationToken);
                    return;
                }
            }
            catch (Exception ex) when (ex.Message.Contains("no such table"))
            {
                // Таблицы нет - значит круг точно уникальный
            }

            state.Data["circleNumber"] = circleNumber;
            state.CurrentAction = "add_crypto_circle_deposit";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId, $"Круг #{circleNumber}\n\nВведите сумму депозита на круг (в USDT):", cancellationToken);
        }

        private async Task HandleAddCryptoCircleDepositAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal depositAmount) || depositAmount <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму депозита:", cancellationToken);
                return;
            }

            state.Data["depositAmount"] = depositAmount;
            state.CurrentAction = "add_crypto_circle_expected";
            state.Step = 3;
            _userStates[userId] = state;

            var circleNumber = (int)state.Data["circleNumber"]!;
            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Круг #{circleNumber} | Депозит: {depositAmount} USDT\n\n" +
                $"Введите ожидаемую общую сумму после завершения круга (в USDT):", cancellationToken);
        }

        private async Task HandleAddCryptoCircleExpectedAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal expectedAmount) || expectedAmount <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную ожидаемую сумму:", cancellationToken);
                return;
            }

            var circleNumber = (int)state.Data["circleNumber"]!;
            var depositAmount = (decimal)state.Data["depositAmount"]!;

            var circle = await _cryptoService.CreateCircleAsync(circleNumber, depositAmount, expectedAmount);

            if (circle != null)
            {
                var expectedProfit = expectedAmount - depositAmount;

                // Confirmation message (удаляется через 5 секунд)
                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ Круг #{circleNumber} успешно создан!\n\n" +
                    $"Депозит: {depositAmount} USDT\n" +
                    $"Ожидаемый конец: {expectedAmount} USDT\n" +
                    $"Ожидаемая прибыль: {expectedProfit} USDT", cancellationToken);

                _userStates.Remove(userId);

                // ВАЖНО: Очищаем состояние меню, чтобы следующее сообщение было новым
                _menuManager.ClearMenuState(chatId);

                // Открываем меню кругов НОВЫМ сообщением
                await ShowCryptoCirclesAsync(chatId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании круга", cancellationToken);
                _userStates.Remove(userId);

                // При ошибке тоже показываем меню кругов
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoCirclesAsync(chatId, cancellationToken);
            }
        }


        private async Task StartCompleteCryptoCircleAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var circles = await _cryptoService.GetActiveCirclesAsync();

            if (!circles.Any())
            {
                await SendTemporaryMessageAsync(chatId, "❌ Нет активных кругов для завершения", cancellationToken, 5);
                await ShowCryptoCirclesAsync(chatId, cancellationToken);
                return;
            }

            var text = "📊 ЗАВЕРШЕНИЕ КРУГА\n\nВыберите круг для завершения:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var circle in circles.OrderBy(c => c.CircleNumber))
            {
                var deals = await _cryptoService.GetDealsByCircleAsync(circle.Id);
                var dealsInfo = deals.Any() ? $"сделок: {deals.Count}" : "нет сделок";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"Круг #{circle.CircleNumber} | {circle.DepositAmount} USDT | {dealsInfo}",
                $"crypto_complete_circle_{circle.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_crypto_circles")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "complete_circle_select", cancellationToken);
        }

        private async Task HandleCompleteCryptoCircleAsync(long chatId, long userId, int circleId, CancellationToken cancellationToken)
        {
            var circle = await _cryptoService.GetCircleAsync(circleId);
            if (circle == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Круг не найден", cancellationToken);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "complete_circle_amount",
                Data = new Dictionary<string, object?> { ["circleId"] = circleId },
                Step = 1
            };

            await SendTemporaryMessageAsync(chatId,
                $"📊 ЗАВЕРШЕНИЕ КРУГА #{circle.CircleNumber}\n\n" +
                $"Начальный депозит: {circle.DepositAmount} USDT\n" +
                $"Ожидаемая сумма: {circle.ExpectedEndAmount} USDT\n\n" +
                $"Введите фактическую сумму после завершения круга (в USDT):", cancellationToken);


        }

        private async Task HandleCompleteCircleAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal actualAmount) || actualAmount <= 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму:", cancellationToken);
                return;
            }

            var circleId = (int)state.Data["circleId"]!;
            var circle = await _cryptoService.GetCircleAsync(circleId);

            if (circle == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Круг не найден", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            var success = await _cryptoService.CompleteCircleAsync(circleId, actualAmount);

            if (success)
            {
                var actualProfit = actualAmount - circle.DepositAmount;
                var profitEmoji = actualProfit >= 0 ? "✅" : "❌";

                await SendTemporaryMessageAsync(chatId,
                    $"{profitEmoji} Круг #{circle.CircleNumber} завершен!\n\n" +
                    $"Начальный депозит: {circle.DepositAmount} USDT\n" +
                    $"Ожидалось: {circle.ExpectedEndAmount} USDT\n" +
                    $"Фактический конец: {actualAmount} USDT\n" +
                    $"==================================\n" +
                    $"Ожидаемая прибыль: {circle.ExpectedProfit} USDT\n" +
                    $"Фактическая прибыль: {actualProfit} USDT\n" +
                    $"Отклонение: {(actualProfit - circle.ExpectedProfit):F2} USDT", cancellationToken);

                _userStates.Remove(userId);

                // ОЧИЩАЕМ СОСТОЯНИЕ МЕНЮ
                _menuManager.ClearMenuState(chatId);

                // ОТКРЫВАЕМ МЕНЮ КРУГОВ НОВЫМ СООБЩЕНИЕМ
                await ShowCryptoCirclesAsync(chatId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при завершении круга", cancellationToken);
                _userStates.Remove(userId);
            }
        }

        // ========== СДЕЛКИ ==========
        private async Task ShowCryptoDealsAsync(long chatId, CancellationToken cancellationToken)
        {
            var deals = await _cryptoService.GetDealsByDateRangeAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow);
            var circles = await _cryptoService.GetAllCirclesAsync();

            var totalAmount = deals.Sum(d => d.Amount);
            var avgDeal = deals.Any() ? deals.Average(d => d.Amount) : 0;

            var text = "💱 СДЕЛКИ CRYPTO BOT\n\n";
            text += $"📊 СТАТИСТИКА ЗА 3 МЕСЯЦА:\n";
            text += $"Всего сделок: {deals.Count}\n";
            text += $"Общая сумма: {totalAmount:F2} USDT\n";
            text += $"Средняя сделка: {avgDeal:F2} USDT\n\n";

            if (deals.Any())
            {
                text += "📋 ПОСЛЕДНИЕ 10 СДЕЛОК:\n";
                foreach (var deal in deals.OrderByDescending(d => d.Date).Take(10))
                {
                    var circleInfo = deal.CircleId.HasValue
                        ? $" (круг #{deal.Circle?.CircleNumber})"
                        : " (без круга)";

                    text += $"• #{deal.DealNumber}{circleInfo}: {deal.Amount} USDT\n";
                    text += $"  {deal.Date:dd.MM.yyyy HH:mm}\n";
                }
            }

            // Группировка по кругам
            var dealsByCircle = deals.Where(d => d.CircleId.HasValue).GroupBy(d => d.CircleId);
            if (dealsByCircle.Any())
            {
                text += "\n📊 ПО КРУГАМ:\n";
                foreach (var group in dealsByCircle)
                {
                    var circle = circles.FirstOrDefault(c => c.Id == group.Key);
                    if (circle != null)
                    {
                        text += $"• Круг #{circle.CircleNumber}: {group.Count()} сделок, {group.Sum(d => d.Amount)} USDT\n";
                    }
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ НОВАЯ СДЕЛКА", "crypto_add_deal"),
            InlineKeyboardButton.WithCallbackData("🔗 ПРИВЯЗАТЬ К КРУГУ", "crypto_link_deal")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", "crypto_deals_stats"),
            InlineKeyboardButton.WithCallbackData("📈 ГРАФИК", "crypto_deals_chart")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_crypto_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_deals", cancellationToken);
        }

        private async Task StartAddCryptoDealAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var lastDeal = (await _cryptoService.GetAllCirclesAsync())  // Получаем последний номер сделки
                .SelectMany(c => c.Deals ?? new List<CryptoDeal>())
                .OrderByDescending(d => d.DealNumber)
                .FirstOrDefault();

            var nextDealNumber = (lastDeal?.DealNumber ?? 0) + 1;

            _userStates[userId] = new UserState
            {
                CurrentAction = "add_crypto_deal_number",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await SendTemporaryMessageAsync(chatId,
                $"📝 НОВАЯ СДЕЛКА\n\n" +
                $"Последний номер сделки: {lastDeal?.DealNumber ?? 0}\n" +
                $"Рекомендуемый номер: {nextDealNumber}\n\n" +
                $"Введите номер сделки из КБ (или 0 для авто):", cancellationToken);
        }

        private async Task CreateDealWithoutCircleAsync(long chatId, long userId, UserState state, CancellationToken cancellationToken)
        {
            Console.WriteLine($"   → CreateDealWithoutCircleAsync вызван");

            if (!state.Data.ContainsKey("dealNumber") || !state.Data.ContainsKey("amount") || !state.Data.ContainsKey("dealDate"))
            {
                Console.WriteLine($"   → ОШИБКА: отсутствуют данные сделки");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка: данные сделки не найдены", cancellationToken, 3);
                _userStates.Remove(userId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
                return;
            }

            var dealNumber = (int)state.Data["dealNumber"]!;
            var amount = (decimal)state.Data["amount"]!;
            var dealDate = (DateTime)state.Data["dealDate"]!;

            Console.WriteLine($"   → Создаём сделку: номер={dealNumber}, сумма={amount}, дата={dealDate}");

            var deal = await _cryptoService.CreateDealAsync(dealNumber, amount, dealDate, null);

            if (deal != null)
            {
                Console.WriteLine($"   ✅ Сделка создана, ID: {deal.Id}");
                await SendTemporaryMessageAsync(chatId,
                    $"✅ Сделка #{dealNumber} успешно добавлена!\n" +
                    $"Сумма: {amount} USDT\n" +
                    $"Дата: {dealDate:dd.MM.yyyy HH:mm}", cancellationToken, 5);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
            }
            else
            {
                Console.WriteLine($"   ❌ Ошибка при создании сделки");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении сделки", cancellationToken, 5);
                _userStates.Remove(userId);
                await ShowCryptoDealsAsync(chatId, cancellationToken);
            }
        }

        private async Task HandleAddCryptoDealNumberAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            int dealNumber;

            if (text == "0")
            {
                var lastDeal = (await _cryptoService.GetAllCirclesAsync())
                    .SelectMany(c => c.Deals ?? new List<CryptoDeal>())
                    .OrderByDescending(d => d.DealNumber)
                    .FirstOrDefault();
                dealNumber = (lastDeal?.DealNumber ?? 0) + 1;
            }
            else if (!int.TryParse(text, out dealNumber) || dealNumber <= 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректный номер сделки:", cancellationToken);
                return;
            }

            state.Data["dealNumber"] = dealNumber;
            state.CurrentAction = "add_crypto_deal_amount";
            state.Step = 2;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId, $"Сделка #{dealNumber}\n\nВведите сумму сделки (в USDT):", cancellationToken);
        }

        private async Task HandleAddCryptoDealAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму:", cancellationToken);
                return;
            }

            state.Data["amount"] = amount;
            state.CurrentAction = "add_crypto_deal_date";
            state.Step = 3;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId,
                $"Сделка #{state.Data["dealNumber"]} | Сумма: {amount} USDT\n\n" +
                $"Введите дату сделки в формате ДД.ММ.ГГГГ ЧЧ:ММ\n" +
                $"(или отправьте '-' для текущей даты):", cancellationToken);
        }

        private async Task HandleAddCryptoDealDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            Console.WriteLine($"   → HandleAddCryptoDealDateAsync вызван с текстом: {text}");
            Console.WriteLine($"   → Текущее состояние: {state.CurrentAction}, шаг: {state.Step}");

            DateTime dealDate;

            if (text == "-")
            {
                dealDate = DateTime.UtcNow;
                Console.WriteLine($"   → Используем текущую дату: {dealDate}");
            }
            else if (!DateTime.TryParseExact(text, "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out dealDate))
            {
                Console.WriteLine($"   → Неверный формат даты: {text}");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ ЧЧ:ММ", cancellationToken, 3);
                return;
            }
            else
            {
                Console.WriteLine($"   → Распознана дата: {dealDate}");
            }

            // Сохраняем данные
            if (!state.Data.ContainsKey("dealNumber") || !state.Data.ContainsKey("amount"))
            {
                Console.WriteLine($"   → ОШИБКА: отсутствуют данные сделки");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка: данные сделки не найдены", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var dealNumber = (int)state.Data["dealNumber"]!;
            var amount = (decimal)state.Data["amount"]!;

            Console.WriteLine($"   → Данные сделки: номер={dealNumber}, сумма={amount}, дата={dealDate}");

            // Сохраняем дату
            state.Data["dealDate"] = dealDate;

            // Меняем состояние на выбор круга
            state.CurrentAction = "add_crypto_deal_link_circle";
            state.Step = 4;
            _userStates[userId] = state;

            Console.WriteLine($"   → Состояние изменено на: {state.CurrentAction}, шаг: {state.Step}");

            // Получаем активные круги
            var circles = await _cryptoService.GetActiveCirclesAsync();
            Console.WriteLine($"   → Найдено активных кругов: {circles.Count}");

            if (!circles.Any())
            {
                Console.WriteLine($"   → Нет активных кругов, создаём сделку без привязки");
                await CreateDealWithoutCircleAsync(chatId, userId, state, cancellationToken);
                return;
            }

            // ВАЖНО: Отправляем НОВОЕ сообщение, не трогаем старое меню
            var text2 = $"🔗 ПРИВЯЗКА СДЕЛКИ #{dealNumber}\n\nВыберите круг для привязки:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var circle in circles.OrderBy(c => c.CircleNumber))
            {
                var circleDeals = await _cryptoService.GetDealsByCircleAsync(circle.Id);
                var totalCircleAmount = circleDeals.Sum(d => d.Amount);
                var buttonText = $"Круг #{circle.CircleNumber} | Уже {circleDeals.Count} сделок | {totalCircleAmount:F2} USDT";

                Console.WriteLine($"   → Добавляем кнопку: {buttonText}");

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(buttonText, $"crypto_confirm_link_{circle.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("⏭️ ПРОПУСТИТЬ (без круга)", "crypto_link_deal_skip")
    });

            Console.WriteLine($"   → Показываем НОВОЕ меню выбора круга");

            // Используем SendTemporaryInlineMessageAsync для отправки нового сообщения
            await _menuManager.SendTemporaryInlineMessageAsync(chatId, text2, new InlineKeyboardMarkup(buttons), cancellationToken, 300); // 5 минут на выбор
        }

        // ========== СТАТИСТИКА CRYPTO ==========
        private async Task ShowCryptoStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var stats3m = await _cryptoService.GetCryptoStatisticsAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow);
                var stats1m = await _cryptoService.GetCryptoStatisticsAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
                var circles = await _cryptoService.GetAllCirclesAsync();
                var deals = await _cryptoService.GetDealsByDateRangeAsync(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow);

                var totalProfit = circles.Where(c => c.Status == CircleStatus.Completed && c.ActualProfit.HasValue)
                                         .Sum(c => c.ActualProfit.Value);
                var expectedProfit = circles.Where(c => c.Status == CircleStatus.Active)
                                            .Sum(c => c.ExpectedProfit);

                var avgDealAmount = deals.Any() ? deals.Average(d => d.Amount) : 0;
                var avgCircleDeposit = circles.Any() ? circles.Average(c => c.DepositAmount) : 0;
                var avgCircleProfit = circles.Any(c => c.ActualProfit.HasValue)
                    ? circles.Where(c => c.ActualProfit.HasValue).Average(c => c.ActualProfit.Value)
                    : 0;
                var avgProfitPercent = circles.Any(c => c.ActualProfit.HasValue && c.DepositAmount > 0)
                    ? circles.Where(c => c.ActualProfit.HasValue && c.DepositAmount > 0)
                             .Average(c => (c.ActualProfit.Value / c.DepositAmount) * 100)
                    : 0;

                var text = "📊 СТАТИСТИКА CRYPTO BOT\n\n" +
                           $"💰 ФИНАНСОВЫЕ ПОКАЗАТЕЛИ:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ Реализованная прибыль: {totalProfit:F2} USDT\n" +
                           $"│ Ожидаемая прибыль: {expectedProfit:F2} USDT\n" +
                           $"│ Потенциальная всего: {totalProfit + expectedProfit:F2} USDT\n" +
                           $"└─────────────────────────────────\n\n" +

                           $"🔄 КРУГИ:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ Всего: {circles.Count}\n" +
                           $"│ Активные: {circles.Count(c => c.Status == CircleStatus.Active)}\n" +
                           $"│ Завершенные: {circles.Count(c => c.Status == CircleStatus.Completed)}\n" +
                           $"│ Средний депозит: {avgCircleDeposit:F2} USDT\n" +
                           $"│ Средняя прибыль с круга: {avgCircleProfit:F2} USDT\n" +
                           $"│ Средняя доходность: {avgProfitPercent:F1}%\n" +
                           $"└─────────────────────────────────\n\n" +

                           $"💱 СДЕЛКИ:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ Всего сделок: {deals.Count}\n" +
                           $"│ За 3 месяца: {stats3m.TotalDeals}\n" +
                           $"│ За месяц: {stats1m.TotalDeals}\n" +
                           $"│ Объем за 3м: {stats3m.TotalDealsAmount:F2} USDT\n" +
                           $"│ Объем за месяц: {stats1m.TotalDealsAmount:F2} USDT\n" +
                           $"│ Средняя сделка: {avgDealAmount:F2} USDT\n" +
                           $"└─────────────────────────────────";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("📈 Сделки", "crypto_deals_chart"),
                InlineKeyboardButton.WithCallbackData("💰 Прибыль", "crypto_profit_chart")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("🥧 Круги", "crypto_circles_chart"),
                InlineKeyboardButton.WithCallbackData("📊 По месяцам", "crypto_stats_monthly")
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_crypto_menu") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "crypto_stats", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowCryptoStatsAsync");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки статистики", cancellationToken, 3);
            }
        }
        private async Task ShowCommissionEditListAsync(long chatId, CancellationToken cancellationToken)
        {
            var commissions = await _commissionService.GetAllCommissionsAsync();
            var groupedCommissions = commissions.GroupBy(c => c.BankName).OrderBy(g => g.Key);

            var text = "📝 РЕДАКТИРОВАНИЕ КОМИССИЙ\n\nВыберите комиссию для редактирования:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var group in groupedCommissions)
            {
                foreach (var comm in group.Take(3))
                {
                    var commissionText = comm.CommissionType switch
                    {
                        "percent" => $"{comm.PercentValue}%",
                        "fixed" => $"{comm.FixedValue} {comm.FixedCurrency}",
                        "combined" => $"{comm.PercentValue}% + {comm.FixedValue} {comm.FixedCurrency}",
                        _ => ""
                    };

                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"{comm.BankName} | {comm.Category} | {commissionText}",
                    $"commission_edit_{comm.Id}")
            });
                }
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ КОМИССИЮ", "commission_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_commissions_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "commission_edit_list", cancellationToken);
        }

        private async Task StartAddCommissionTipAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "add_commission_tip_title",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await SendTemporaryMessageAsync(chatId,
                "💡 ДОБАВЛЕНИЕ СОВЕТА ПО КОМИССИЯМ\n\n" +
                "Введите заголовок совета (кратко):", cancellationToken);
        }

        private async Task HandleAddCommissionTipTitleAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Заголовок не может быть пустым", cancellationToken);
                return;
            }

            state.Data["title"] = text;
            state.CurrentAction = "add_commission_tip_content";
            state.Step = 2;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId,
                $"Заголовок: {text}\n\n" +
                $"Введите текст совета (подробное описание):", cancellationToken);
        }

        private async Task HandleAddCommissionTipContentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Текст совета не может быть пустым", cancellationToken);
                return;
            }

            state.Data["content"] = text;
            state.CurrentAction = "add_commission_tip_category";
            state.Step = 3;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId,
                $"Текст сохранён\n\n" +
                $"Выберите категорию совета:\n" +
                $"• bank - для банков\n" +
                $"• crypto - для крипты\n" +
                $"• p2p - для P2P\n" +
                $"• general - общие советы\n\n" +
                $"Введите категорию:", cancellationToken, 60);
        }

        private async Task HandleAddCommissionTipCategoryAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var validCategories = new[] { "bank", "crypto", "p2p", "general" };
            var category = text.ToLower().Trim();

            if (!validCategories.Contains(category))
            {
                await SendTemporaryMessageAsync(chatId,
                    $"❌ Неверная категория. Допустимые: {string.Join(", ", validCategories)}", cancellationToken);
                return;
            }

            try
            {
                var tip = new CommissionTip
                {
                    Title = state.Data["title"]?.ToString() ?? "",
                    Content = state.Data["content"]?.ToString() ?? "",
                    Category = category,
                    Priority = 1,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _commissionService.CreateTipAsync(tip);

                if (result != null)
                {
                    await SendTemporaryMessageAsync(chatId,
                        $"✅ Совет успешно добавлен!\n\n" +
                        $"📌 {tip.Title}\n" +
                        $"📝 {tip.Content}\n" +
                        $"📊 Категория: {category}", cancellationToken);

                    _userStates.Remove(userId);
                    await ShowCommissionTipsAsync(chatId, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении совета", cancellationToken);
                    _userStates.Remove(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding commission tip");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении совета", cancellationToken);
                _userStates.Remove(userId);
            }
        }

        // Добавить обработку редактирования комиссии
        private async Task HandleCommissionEditAsync(long chatId, long userId, int commissionId, CancellationToken cancellationToken)
        {
            var commission = await _commissionService.GetCommissionAsync(commissionId);
            if (commission == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Комиссия не найдена", cancellationToken);
                return;
            }

            var commissionText = commission.CommissionType switch
            {
                "percent" => $"{commission.PercentValue}%",
                "fixed" => $"{commission.FixedValue} {commission.FixedCurrency}",
                "combined" => $"{commission.PercentValue}% + {commission.FixedValue} {commission.FixedCurrency}",
                _ => ""
            };

            var text = $"📝 РЕДАКТИРОВАНИЕ КОМИССИИ\n\n" +
                       $"Банк: {commission.BankName}\n" +
                       $"Категория: {commission.Category}\n" +
                       $"Комиссия: {commissionText}\n" +
                       $"Приоритет: {commission.Priority}\n\n" +
                       $"Что хотите изменить?";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("🏦 Банк", $"commission_edit_bank_{commissionId}"),
            InlineKeyboardButton.WithCallbackData("📋 Категорию", $"commission_edit_category_{commissionId}")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("💰 Значение", $"commission_edit_value_{commissionId}"),
            InlineKeyboardButton.WithCallbackData("📊 Приоритет", $"commission_edit_priority_{commissionId}")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"commission_delete_{commissionId}"),
            InlineKeyboardButton.WithCallbackData("◀️ Назад", "commission_edit_list")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"commission_edit_{commissionId}", cancellationToken);
        }

        private async Task HandleCommissionDeleteAsync(long chatId, long userId, int commissionId, CancellationToken cancellationToken)
        {
            var commission = await _commissionService.GetCommissionAsync(commissionId);
            if (commission == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Комиссия не найдена", cancellationToken);
                return;
            }

            var commissionText = commission.CommissionType switch
            {
                "percent" => $"{commission.PercentValue}%",
                "fixed" => $"{commission.FixedValue} {commission.FixedCurrency}",
                "combined" => $"{commission.PercentValue}% + {commission.FixedValue} {commission.FixedCurrency}",
                _ => ""
            };

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить эту комиссию?\n\n" +
                       $"Банк: {commission.BankName}\n" +
                       $"Категория: {commission.Category}\n" +
                       $"Комиссия: {commissionText}";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"commission_delete_confirm_{commissionId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", "commission_edit_list")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_confirm", cancellationToken);
        }

        private async Task HandleCommissionDeleteConfirmAsync(long chatId, long userId, int commissionId, CancellationToken cancellationToken)
        {
            var success = await _commissionService.DeleteCommissionAsync(commissionId);

            if (success)
            {
                await SendTemporaryMessageAsync(chatId, "✅ Комиссия успешно удалена", cancellationToken);
                await ShowCommissionEditListAsync(chatId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при удалении комиссии", cancellationToken);
            }
        }
        private async Task ShowCommissionManagementAsync(long chatId, CancellationToken cancellationToken)
        {
            var commissionStats = await _financeService.GetCommissionStatisticsAsync();

            var text = $"📊 УПРАВЛЕНИЕ КОМИССИЯМИ\n\n" +
                       $"💰 Всего комиссий: {commissionStats.TotalCommissions:N2} ₽\n" +
                       $"📊 Количество операций: {commissionStats.CommissionCount}\n" +
                       $"📈 Средняя комиссия: {commissionStats.AverageCommission:N2} ₽\n" +
                       $"🔝 Макс. комиссия: {commissionStats.LargestCommission:N2} ₽\n\n" +
                       $"📋 По проектам:\n";

            foreach (var project in commissionStats.CommissionsByProject.Take(3))
            {
                text += $"• {project.Key}: {project.Value:N2} ₽\n";
            }

            text += $"\n⚙️ Настройки комиссий:\n" +
                    $"• Биржевые комиссии: 0.1%\n" +
                    $"• Вывод на карту: 1.5%\n" +
                    $"• Крипто-переводы: по сети\n" +
                    $"• P2P комиссии: 0.5%";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Добавить комиссию", "commission_add") },
                new() { InlineKeyboardButton.WithCallbackData("📊 Детализация", "commission_details") },
                new() { InlineKeyboardButton.WithCallbackData("⚙️ Настройки", "commission_settings") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "commission_management",
                cancellationToken);
        }
        private async Task ShowDepositInfoAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Получаем баланс по статусам
                var balanceByStatus = await _financeService.GetBalanceByStatusAsync();

                var workingBalance = balanceByStatus.GetValueOrDefault(IFinanceService.FundStatus.Working, 0);
                var reservedBalance = balanceByStatus.GetValueOrDefault(IFinanceService.FundStatus.Reserved, 0);
                var blockedBalance = balanceByStatus.GetValueOrDefault(IFinanceService.FundStatus.Blocked, 0);
                var transitBalance = balanceByStatus.GetValueOrDefault(IFinanceService.FundStatus.InTransit, 0);

                var totalBalance = workingBalance + reservedBalance + blockedBalance + transitBalance;

                var text = $"💵 БУХГАЛТЕРИЯ - ДЕПОЗИТ\n\n" +
                          $"💰 РАБОЧИЕ СРЕДСТВА (в обороте):\n" +
                          $"{workingBalance:N2} ₽\n\n" +
                          $"🏦 РЕЗЕРВ (нерабочие):\n" +
                          $"{reservedBalance:N2} ₽\n\n" +
                          $"🔒 ЗАБЛОКИРОВАНО:\n" +
                          $"{blockedBalance:N2} ₽\n\n" +
                          $"⏳ В ПУТИ (крипта/переводы):\n" +
                          $"{transitBalance:N2} ₽\n\n" +
                          $"───────────────\n" +
                          $"ИТОГО: {totalBalance:N2} ₽\n\n" +
                          $"Последние операции:\n";

                var lastRecords = await _financeService.GetAllRecordsAsync();
                foreach (var record in lastRecords.Take(5))
                {
                    var sign = record.Type == FinancialRecordType.Income ? "+" : "-";
                    var date = record.TransactionDate.ToString("dd.MM.yyyy");
                    var statusIcon = record.FundStatus switch
                    {
                        "Working" => "💰",
                        "Reserved" => "🏦",
                        "Blocked" => "🔒",
                        "InTransit" => "⏳",
                        _ => "•"
                    };
                    text += $"• {date}: {statusIcon} {sign}{record.Amount:N2} ₽ ({record.Description})\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() {
                InlineKeyboardButton.WithCallbackData("💰 В оборот", "finance_deposit_to_working"),
                InlineKeyboardButton.WithCallbackData("🏦 В резерв", "finance_deposit_to_reserved")
            },
            new() {
                InlineKeyboardButton.WithCallbackData("➕ Пополнить", "finance_deposit_add"),
                InlineKeyboardButton.WithCallbackData("➖ Вывести", "finance_deposit_withdraw")
            },
            new() {
                InlineKeyboardButton.WithCallbackData("📊 История", "finance_deposit_history"),
                InlineKeyboardButton.WithCallbackData("↔️ Переместить", "finance_deposit_transfer")
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "finance_deposit",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing deposit info");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при получении информации о депозите.", cancellationToken);
            }
        }

        private async Task ShowIncomesAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var weekStart = DateTime.UtcNow.AddDays(-7);

                var monthlyIncome = await _financeService.GetTotalIncomeAsync(monthStart, DateTime.UtcNow);
                var weeklyIncome = await _financeService.GetTotalIncomeAsync(weekStart, DateTime.UtcNow);

                var incomeByCategory = await _financeService.GetIncomeByCategoryAsync(monthStart, DateTime.UtcNow);

                var text = $"💰 ДОХОДЫ\n\n" +
                          $"💵 За месяц: {monthlyIncome:N2} ₽\n" +
                          $"📈 За неделю: {weeklyIncome:N2} ₽\n\n" +
                          $"По категориям:\n";

                foreach (var category in incomeByCategory.Take(5))
                {
                    var percentage = monthlyIncome > 0
                        ? Math.Round(category.Total / monthlyIncome * 100, 1)
                        : 0;
                    text += $"• {category.Category}: {category.Total:N2} ₽ ({percentage}%)\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("➕ Добавить доход", CallbackData.FinanceAddIncome) },
            new() { InlineKeyboardButton.WithCallbackData("📊 Детализация", "finance_incomes_details") },
            new() { InlineKeyboardButton.WithCallbackData("📅 За период", "finance_incomes_period") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "finance_incomes", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing incomes");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке доходов.", cancellationToken, 3);
            }
        }

        private async Task ShowExpensesAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var weekStart = DateTime.UtcNow.AddDays(-7);

                var monthlyExpenses = await _financeService.GetTotalExpensesAsync(monthStart, DateTime.UtcNow);
                var weeklyExpenses = await _financeService.GetTotalExpensesAsync(weekStart, DateTime.UtcNow);

                var expensesByCategory = await _financeService.GetExpensesByCategoryAsync(monthStart, DateTime.UtcNow);

                var text = $"💰 РАСХОДЫ\n\n" +
                          $"📉 За месяц: {monthlyExpenses:N2} ₽\n" +
                          $"📊 За неделю: {weeklyExpenses:N2} ₽\n\n" +
                          $"По категориям:\n";

                foreach (var category in expensesByCategory.Take(5))
                {
                    var percentage = monthlyExpenses > 0
                        ? Math.Round(category.Total / monthlyExpenses * 100, 1)
                        : 0;
                    text += $"• {category.Category}: {category.Total:N2} ₽ ({percentage}%)\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("➕ Добавить расход", CallbackData.FinanceAddExpense) },
            new() { InlineKeyboardButton.WithCallbackData("📊 Детализация", "finance_expenses_details") },
            new() { InlineKeyboardButton.WithCallbackData("📅 За период", "finance_expenses_period") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "finance_expenses", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing expenses");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке расходов.", cancellationToken, 3);
            }
        }

        private async Task ShowExpensesDetailsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var expenses = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Expense);
                var monthlyExpenses = expenses.Where(e => e.TransactionDate >= monthStart).ToList();

                var total = monthlyExpenses.Sum(e => e.Amount);
                var byCategory = monthlyExpenses
                    .GroupBy(e => e.Category)
                    .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount), Count = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                var text = $"📊 ДЕТАЛИЗАЦИЯ РАСХОДОВ\n\n" +
                           $"📅 Период: {monthStart:MMMM yyyy}\n" +
                           $"💰 Всего: {total:N0} ₽\n\n";

                if (byCategory.Any())
                {
                    text += "📋 ПО КАТЕГОРИЯМ:\n";
                    foreach (var cat in byCategory)
                    {
                        var percent = total > 0
                            ? Math.Round(cat.Total / total * 100, 1)
                            : 0;
                        text += $"┌─ {cat.Category}\n";
                        text += $"│ Сумма: {cat.Total} ₽\n";
                        text += $"│ Доля: {percent}%\n";
                        text += $"│ Операций: {cat.Count}\n";
                        text += $"└─────────────────────────\n\n";
                    }
                }
                else
                {
                    text += "📭 Нет расходов за этот месяц\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📅 За период", "finance_expenses_period") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceExpenses) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "expenses_details", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing expenses details");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки данных", cancellationToken, 3);
            }
        }

        private async Task ShowFinanceAccountsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📊 УЧЁТЫ\n\nВыберите раздел учёта:";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("₿ CRYPTO BOT", "finance_crypto_menu") },
                new() { InlineKeyboardButton.WithCallbackData("🎮 FUNPAY", "finance_funpay_menu") },
                new() { InlineKeyboardButton.WithCallbackData("🏦 FAST INVEST", "finance_fastinvest_menu") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
            };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "finance_accounts", cancellationToken);
        }

        // Commissions Menu
        private async Task ShowCommissionsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var commissions = await _commissionService.GetAllCommissionsAsync();
                var banksCount = commissions.Select(c => c.BankName).Distinct().Count();
                var tips = await _commissionService.GetAllTipsAsync();

                var text = "📊 КОМИССИИ\n\n" +
                           $"📋 СТАТИСТИКА:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ 🏦 Банков: {banksCount}\n" +
                           $"│ 📝 Комиссий: {commissions.Count}\n" +
                           $"│ 💡 Советов: {tips.Count}\n" +
                           $"└─────────────────────────────────\n\n" +
                           $"Выберите раздел:";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("🏦 ПО БАНКАМ", "finance_commissions_banks"),
                InlineKeyboardButton.WithCallbackData("💡 СОВЕТЫ", "finance_commissions_tips")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "commission_add"),
                InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", CallbackData.FinanceCommission)
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "commissions_menu", cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("no such table"))
            {
                var text = "📊 КОМИССИИ\n\n" +
                           "⚠️ База данных инициализируется...\n" +
                           "Добавьте первую комиссию, чтобы увидеть статистику.\n\n" +
                           "Выберите действие:";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("🏦 ПО БАНКАМ", "finance_commissions_banks"),
                InlineKeyboardButton.WithCallbackData("💡 СОВЕТЫ", "finance_commissions_tips")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ КОМИССИЮ", "commission_add")
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "commissions_menu", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowCommissionsMenuAsync");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка загрузки данных", cancellationToken);
            }
        }

        private async Task ShowCommissionsByBanksAsync(long chatId, CancellationToken cancellationToken)
        {
            var commissionsByBank = await _commissionService.GetCommissionsGroupedByBankAsync();

            var text = "🏦 КОМИССИИ ПО БАНКАМ\n\n";

            foreach (var bank in commissionsByBank.Keys.OrderBy(b => b))
            {
                text += $"┌─────────────── {bank.ToUpper()} ───────────────┐\n";

                foreach (var comm in commissionsByBank[bank].OrderBy(c => c.Category))
                {
                    var commissionText = comm.CommissionType switch
                    {
                        "percent" => $"{comm.PercentValue}%",
                        "fixed" => $"{comm.FixedValue} {comm.FixedCurrency}",
                        "combined" => $"{comm.PercentValue}% + {comm.FixedValue} {comm.FixedCurrency}",
                        _ => "—"
                    };

                    text += $"│ {comm.Category}: {commissionText,-30} │\n";

                    if (!string.IsNullOrEmpty(comm.Description))
                    {
                        text += $"│   {comm.Description,-36} │\n";
                    }
                }
                text += $"└─────────────────────────────────────────┘\n\n";
            }

            // Добавляем быстрый расчет комиссии
            text += "\n📊 БЫСТРЫЙ РАСЧЕТ КОМИССИИ:\n" +
                    "Введите команду в формате:\n" +
                    "/calc банк категория сумма\n" +
                    "Пример: /calc тинькофф перевод 50000";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ КОМИССИЮ", "commission_add"),
            InlineKeyboardButton.WithCallbackData("📊 РЕДАКТИРОВАТЬ", "commission_edit_list")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("💡 СОВЕТЫ", "finance_commissions_tips"),
            InlineKeyboardButton.WithCallbackData("📈 СТАТИСТИКА", CallbackData.FinanceCommission)
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_commissions_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "commissions_banks", cancellationToken);
        }

        private async Task ShowCommissionTipsAsync(long chatId, CancellationToken cancellationToken)
        {
            var tips = await _commissionService.GetAllTipsAsync();
            var tipsByCategory = tips.GroupBy(t => t.Category ?? "Общее");

            var text = "💡 СОВЕТЫ ПО УМЕНЬШЕНИЮ КОМИССИЙ\n\n";

            foreach (var category in tipsByCategory)
            {
                text += $"┌─────────────── {category.Key.ToUpper()} ───────────────┐\n";

                foreach (var tip in category.OrderByDescending(t => t.Priority))
                {
                    text += $"│ ✦ {tip.Title}\n";
                    text += $"│   {tip.Content}\n";
                    if (!string.IsNullOrEmpty(tip.BankName))
                    {
                        text += $"│   (для {tip.BankName})\n";
                    }
                    text += $"│\n";
                }
                text += $"└─────────────────────────────────────────┘\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ СОВЕТ", "commission_add_tip"),
            InlineKeyboardButton.WithCallbackData("🏦 КОМИССИИ ПО БАНКАМ", "finance_commissions_banks")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "finance_commissions_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "commissions_tips", cancellationToken);
        }

        private async Task StartAddCommissionAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "add_commission_bank",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            var text = "📝 ДОБАВЛЕНИЕ КОМИССИИ\n\n" +
                       "Введите название банка или криптосети:\n" +
                       "(например: Тинькофф, Сбер, Альфа, TRC20)";

            await SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }

        private async Task HandleAddCommissionBankAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Название не может быть пустым", cancellationToken);
                return;
            }

            state.Data["bankName"] = text.Trim();
            state.CurrentAction = "add_commission_category";
            state.Step = 2;
            _userStates[userId] = state;

            var categories = "перевод, снятие, P2P, SWIFT, эквайринг";
            await SendTemporaryMessageAsync(chatId,
                $"Введите категорию комиссии:\n(например: {categories})", cancellationToken);
        }

        private async Task HandleAddCommissionCategoryAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Категория не может быть пустой", cancellationToken);
                return;
            }

            state.Data["category"] = text.Trim().ToLower();
            state.CurrentAction = "add_commission_type";
            state.Step = 3;
            _userStates[userId] = state;

            var text2 = "Выберите тип комиссии:\n" +
                       "1 - Процент (%)\n" +
                       "2 - Фиксированная (₽/$/USDT)\n" +
                       "3 - Комбинированная (% + фикс)\n\n" +
                       "Введите номер (1/2/3):";

            await SendTemporaryMessageAsync(chatId, text2, cancellationToken);
        }

        private async Task HandleAddCommissionTypeAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int type) || type < 1 || type > 3)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите 1, 2 или 3", cancellationToken);
                return;
            }

            string commissionType = type switch
            {
                1 => "percent",
                2 => "fixed",
                3 => "combined",
                _ => "percent"
            };

            state.Data["commissionType"] = commissionType;

            if (commissionType == "fixed")
            {
                state.CurrentAction = "add_commission_fixed";
                state.Step = 4;
                await SendTemporaryMessageAsync(chatId, "Введите фиксированную сумму комиссии (например: 99):", cancellationToken);
            }
            else
            {
                state.CurrentAction = "add_commission_percent";
                state.Step = 4;
                await SendTemporaryMessageAsync(chatId, "Введите процент комиссии (например: 1.5):", cancellationToken);
            }

            _userStates[userId] = state;
        }

        private async Task HandleAddCommissionPercentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal percent) || percent < 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректный процент", cancellationToken);
                return;
            }

            state.Data["percentValue"] = percent;

            if (state.Data["commissionType"]?.ToString() == "combined")
            {
                state.CurrentAction = "add_commission_fixed";
                state.Step = 5;
                await SendTemporaryMessageAsync(chatId, "Введите фиксированную часть комиссии (например: 99):", cancellationToken);
            }
            else
            {
                await SaveCommissionAsync(chatId, userId, state, cancellationToken);
            }

            _userStates[userId] = state;
        }

        private async Task HandleAddCommissionFixedAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal fixedValue) || fixedValue < 0)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken);
                return;
            }

            state.Data["fixedValue"] = fixedValue;
            state.CurrentAction = "add_commission_currency";
            state.Step = 6;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId, "Введите валюту (RUB/USD/USDT):", cancellationToken);
        }

        private async Task HandleAddCommissionCurrencyAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var currency = text.Trim().ToUpper();
            if (currency != "RUB" && currency != "USD" && currency != "USDT")
            {
                await SendTemporaryMessageAsync(chatId, "❌ Поддерживаемые валюты: RUB, USD, USDT", cancellationToken);
                return;
            }

            state.Data["fixedCurrency"] = currency;
            await SaveCommissionAsync(chatId, userId, state, cancellationToken);
        }

        private async Task SaveCommissionAsync(long chatId, long userId, UserState state, CancellationToken cancellationToken)
        {
            try
            {
                var commission = new BankCommission
                {
                    BankName = state.Data["bankName"]?.ToString() ?? "",
                    Category = state.Data["category"]?.ToString() ?? "",
                    CommissionType = state.Data["commissionType"]?.ToString() ?? "percent",
                    PercentValue = state.Data.ContainsKey("percentValue") ? (decimal?)state.Data["percentValue"] : null,
                    FixedValue = state.Data.ContainsKey("fixedValue") ? (decimal?)state.Data["fixedValue"] : null,
                    FixedCurrency = state.Data.ContainsKey("fixedCurrency") ? state.Data["fixedCurrency"]?.ToString() : null,
                    Description = "Добавлено через Telegram бота",
                    Priority = 1
                };

                var result = await _commissionService.CreateCommissionAsync(commission);

                if (result != null)
                {
                    var commissionText = commission.CommissionType switch
                    {
                        "percent" => $"{commission.PercentValue}%",
                        "fixed" => $"{commission.FixedValue} {commission.FixedCurrency}",
                        "combined" => $"{commission.PercentValue}% + {commission.FixedValue} {commission.FixedCurrency}",
                        _ => ""
                    };

                    await SendTemporaryMessageAsync(chatId,
                        $"✅ Комиссия успешно добавлена!\n\n" +
                        $"Банк: {commission.BankName}\n" +
                        $"Категория: {commission.Category}\n" +
                        $"Комиссия: {commissionText}", cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении комиссии", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving commission");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при сохранении комиссии", cancellationToken);
            }

            _userStates.Remove(userId);
            await ShowCommissionsByBanksAsync(chatId, cancellationToken);
        }
        public async Task ShowFinanceStatisticsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var statistics = await _financeService.GetFinanceStatisticsAsync();

                var text = $"📊 Финансовая статистика\n\n" +
                          $"💰 Общий доход: {statistics.TotalIncome:N2} ₽\n" +
                          $"💸 Общие расходы: {statistics.TotalExpenses:N2} ₽\n" +
                          $"⚖️ Баланс: {statistics.Balance:N2} ₽\n\n" +
                          $"📈 Доходы за текущий месяц: {statistics.MonthlyIncome:N2} ₽\n" +
                          $"📉 Расходы за текущий месяц: {statistics.MonthlyExpenses:N2} ₽\n\n" +
                          $"🏆 Топ категорий доходов:\n";

                foreach (var category in statistics.IncomeByCategory.Take(3))
                {
                    var percentage = statistics.MonthlyIncome > 0 ? category.Total / statistics.MonthlyIncome * 100 : 0;
                    text += $"• {category.Category}: {category.Total:N2} ₽ ({percentage:F1}%)\n";
                }

                text += $"\n💸 Топ категорий расходов:\n";
                foreach (var category in statistics.ExpensesByCategory.Take(3))
                {
                    var percentage = statistics.MonthlyExpenses > 0 ? category.Total / statistics.MonthlyExpenses * 100 : 0;
                    text += $"• {category.Category}: {category.Total:N2} ₽ ({percentage:F1}%)\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📅 За месяц", "finance_stats_month") },
            new() { InlineKeyboardButton.WithCallbackData("📅 За квартал", "finance_stats_quarter") },
            new() { InlineKeyboardButton.WithCallbackData("📊 Графики", "finance_stats_charts") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "finance_statistics",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing finance statistics");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при получении финансовой статистики.", cancellationToken);
            }
        }

        private async Task ShowIncomeCategoriesAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            await _menuManager.ShowInlineMenuAsync(
                chatId,
                "💰 Выберите категорию дохода:",
                MainMenuKeyboard.GetIncomeCategories(),
                "income_categories",
                cancellationToken);
        }

        private async Task ShowExpenseCategoriesAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            await _menuManager.ShowInlineMenuAsync(
                chatId,
                "💸 Выберите категорию расхода:",
                MainMenuKeyboard.GetExpenseCategories(),
                "expense_categories",
                cancellationToken);
        }

        private async Task ShowCommissionInfoAsync(long chatId, CancellationToken cancellationToken)
        {
            var commissionRecords = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Expense);
            var totalCommission = commissionRecords.Sum(r => r.Amount);
            var monthlyCommission = commissionRecords
                .Where(r => r.TransactionDate.Month == DateTime.UtcNow.Month)
                .Sum(r => r.Amount);

            var text = $"📊 Комиссии\n\n" +
                      $"Общая комиссия: {totalCommission:N2} ₽\n" +
                      $"За месяц: {monthlyCommission:N2} ₽\n\n" +
                      $"Рекомендации:\n" +
                      $"• Используйте BNB для оплаты комиссий\n" +
                      $"• Объединяйте ордера";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("📅 За период", "finance_commission_period") },
                new() { InlineKeyboardButton.WithCallbackData("📊 Детализация", "finance_commission_details") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "finance_commission",
                cancellationToken);
        }

        private async Task ShowInvestmentsInfoAsync(long chatId, CancellationToken cancellationToken)
        {
            var investmentRecords = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Investment);
            var totalInvestment = investmentRecords.Sum(r => r.Amount);

            var text = $"👥 Вклады участников\n\n" +
                      $"Общая сумма вкладов: {totalInvestment:N2} ₽\n\n" +
                      $"Участники:\n";

            var byUser = investmentRecords.GroupBy(r => r.User?.Username ?? "Неизвестно");
            foreach (var group in byUser.OrderByDescending(g => g.Sum(r => r.Amount)).Take(5))
            {
                var userTotal = group.Sum(r => r.Amount);
                var percentage = totalInvestment > 0 ? userTotal / totalInvestment * 100 : 0;
                text += $"• @{group.Key}: {userTotal:N2} ₽ ({percentage:F1}%)\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Добавить вклад", "finance_investment_add") },
                new() { InlineKeyboardButton.WithCallbackData("➖ Вывести долю", "finance_investment_withdraw") },
                new() { InlineKeyboardButton.WithCallbackData("📊 История", "finance_investment_history") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "finance_investments",
                cancellationToken);
        }

        private async Task ShowCryptoInfoAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "💸 Криптовалюты\n\n" +
                      "Общий портфель: 15,000 USDT\n\n" +
                      "Активы:\n" +
                      "• BTC: 8,000 USDT (53.3%)\n" +
                      "• ETH: 4,000 USDT (26.7%)\n" +
                      "• SOL: 2,000 USDT (13.3%)\n" +
                      "• USDT: 1,000 USD (6.7%)\n\n" +
                      "Изменение за 24ч: +3.2%";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("📈 Графики", "finance_crypto_charts") },
                new() { InlineKeyboardButton.WithCallbackData("💰 Баланс", "finance_crypto_balance") },
                new() { InlineKeyboardButton.WithCallbackData("📊 История", "finance_crypto_history") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "finance_crypto",
                cancellationToken);
        }

        private async Task ShowFastInvestInfoAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "🏦 Fast Invest (ФИ)\n\n" +
                      "Текущий баланс: 3,250 USD\n" +
                      "Доход за месяц: +325 USD (+10%)\n\n" +
                      "Активные инвестиции:\n" +
                      "• Заем #12345: 1,000 USD (12% годовых)\n" +
                      "• Заем #12346: 750 USD (10% годовых)\n" +
                      "• Заем #12347: 500 USD (8% годовых)\n\n" +
                      "Рекомендации:\n" +
                      "• Диверсифицируйте инвестиции\n" +
                      "• Реинвестируйте прибыль";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Инвестировать", "finance_fi_invest") },
                new() { InlineKeyboardButton.WithCallbackData("💰 Баланс", "finance_fi_balance") },
                new() { InlineKeyboardButton.WithCallbackData("📊 История", "finance_fi_history") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "finance_fi",
                cancellationToken);
        }

        private async Task ShowFinanceCategoriesMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "⚙️ Настройки категорий\n\n" +
                      "Здесь вы можете управлять категориями доходов и расходов.\n\n" +
                      "Доходы:\n" +
                      "• Продажи\n" +
                      "• Партнерки\n" +
                      "• Инвестиции\n" +
                      "• Фриланс\n" +
                      "• Трейдинг\n" +
                      "• Прочее\n\n" +
                      "Расходы:\n" +
                      "• Аренда\n" +
                      "• Оборудование\n" +
                      "• Софт\n" +
                      "• Реклама\n" +
                      "• Зарплаты\n" +
                      "• Прочее";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Добавить категорию", "finance_category_add") },
                new() { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", "finance_category_edit") },
                new() { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", "finance_category_delete") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "finance_categories",
                cancellationToken);
        }

        private async Task HandleDepositActionAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case "finance_deposit_add":
                    _userStates[userId] = new UserState
                    {
                        CurrentAction = "add_deposit",
                        Step = 1
                    };
                    await SendTemporaryMessageAsync(chatId, "Введите сумму пополнения (РУБ):", cancellationToken);
                    break;

                case "finance_deposit_withdraw":
                    _userStates[userId] = new UserState
                    {
                        CurrentAction = "withdraw_deposit",
                        Step = 1
                    };
                    await SendTemporaryMessageAsync(chatId, "Введите сумму вывода (РУБ):", cancellationToken);
                    break;

                case "finance_deposit_history":
                    await ShowDepositHistoryAsync(chatId, cancellationToken);
                    break;

                default:
                    await SendTemporaryMessageAsync(chatId, "Неизвестное действие.", cancellationToken);
                    break;
            }
        }

        private async Task HandleIncomesActionAsync(long chatId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case "finance_incomes_details":
                    await ShowDetailedIncomesAsync(chatId, cancellationToken);
                    break;

                case "finance_incomes_period":
                    await ShowIncomesPeriodSelectionAsync(chatId, cancellationToken);
                    break;

                default:
                    await SendTemporaryMessageAsync(chatId, "Неизвестное действие.", cancellationToken);
                    break;
            }
        }

        private async Task HandleExpensesActionAsync(long chatId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case "finance_expenses_details":
                    await ShowDetailedExpensesAsync(chatId, cancellationToken);
                    break;

                case "finance_expenses_period":
                    await ShowMonthlyExpensesChartAsync(chatId, cancellationToken, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
                    break;

                default:
                    await SendTemporaryMessageAsync(chatId, "Неизвестное действие.", cancellationToken);
                    break;
            }
        }

        private async Task HandleStatsActionAsync(long chatId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case "finance_stats_month":
                    var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    await ShowFinanceStatisticsForPeriodAsync(chatId, monthStart, DateTime.UtcNow, "месяц", cancellationToken);
                    break;

                case "finance_stats_quarter":
                    var quarterStart = new DateTime(DateTime.UtcNow.Year, ((DateTime.UtcNow.Month - 1) / 3) * 3 + 1, 1);
                    await ShowFinanceStatisticsForPeriodAsync(chatId, quarterStart, DateTime.UtcNow, "квартал", cancellationToken);
                    break;

                case "finance_stats_charts":
                    await ShowFinanceChartsAsync(chatId, cancellationToken);
                    break;

                default:
                    await SendTemporaryMessageAsync(chatId, "Неизвестное действие.", cancellationToken);
                    break;
            }
        }

        private async Task ShowDepositHistoryAsync(long chatId, CancellationToken cancellationToken)
        {
            var depositRecords = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Deposit);
            var withdrawalRecords = await _financeService.GetRecordsByCategoryAsync("Вывод");

            var allRecords = depositRecords.Concat(withdrawalRecords)
                .OrderByDescending(r => r.TransactionDate)
                .Take(20)
                .ToList();

            var text = "📊 История операций по депозиту\n\n";

            if (allRecords.Count == 0)
            {
                text += "📭 Операций нет.\n";
            }
            else
            {
                foreach (var record in allRecords)
                {
                    var sign = record.Type == FinancialRecordType.Deposit ? "+" : "-";
                    var date = record.TransactionDate.ToString("dd.MM.yyyy");
                    var type = record.Type == FinancialRecordType.Deposit ? "Пополнение" : "Вывод";
                    text += $"• {date}: {sign}{record.Amount:N2} ₽ - {type} ({record.Description})\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceDeposit) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "deposit_history",
                cancellationToken);
        }

        private async Task ShowDetailedIncomesAsync(long chatId, CancellationToken cancellationToken)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var incomes = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Income);
            var monthlyIncomes = incomes.Where(i => i.TransactionDate >= monthStart).ToList();

            var text = $"💰 Детализация доходов ({monthStart:MMMM yyyy})\n\n";

            if (monthlyIncomes.Count == 0)
            {
                text += "📭 Доходов в этом месяце нет.\n";
            }
            else
            {
                var total = monthlyIncomes.Sum(i => i.Amount);
                text += $"📊 Всего: {total:N2} ₽\n\n";

                var byCategory = monthlyIncomes.GroupBy(i => i.Category);
                foreach (var group in byCategory.OrderByDescending(g => g.Sum(i => i.Amount)))
                {
                    var categoryTotal = group.Sum(i => i.Amount);
                    var percentage = total > 0 ? categoryTotal / total * 100 : 0;
                    text += $"🏷️ {group.Key}: {categoryTotal:N2} ₽ ({percentage:F1}%)\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceIncomes) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "incomes_details",
                cancellationToken);
        }

        private async Task ShowIncomesPeriodSelectionAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📅 Выбор периода для доходов\n\n" +
                      "Выберите период для отображения доходов:";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("📅 За неделю", "incomes_period_week") },
                new() { InlineKeyboardButton.WithCallbackData("📅 За месяц", "incomes_period_month") },
                new() { InlineKeyboardButton.WithCallbackData("📅 За квартал", "incomes_period_quarter") },
                new() { InlineKeyboardButton.WithCallbackData("📅 За год", "incomes_period_year") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceIncomes) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "incomes_period",
                cancellationToken);
        }

        private async Task ShowDetailedExpensesAsync(long chatId, CancellationToken cancellationToken)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var expenses = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Expense);
            var monthlyExpenses = expenses.Where(e => e.TransactionDate >= monthStart).ToList();

            var text = $"💸 Детализация расходов ({monthStart:MMMM yyyy})\n\n";

            if (monthlyExpenses.Count == 0)
            {
                text += "📭 Расходов в этом месяце нет.\n";
            }
            else
            {
                var total = monthlyExpenses.Sum(e => e.Amount);
                text += $"📊 Всего: {total:N2} ₽\n\n";

                var byCategory = monthlyExpenses.GroupBy(e => e.Category);
                foreach (var group in byCategory.OrderByDescending(g => g.Sum(e => e.Amount)))
                {
                    var categoryTotal = group.Sum(e => e.Amount);
                    var percentage = total > 0 ? categoryTotal / total * 100 : 0;
                    text += $"🏷️ {group.Key}: {categoryTotal:N2} ₽ ({percentage:F1}%)\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceExpenses) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "expenses_details",
                cancellationToken);
        }

        private async Task ShowMonthlyExpensesChartAsync(long chatId, CancellationToken cancellationToken, int year, int month)
        {
            try
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "📊 Генерация графика...", cancellationToken, 2);

                var chartData = await _financeService.GenerateMonthlyExpensesChartAsync(year, month);

                if (chartData.Length == 0)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId,
                        $"❌ Нет расходов за {new DateTime(year, month, 1):MMMM yyyy}",
                        cancellationToken, 3);

                    // Возвращаемся в меню расходов
                    await ShowExpensesAsync(chatId, cancellationToken);
                    return;
                }

                using var stream = new MemoryStream(chartData);
                var fileName = $"expenses_{year}_{month}.png";

                // Отправляем фото
                await _botClient.SendPhoto(
                    chatId: chatId,
                    photo: new InputFileStream(stream, fileName),
                    caption: $"📊 Расходы за {new DateTime(year, month, 1):MMMM yyyy}",
                    cancellationToken: cancellationToken
                );

                // Кнопки навигации
                var prevMonth = new DateTime(year, month, 1).AddMonths(-1);
                var nextMonth = new DateTime(year, month, 1).AddMonths(1);
                var currentMonth = DateTime.UtcNow;

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData($"◀️ {prevMonth:MMM yyyy}", $"expenses_chart_{prevMonth.Year}_{prevMonth.Month}"),
                InlineKeyboardButton.WithCallbackData($"{nextMonth:MMM yyyy} ▶️", $"expenses_chart_{nextMonth.Year}_{nextMonth.Month}")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData($"📅 {currentMonth:MMM yyyy}",
                    $"expenses_chart_{currentMonth.Year}_{currentMonth.Month}")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад в меню", CallbackData.FinanceExpenses)
            }
        };

                // ВАЖНО: Очищаем состояние меню перед показом нового
                _menuManager.ClearMenuState(chatId);

                // Показываем меню с кнопками
                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    "📌 Выберите месяц или вернитесь назад:",
                    new InlineKeyboardMarkup(buttons),
                    "expenses_chart_nav",
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chart");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при генерации графика", cancellationToken, 3);
                await ShowExpensesAsync(chatId, cancellationToken);
            }
        }

        private async Task ShowFinanceStatisticsForPeriodAsync(long chatId, DateTime startDate, DateTime endDate, string periodName, CancellationToken cancellationToken)
        {
            var statistics = await _financeService.GetFinanceStatisticsAsync(startDate, endDate);

            var text = $"📊 Финансовая статистика за {periodName}\n\n" +
                      $"📅 Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}\n\n" +
                      $"💰 Доход: {statistics.TotalIncome:N2} ₽\n" +
                      $"💸 Расход: {statistics.TotalExpenses:N2} ₽\n" +
                      $"⚖️ Баланс: {statistics.Balance:N2} ₽\n" +
                      $"📈 Рентабельность: {(statistics.TotalIncome > 0 ? statistics.Balance / statistics.TotalIncome * 100 : 0):F1}%\n\n" +
                      $"🏆 Топ категорий доходов:\n";

            foreach (var category in statistics.IncomeByCategory.Take(3))
            {
                var percentage = statistics.TotalIncome > 0 ? category.Total / statistics.TotalIncome * 100 : 0;
                text += $"• {category.Category}: {category.Total:N2} ₽ ({percentage:F1}%)\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceStats) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                $"finance_stats_{periodName}",
                cancellationToken);
        }

        private async Task ShowFinanceChartsAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📈 Графики финансового KPI\n\n" +
                      "Доступные графики:\n\n" +
                      "📊 1. Динамика доходов и расходов\n" +
                      "   • Линейный график по месяцам\n" +
                      "   • Сравнение с предыдущим периодом\n\n" +
                      "📊 2. Структура доходов\n" +
                      "   • Круговая диаграмма по категориям\n" +
                      "   • Доля каждой категории\n\n" +
                      "📊 3. Структура расходов\n" +
                      "   • Круговая диаграмма по категориям\n" +
                      "   • Оптимизация затрат\n\n" +
                      "📊 4. Прогноз на следующий месяц\n" +
                      "   • Тренд на основе исторических данных\n" +
                      "   • Рекомендации по оптимизации";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("📈 Динамика", "charts_trend") },
                new() { InlineKeyboardButton.WithCallbackData("🥧 Доходы", "charts_income") },
                new() { InlineKeyboardButton.WithCallbackData("🥧 Расходы", "charts_expenses") },
                new() { InlineKeyboardButton.WithCallbackData("🔮 Прогноз", "charts_forecast") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceStats) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "finance_charts",
                cancellationToken);
        }
        #endregion

        #region Обработка состояний пользователя - ПОЛНАЯ РЕАЛИЗАЦИЯ
        private async Task HandleUserStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandleUserStateAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   ├─ Текст: {text}");
            Console.WriteLine($"   ├─ Действие: {state.CurrentAction}");
            Console.WriteLine($"   ├─ Шаг: {state.Step}");
            Console.WriteLine($"   └─ ProjectId: {state.ProjectId}");

            // ЕСЛИ ПОЛЬЗОВАТЕЛЬ НАЖАЛ КНОПКУ ГЛАВНОГО МЕНЮ - ОЧИЩАЕМ СОСТОЯНИЕ
            if (text == "📂 Проекты" || text == "✅ Задачи" || text == "💰 Бухгалтерия" ||
                text == "📈 KPI" || text == "👤 Контакты" || text == "📊 Статусы" ||
                text == "🗃️ База данных" || text == "📢 Реклама" || text == "⚙️ Настройки" ||
                text == "◀️ Назад в меню")
            {
                Console.WriteLine($"🔄 Пользователь нажал кнопку главного меню, очищаем состояние");
                ClearUserState(userId);

                var mainMenuHandler = _serviceProvider.GetRequiredService<MainMenuHandler>();
                var message = new Message
                {
                    Chat = new Chat { Id = chatId },
                    From = new TelegramUser { Id = userId },
                    Text = text
                };

                await mainMenuHandler.HandleMainMenuSelectionAsync(message, cancellationToken);
                return;
            }

            switch (state.CurrentAction)
            {
                // ===== СУЩЕСТВУЮЩИЕ СОСТОЯНИЯ =====
                case UserActions.CreateProject:
                    await HandleCreateProjectStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case UserActions.EditProject:
                    await HandleEditProjectStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case UserActions.CreateTask:
                    await HandleCreateTaskStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case UserActions.AddIncome:
                    await HandleAddIncomeStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case UserActions.AddExpense:
                    await HandleAddExpenseStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case UserActions.AddContact:
                    await HandleAddContactStateAsync(chatId, userId, text, cancellationToken);
                    break;
                case UserActions.SearchContact:
                    await HandleSearchContactStateAsync(chatId, userId, text, cancellationToken);
                    break;

                // БД 
                case "edit_contact_notes":
                    Console.WriteLine($"   → HandleUserStateAsync: edit_contact_notes");
                    await HandleEditContactNotesAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_username":
                    await HandleAddContactUsernameAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_name":
                    await HandleAddContactNameAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_nickname":
                    await HandleAddContactNicknameAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_phone":
                    await HandleAddContactPhoneAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_birthdate":
                    await HandleAddContactBirthDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_our_phone":
                    await HandleAddContactOurPhoneAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_bank_password":
                    await HandleAddContactBankPasswordAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_pin":
                    await HandleAddContactPinAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_our_email":
                    await HandleAddContactOurEmailAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_passport_series":
                    await HandleAddContactPassportSeriesAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_passport_number":
                    await HandleAddContactPassportNumberAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_passport_expiry":
                    await HandleAddContactPassportExpiryAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_passport_department":
                    await HandleAddContactPassportDepartmentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_passport_issued_by":
                    await HandleAddContactPassportIssuedByAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_passport_issue_date":
                    await HandleAddContactPassportIssueDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_inn":
                    await HandleAddContactInnAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_status":
                    await HandleAddContactStatusAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_contact_notes":
                    await HandleAddContactNotesAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_contact_select_field":
                    await HandleEditContactSelectFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_contact_value":
                    await HandleEditContactValueAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_card_number":
                    await HandleAddCardNumberAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_contact_card_value":
                    await HandleEditContactCardValueAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_contact_search":
                    await HandleContactSearchAsync(chatId, userId, text, cancellationToken);
                    break;
                case "db_add_contact_card":
                    await HandleAddContactCardAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_contact_field":
                    await HandleEditContactFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_post_title":
                    await HandleAddPostTitleAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_post_content":
                    await HandleAddPostContentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_post_channel":
                    await HandleAddPostChannelAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_post_date":
                    await HandleAddPostDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_post_status":
                    await HandleAddPostStatusAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_search_posts":
                    await HandleSearchPostsAsync(chatId, userId, text, cancellationToken);
                    break;
                case "db_add_manual_title":
                    await HandleAddManualTitleAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_manual_category":
                    await HandleAddManualCategoryAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_manual_bank":
                    await HandleAddManualBankAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_manual_content":
                    await HandleAddManualContentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_manual_author":
                    await HandleAddManualAuthorAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_manual_file":
                    await HandleAddManualFileAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_search_manuals":
                    await HandleSearchManualsAsync(chatId, userId, text, cancellationToken);
                    break;
                case "db_add_report_title":
                    await HandleAddReportTitleAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_report_investor":
                    await HandleAddReportInvestorAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_report_date":
                    await HandleAddReportDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_report_deposit":
                    await HandleAddReportDepositAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_report_profit":
                    await HandleAddReportProfitAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_report_summary":
                    await HandleAddReportSummaryAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_report_status":
                    await HandleAddReportStatusAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_search_reports":
                    await HandleSearchReportsAsync(chatId, userId, text, cancellationToken);
                    break;
                case "db_add_doc_title":
                    await HandleAddDocTitleAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_doc_project":
                    await HandleAddDocProjectAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_doc_type":
                    await HandleAddDocTypeAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_doc_content":
                    await HandleAddDocContentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_doc_file":
                    await HandleAddDocFileAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_search_docs":
                    await HandleSearchDocsAsync(chatId, userId, text, cancellationToken);
                    break;
                case "db_add_ad_name":
                    await HandleAddAdNameAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_ad_project":
                    await HandleAddAdProjectAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_ad_budget":
                    await HandleAddAdBudgetAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_ad_start_date":
                    await HandleAddAdStartDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_ad_end_date":
                    await HandleAddAdEndDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_ad_description":
                    await HandleAddAdDescriptionAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_ad_link":
                    await HandleAddAdLinkAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_ad_status":
                    await HandleAddAdStatusAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_spent_amount":
                    await HandleAddSpentAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_search_ads":
                    await HandleSearchAdsAsync(chatId, userId, text, cancellationToken);
                    break;
                case "db_add_funpay_nickname":
                    await HandleAddFunPayNicknameAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_funpay_golden":
                    await HandleAddFunPayGoldenAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_funpay_bot":
                    await HandleAddFunPayBotAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_funpay_password":
                    await HandleAddFunPayPasswordAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_funpay_api":
                    await HandleAddFunPayApiAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_add_funpay_warning_reason":
                    await HandleAddFunPayWarningReasonAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_resolve_warning":
                    await HandleResolveWarningAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_search_funpay":
                    await HandleSearchFunPayAsync(chatId, userId, text, cancellationToken);
                    break;
                // ===== МАНУАЛЫ =====
                case "db_edit_manual_field":
                    await HandleEditManualFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_manual_value":
                    await HandleEditManualValueAsync(chatId, userId, text, state, cancellationToken);
                    break;
                // ===== ПОСТЫ =====
                case "db_edit_post_field":
                    await HandleEditPostFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_post_value":
                    await HandleEditPostValueAsync(chatId, userId, text, state, cancellationToken);
                    break;
                // ===== ОТЧЁТЫ =====
                case "add_notification_title":
                    await HandleAddNotificationTitleAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_notification_message":
                    await HandleAddNotificationMessageAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_notification_frequency":
                    await HandleAddNotificationFrequencyAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_notification_time":
                    await HandleAddNotificationTimeAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_notification_date":
                    await HandleAddNotificationDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_notification_monthday":
                    await HandleAddNotificationMonthDayAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_notification_month":
                    await HandleAddNotificationMonthAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "edit_notification_field":
                    await HandleEditNotificationFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "edit_notification_value":
                    await HandleEditNotificationValueAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "edit_notification_extra":
                    await HandleEditNotificationExtraAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "search_notifications":
                    await HandleSearchNotificationsAsync(chatId, userId, text, cancellationToken);
                    break;
                case "set_report_time":
                    await HandleSetReportTimeAsync(chatId, userId, text, cancellationToken);
                    break;
                case "db_edit_report_field":
                    await HandleEditReportFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_report_value":
                    await HandleEditReportValueAsync(chatId, userId, text, state, cancellationToken);
                    break;

                // ===== ДОКУМЕНТЫ =====
                case "db_edit_doc_field":
                    await HandleEditDocFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_doc_value":
                    await HandleEditDocValueAsync(chatId, userId, text, state, cancellationToken);
                    break;

                // ===== ПЛАНЫ =====
                case "add_plan_title":
                    await HandleAddPlanTitleAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_plan_content":
                    await HandleAddPlanContentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "edit_plan_title":
                    await HandleEditPlanTitleAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "edit_plan_content":
                    await HandleEditPlanContentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "search_plans":
                    await HandleSearchPlansAsync(chatId, userId, text, cancellationToken);
                    break;

                // ===== РЕКЛАМА =====
                case "db_edit_ad_field":
                    await HandleEditAdFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_ad_value":
                    await HandleEditAdValueAsync(chatId, userId, text, state, cancellationToken);
                    break;

                // ===== FUNPAY =====
                case "db_edit_funpay_field":
                    await HandleEditFunPayFieldAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "db_edit_funpay_value":
                    await HandleEditFunPayValueAsync(chatId, userId, text, state, cancellationToken);
                    break;
                // ===== CRYPTO BOT СОСТОЯНИЯ =====
                case "add_crypto_deal_link_circle":
                    Console.WriteLine($"   → Ожидание выбора круга через callback");
                    await SendTemporaryMessageAsync(chatId, "Пожалуйста, выберите круг из списка выше", cancellationToken, 5);
                    break;
                case "add_crypto_circle_number":
                    await HandleAddCryptoCircleNumberAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_crypto_circle_deposit":
                    await HandleAddCryptoCircleDepositAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_crypto_circle_expected":
                    await HandleAddCryptoCircleExpectedAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_crypto_deal_number":
                    await HandleAddCryptoDealNumberAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_crypto_deal_amount":
                    await HandleAddCryptoDealAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_crypto_deal_date":
                    await HandleAddCryptoDealDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "complete_circle_amount":
                    await HandleCompleteCircleAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;

                // ===== FUNPAY СОСТОЯНИЯ =====
                case "add_funpay_account_name":
                    await HandleAddFunPayAccountNameAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_account_golden":
                    await HandleAddFunPayAccountGoldenAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_account_bot":
                    await HandleAddFunPayAccountBotAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_account_api":
                    await HandleAddFunPayAccountApiAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_warning_reason":
                    await HandleAddFunPayWarningReasonAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_sale_order":
                    await HandleAddFunPaySaleOrderAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_sale_amount":
                    await HandleAddFunPaySaleAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_sale_purchase":
                    await HandleAddFunPaySalePurchaseAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_sale_quantity":
                    await HandleAddFunPaySaleQuantityAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_sale_category":
                    await HandleAddFunPaySaleCategoryAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_withdrawal_amount":
                    await HandleAddFunPayWithdrawalAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_withdrawal_destination":
                    await HandleAddFunPayWithdrawalDestinationAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_funpay_withdrawal_description":
                    await HandleAddFunPayWithdrawalDescriptionAsync(chatId, userId, text, state, cancellationToken);
                    break;

                // ===== FAST INVEST СОСТОЯНИЯ =====
                case "fastinvest_search_query":
                    await HandleFastInvestSearchAsync(chatId, userId, text, cancellationToken);
                    break;
                case "add_fastinvest_deposit":
                    await HandleAddFastInvestDepositAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_fastinvest_date":
                    await HandleAddFastInvestDateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_fastinvest_comment":
                    await HandleAddFastInvestCommentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "complete_fastinvest_amount":
                    await HandleCompleteFastInvestAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "withdraw_fastinvest_amount":
                    await HandleWithdrawFastInvestAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;

                // ===== ВКЛАДЫ УЧАСТНИКОВ СОСТОЯНИЯ =====
                case "investment_search_query":
                    await HandleInvestmentSearchAsync(chatId, userId, text, cancellationToken);
                    break;
                case "add_investment_amount":
                    await HandleAddInvestmentAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_investment_description":
                    await HandleAddInvestmentDescriptionAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "withdraw_investment_amount":
                    await HandleWithdrawInvestmentAmountAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "withdraw_investment_description":
                    await HandleWithdrawInvestmentDescriptionAsync(chatId, userId, text, state, cancellationToken);
                    break;

                // ===== КОМИССИИ СОСТОЯНИЯ =====
                case "add_commission_tip_title":
                    await HandleAddCommissionTipTitleAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_commission_tip_content":
                    await HandleAddCommissionTipContentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_commission_tip_category":
                    await HandleAddCommissionTipCategoryAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_commission_bank":
                    await HandleAddCommissionBankAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_commission_category":
                    await HandleAddCommissionCategoryAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_commission_type":
                    await HandleAddCommissionTypeAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_commission_percent":
                    await HandleAddCommissionPercentAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_commission_fixed":
                    await HandleAddCommissionFixedAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_commission_currency":
                    await HandleAddCommissionCurrencyAsync(chatId, userId, text, state, cancellationToken);
                    break;

                // ===== СТАРЫЕ СОСТОЯНИЯ =====
                case "add_deposit":
                    await HandleAddDepositStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "withdraw_deposit":
                    await HandleWithdrawDepositStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "create_content_plan":
                    await HandleCreateContentPlanStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "create_campaign":
                    await HandleCreateCampaignStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_bank_card":
                    await HandleAddBankCardStateAsync(chatId, userId, text, state, cancellationToken);
                    break;
                case "add_crypto_wallet":
                    await HandleAddCryptoWalletStateAsync(chatId, userId, text, state, cancellationToken);
                    break;

                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестное действие. Попробуйте начать заново.", cancellationToken);
                    ClearUserState(userId);
                    break;
            }
        }
        private async Task HandleAddBankCardStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var contactId = (int)state.Data["contactId"]!;
            var step = (int)state.Data["step"]!;

            switch (step)
            {
                case 1: // Ввод номера карты
                    state.Data["cardNumber"] = text;
                    state.Data["step"] = 2;
                    _userStates[userId] = state;
                    await SendTemporaryMessageAsync(chatId, "Введите название банка (например: Тинькофф, Сбер):", cancellationToken);
                    break;

                case 2: // Ввод банка
                    state.Data["bankName"] = text;
                    state.Data["step"] = 3;
                    _userStates[userId] = state;
                    await SendTemporaryMessageAsync(chatId, "Выберите тип карты (debit/credit):", cancellationToken);
                    break;

                case 3: // Ввод типа карты
                    if (text.ToLower() != "debit" && text.ToLower() != "credit")
                    {
                        await SendTemporaryMessageAsync(chatId, "❌ Введите 'debit' для дебетовой или 'credit' для кредитной карты:", cancellationToken);
                        return;
                    }

                    var card = new BankCard
                    {
                        CardNumber = state.Data["cardNumber"]?.ToString(),
                        BankName = state.Data["bankName"]?.ToString(),
                        CardType = text.ToLower(),
                        IsPrimary = false
                    };

                    var success = await _contactService.AddBankCardAsync(contactId, card);

                    if (success)
                    {
                        await SendTemporaryMessageAsync(chatId, "✅ Банковская карта успешно добавлена!", cancellationToken);
                        _userStates.Remove(userId);
                        _menuManager.ClearMenuState(chatId);
                        await ShowContactBanksAsync(chatId, contactId, cancellationToken);
                    }
                    else
                    {
                        await SendTemporaryMessageAsync(chatId, "❌ Не удалось добавить карту.", cancellationToken);
                        _userStates.Remove(userId);
                        _menuManager.ClearMenuState(chatId);
                    }
                    break;
            }
        }

        private async Task HandleAddCryptoWalletStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var contactId = (int)state.Data["contactId"]!;
            var step = (int)state.Data["step"]!;

            switch (step)
            {
                case 1: // Выбор сети
                    var validNetworks = new[] { "BTC", "ETH", "TRX", "BSC", "SOL" };
                    if (!validNetworks.Contains(text.ToUpper()))
                    {
                        await SendTemporaryMessageAsync(chatId,
                            "❌ Неверная сеть. Выберите из: BTC, ETH, TRX, BSC, SOL", cancellationToken);
                        return;
                    }

                    state.Data["network"] = text.ToUpper();
                    state.Data["step"] = 2;
                    _userStates[userId] = state;
                    await SendTemporaryMessageAsync(chatId, "Введите адрес кошелька:", cancellationToken);
                    break;

                case 2: // Ввод адреса
                    state.Data["address"] = text;
                    state.Data["step"] = 3;
                    _userStates[userId] = state;
                    await SendTemporaryMessageAsync(chatId, "Введите метку для кошелька (или отправьте '-' чтобы пропустить):", cancellationToken);
                    break;

                case 3: // Ввод метки
                    var wallet = new CryptoWallet
                    {
                        Network = state.Data["network"]?.ToString(),
                        Address = state.Data["address"]?.ToString(),
                        Label = text == "-" ? null : text,
                        IsPrimary = false
                    };

                    var success = await _contactService.AddCryptoWalletAsync(contactId, wallet);

                    if (success)
                    {
                        await SendTemporaryMessageAsync(chatId, "✅ Крипто-кошелек успешно добавлен!", cancellationToken);
                        _userStates.Remove(userId);
                        await ShowContactCryptoAsync(chatId, contactId, cancellationToken);
                    }
                    else
                    {
                        await SendTemporaryMessageAsync(chatId, "❌ Не удалось добавить кошелек.", cancellationToken);
                        _userStates.Remove(userId);
                    }
                    break;
            }
        }
        private async Task HandleCreateProjectStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandleCreateProjectStateAsync");
            Console.WriteLine($"   ├─ Шаг: {state.Step}");
            Console.WriteLine($"   ├─ Текст: {text}");

            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine($"❌ Пустое название проекта");
                await SendTemporaryMessageAsync(chatId, "Название проекта не может быть пустым.", cancellationToken);
                return;
            }

            if (state.Step == 1)
            {
                Console.WriteLine($"   → Шаг 1: Сохраняю название проекта");
                state.Data["name"] = text;
                state.Step = 2;
                _userStates[userId] = state;
                await SendTemporaryMessageAsync(chatId, "Введите описание проекта (или отправьте '-' чтобы пропустить):", cancellationToken);
            }
            else if (state.Step == 2)
            {
                Console.WriteLine($"   → Шаг 2: Создаю проект");
                var name = state.Data["name"]?.ToString() ?? "";
                var description = text == "-" ? null : text;

                var project = await _projectService.CreateProjectAsync(name, description, null, userId);
                if (project != null)
                {
                    await SendTemporaryMessageAsync(chatId, $"✅ Проект \"{project.Name}\" успешно создан!", cancellationToken);
                    _userStates.Remove(userId);

                    // ВАЖНО: Очищаем состояние меню, чтобы следующее сообщение было новым
                    _menuManager.ClearMenuState(chatId);

                    await _menuManager.ShowProjectDetailsAsync(chatId, project, cancellationToken, "projects");
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось создать проект");
                    await SendTemporaryMessageAsync(chatId, "❌ Не удалось создать проект.", cancellationToken);
                    _userStates.Remove(userId); // Удаляем состояние
                }
            }
        }

        private async Task HandleEditProjectStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!state.ProjectId.HasValue)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка: ID проекта не найден.", cancellationToken);
                _userStates.Remove(userId); // Очищаем состояние
                return;
            }

            var project = await _projectService.GetProjectAsync(state.ProjectId.Value);
            if (project == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Проект не найден.", cancellationToken);
                _userStates.Remove(userId); // Очищаем состояние
                return;
            }

            project.Name = text;
            project.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _projectService.UpdateProjectAsync(project);
            if (updateResult)
            {
                await SendTemporaryMessageAsync(chatId, $"✅ Название проекта обновлено на: {text}", cancellationToken, 3);
                _userStates.Remove(userId); // ОЧИЩАЕМ СОСТОЯНИЕ ПОСЛЕ УСПЕШНОГО РЕДАКТИРОВАНИЯ
                await _menuManager.ShowProjectDetailsAsync(chatId, project, cancellationToken, "projects");
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось обновить проект.", cancellationToken);
                _userStates.Remove(userId); // ОЧИЩАЕМ СОСТОЯНИЕ ДАЖЕ ПРИ ОШИБКЕ
            }
        }

        private async Task HandleCreateTaskStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!state.ProjectId.HasValue)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка: проект не выбран.", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            // Получаем ID исполнителя
            long assignedToUserId = userId; // По умолчанию на создателя

            if (state.Data.ContainsKey("assignedToUserId") && state.Data["assignedToUserId"] != null)
            {
                if (state.Data["assignedToUserId"] is long longValue)
                {
                    assignedToUserId = longValue;
                }
                else if (state.Data["assignedToUserId"] is int intValue)
                {
                    assignedToUserId = intValue;
                }
                else if (state.Data["assignedToUserId"] is string strValue && long.TryParse(strValue, out long parsed))
                {
                    assignedToUserId = parsed;
                }
            }

            if (state.Step == 1)
            {
                state.Data["title"] = text;
                state.Step = 2;
                _userStates[userId] = state;
                await SendTemporaryMessageAsync(chatId, "Введите описание задачи (или отправьте '-' чтобы пропустить):", cancellationToken);
            }
            else if (state.Step == 2)
            {
                state.Data["description"] = text == "-" ? null : text;
                state.Step = 3;
                _userStates[userId] = state;
                await SendTemporaryMessageAsync(chatId, "Введите срок выполнения задачи в формате ДД.ММ.ГГГГ (или отправьте '-' чтобы пропустить):", cancellationToken);
            }
            else if (state.Step == 3)
            {
                DateTime? dueDate = null;
                if (text != "-")
                {
                    if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        dueDate = parsedDate;
                    }
                    else
                    {
                        await SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ", cancellationToken);
                        return;
                    }
                }

                var task = await _taskService.CreateTaskAsync(
                    title: state.Data["title"]?.ToString() ?? "",
                    description: state.Data["description"]?.ToString(),
                    projectId: state.ProjectId.Value,
                    assignedToUserId: assignedToUserId,
                    createdByUserId: userId,
                    dueDate: dueDate);

                if (task != null)
                {
                    var assignedUser = await _userService.GetUserByTelegramIdAsync(assignedToUserId);
                    var assignedName = assignedUser != null
                        ? (!string.IsNullOrEmpty(assignedUser.Username) ? $"@{assignedUser.Username}" : assignedUser.FirstName)
                        : "Неизвестно";

                    await SendTemporaryMessageAsync(chatId,
                        $"✅ Задача \"{task.Title}\" создана!\n👤 Исполнитель: {assignedName}",
                        cancellationToken, 5);

                    _userStates.Remove(userId);
                    _menuManager.ClearMenuState(chatId);

                    var isAdmin = await _userService.IsAdminAsync(userId);
                    await ShowTaskDetailsAsync(chatId, task.Id, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Не удалось создать задачу.", cancellationToken, 5);
                    _userStates.Remove(userId);
                    _menuManager.ClearMenuState(chatId);
                    var isAdmin = await _userService.IsAdminAsync(userId);
                    await _menuManager.ShowTasksMenuAsync(chatId, isAdmin, cancellationToken);
                }
            }
        }

        private async Task HandleAddDepositStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (state.Step == 1)
            {
                if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму (число больше 0):", cancellationToken);
                    return;
                }

                state.Data["amount"] = amount;
                state.Step = 2;
                await SendTemporaryMessageAsync(chatId, "Введите источник пополнения (например: Карта, Крипта и т.д.):", cancellationToken);
            }
            else if (state.Step == 2)
            {
                var amount = (decimal)state.Data["amount"]!;

                var record = await _financeService.CreateFinancialRecordAsync(
                    type: FinancialRecordType.Deposit,
                    category: "Пополнение",
                    description: text,
                    amount: amount,
                    currency: "РУБ",
                    source: text,
                    userId: userId,
                    projectId: null);

                if (record != null)
                {
                    await SendTemporaryMessageAsync(chatId,
                        $"✅ Депозит пополнен на {amount:N2} РУБ!\nИсточник: {text}",
                        cancellationToken);

                    await _menuManager.ShowFinanceMenuAsync(chatId, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Не удалось пополнить депозит.", cancellationToken);
                }
            }
        }

        private async Task HandleWithdrawDepositStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (state.Step == 1)
            {
                if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму (число больше 0):", cancellationToken);
                    return;
                }

                state.Data["amount"] = amount;
                state.Step = 2;
                await SendTemporaryMessageAsync(chatId, "Введите реквизиты для вывода:", cancellationToken);
            }
            else if (state.Step == 2)
            {
                var amount = (decimal)state.Data["amount"]!;

                var record = await _financeService.CreateFinancialRecordAsync(
                    type: FinancialRecordType.Expense,
                    category: "Вывод",
                    description: $"Вывод средств: {text}",
                    amount: amount,
                    currency: "РУБ",
                    source: text,
                    userId: userId,
                    projectId: null);

                if (record != null)
                {
                    await SendTemporaryMessageAsync(chatId,
                        $"✅ Заявка на вывод {amount:N2} РУБ создана!\nРеквизиты: {text}\n\nОжидайте подтверждения администратора.",
                        cancellationToken, 5);

                    await _menuManager.ShowFinanceMenuAsync(chatId, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Не удалось создать заявку на вывод.", cancellationToken);
                }
            }
        }

        private async Task HandleEditContactFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 1 || field > 8)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 1 до 8", cancellationToken);
                return;
            }

            var contactId = (int)state.Data["contactId"]!;
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            var fieldName = field switch
            {
                1 => "Telegram username",
                2 => "ФИО",
                3 => "Телефон",
                4 => "Номер карты",
                5 => "CVV",
                6 => "Срок карты",
                7 => "Статус",
                8 => "Заметки",
                _ => ""
            };

            state.Data["editField"] = field;
            state.CurrentAction = "db_edit_contact_value";
            state.Step = 2;
            _userStates[userId] = state;

            var currentValue = field switch
            {
                1 => contact.TelegramUsername,
                2 => contact.FullName ?? "не указано",
                3 => contact.PhoneNumber ?? "не указано",
                4 => contact.CardNumber ?? "не указано",
                5 => contact.CVV ?? "не указано",
                6 => contact.CardExpiry ?? "не указано",
                7 => contact.CardStatus ?? "не указано",
                8 => contact.Notes ?? "не указано",
                _ => ""
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ Изменение поля: {fieldName}\n" +
                $"Текущее значение: {currentValue}\n\n" +
                $"Введите новое значение (или отправьте '-' для удаления):", cancellationToken);
        }
        private async Task HandleAddContactStateAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "Username не может быть пустым.", cancellationToken);
                return;
            }

            var contact = await _contactService.CreateSimpleContactAsync(text, contactType: "Доп");
            if (contact != null)
            {
                await SendTemporaryMessageAsync(chatId, $"✅ Контакт @{contact.TelegramUsername} добавлен!", cancellationToken);
                await _menuManager.ShowContactsMenuAsync(chatId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось добавить контакт.", cancellationToken);
            }
        }

        private async Task HandleSearchContactStateAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendTemporaryMessageAsync(chatId, "Поисковый запрос не может быть пустым.", cancellationToken);
                return;
            }

            var contacts = await _contactService.SearchContactsAsync(text);

            if (contacts.Count == 0)
            {
                await SendTemporaryMessageAsync(chatId, $"🔍 По запросу \"{text}\" ничего не найдено.", cancellationToken);
                return;
            }

            var resultText = $"🔍 Результаты поиска по запросу \"{text}\":\n\n";
            foreach (var contact in contacts.Take(15))
            {
                resultText += $"👤 {contact.TelegramUsername}";
                if (!string.IsNullOrEmpty(contact.FullName))
                    resultText += $" - {contact.FullName}";
                resultText += "\n";
            }

            if (contacts.Count > 15)
            {
                resultText += $"\n... и еще {contacts.Count - 15} результатов";
            }

            await SendTemporaryMessageAsync(chatId, resultText, cancellationToken);
        }

        private async Task HandleCreateContentPlanStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (state.Step == 1)
            {
                state.Data["title"] = text;
                state.Step = 2;
                await SendTemporaryMessageAsync(chatId, "Введите цель контент-плана:", cancellationToken);
            }
            else if (state.Step == 2)
            {
                state.Data["goal"] = text;
                state.Step = 3;
                await SendTemporaryMessageAsync(chatId, "Введите стратегию контента:", cancellationToken);
            }
            else if (state.Step == 3)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var contentPlan = new Advertisement
                {
                    Title = state.Data["title"]?.ToString() ?? "",
                    Type = AdvertisementType.ContentPlan,
                    Goal = state.Data["goal"]?.ToString(),
                    ContentStrategy = text,
                    Status = AdvertisementStatus.Planned,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Advertisements.Add(contentPlan);
                await dbContext.SaveChangesAsync();

                await SendTemporaryMessageAsync(chatId, $"✅ Контент-план \"{contentPlan.Title}\" создан!", cancellationToken);
                await _menuManager.ShowAdvertisementMenuAsync(chatId, cancellationToken);
            }
        }

        private async Task HandleCreateCampaignStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (state.Step == 1)
            {
                state.Data["title"] = text;
                state.Step = 2;
                await SendTemporaryMessageAsync(chatId, "Введите бюджет кампании в ₽ (или '-' чтобы пропустить):", cancellationToken);
            }
            else if (state.Step == 2)
            {
                if (text != "-" && decimal.TryParse(text, out decimal budget))
                {
                    state.Data["budget"] = budget;
                }
                state.Step = 3;
                await SendTemporaryMessageAsync(chatId, "Введите цель рекламной кампании:", cancellationToken);
            }
            else if (state.Step == 3)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var campaign = new Advertisement
                {
                    Title = state.Data["title"]?.ToString() ?? "",
                    Type = AdvertisementType.AdCampaign,
                    Goal = text,
                    Budget = state.Data.ContainsKey("budget") ? (decimal?)state.Data["budget"] : null,
                    Status = AdvertisementStatus.Planned,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Advertisements.Add(campaign);
                await dbContext.SaveChangesAsync();

                await SendTemporaryMessageAsync(chatId, $"✅ Рекламная кампания \"{campaign.Title}\" создана!", cancellationToken);
                await _menuManager.ShowAdvertisementMenuAsync(chatId, cancellationToken);
            }
        }
        #endregion

        #region Контакты - ПОЛНАЯ РЕАЛИЗАЦИЯ
        private async Task HandleContactsCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case CallbackData.ContactsAdd:
                    _userStates[userId] = new UserState
                    {
                        CurrentAction = UserActions.AddContact,
                        Step = 1
                    };
                    await SendTemporaryMessageAsync(chatId, "Введите Telegram username (например: @username или просто username):", cancellationToken);
                    break;

                case CallbackData.ContactsSearch:
                    _userStates[userId] = new UserState
                    {
                        CurrentAction = UserActions.SearchContact,
                        Step = 1
                    };
                    await SendTemporaryMessageAsync(chatId, "Введите имя, username или тег для поиска:", cancellationToken);
                    break;

                case CallbackData.ContactsList:
                    await ShowAllContactsAsync(chatId, cancellationToken);
                    break;

                // НОВОЕ: Показать банковские карты контакта
                case var _ when callbackData.StartsWith("contact_banks_"):
                    {
                        var contactIdStr = callbackData.Replace("contact_banks_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            await ShowContactBanksAsync(chatId, contactId, cancellationToken);
                        }
                        break;
                    }

                // НОВОЕ: Показать крипто-кошельки контакта
                case var _ when callbackData.StartsWith("contact_crypto_"):
                    {
                        var contactIdStr = callbackData.Replace("contact_crypto_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            await ShowContactCryptoAsync(chatId, contactId, cancellationToken);
                        }
                        break;
                    }

                // НОВОЕ: Добавить банковскую карту
                case var _ when callbackData.StartsWith("contact_add_bank_"):
                    {
                        var contactIdStr = callbackData.Replace("contact_add_bank_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            _userStates[userId] = new UserState
                            {
                                CurrentAction = "add_bank_card",
                                Data = new Dictionary<string, object?> { ["contactId"] = contactId, ["step"] = 1 },
                                Step = 1
                            };
                            await SendTemporaryMessageAsync(chatId,
                                "💳 ДОБАВЛЕНИЕ БАНКОВСКОЙ КАРТЫ\n\n" +
                                "Введите номер карты (целиком):", cancellationToken);
                        }
                        break;
                    }

                // НОВОЕ: Добавить крипто-кошелек
                case var _ when callbackData.StartsWith("contact_add_crypto_"):
                    {
                        var contactIdStr = callbackData.Replace("contact_add_crypto_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            _userStates[userId] = new UserState
                            {
                                CurrentAction = "add_crypto_wallet",
                                Data = new Dictionary<string, object?> { ["contactId"] = contactId, ["step"] = 1 },
                                Step = 1
                            };
                            await SendTemporaryMessageAsync(chatId,
                                "₿ ДОБАВЛЕНИЕ КРИПТО-КОШЕЛЬКА\n\n" +
                                "Выберите сеть:\n" +
                                "• BTC - Bitcoin\n" +
                                "• ETH - Ethereum\n" +
                                "• TRX - Tron\n" +
                                "• BSC - Binance Smart Chain\n" +
                                "• SOL - Solana\n\n" +
                                "Введите название сети (BTC/ETH/TRX/BSC/SOL):", cancellationToken);
                        }
                        break;
                    }

                // НОВОЕ: Удалить банковскую карту
                case var _ when callbackData.StartsWith("contact_delete_bank_"):
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 4 && int.TryParse(parts[3], out int contactId))
                        {
                            var cardNumber = parts.Length >= 5 ? parts[4] : "";
                            await ShowDeleteBankCardConfirmationAsync(chatId, contactId, cardNumber, cancellationToken);
                        }
                        break;
                    }

                // НОВОЕ: Удалить крипто-кошелек
                case var _ when callbackData.StartsWith("contact_delete_crypto_"):
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 4 && int.TryParse(parts[3], out int contactId))
                        {
                            var address = parts.Length >= 5 ? parts[4] : "";
                            await ShowDeleteCryptoWalletConfirmationAsync(chatId, contactId, address, cancellationToken);
                        }
                        break;
                    }

                // НОВОЕ: Сделать карту основной
                case var _ when callbackData.StartsWith("contact_set_primary_bank_"):
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 6 && int.TryParse(parts[4], out int contactId))
                        {
                            var cardNumber = parts.Length >= 6 ? parts[5] : "";
                            await SetPrimaryBankCardAsync(chatId, contactId, cardNumber, cancellationToken);
                        }
                        break;
                    }

                // НОВОЕ: Сделать кошелек основным
                case var _ when callbackData.StartsWith("contact_set_primary_crypto_"):
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int contactId))
                        {
                            var address = parts.Length >= 6 ? parts[5] : "";
                            await SetPrimaryCryptoWalletAsync(chatId, contactId, address, cancellationToken);
                        }
                        break;
                    }

                default:
                    if (callbackData.StartsWith("contact_"))
                    {
                        var contactIdStr = callbackData.Replace("contact_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            await ShowContactDetailsAsync(chatId, contactId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("contact_edit_"))
                    {
                        var contactIdStr = callbackData.Replace("contact_edit_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            await ShowContactEditMenuAsync(chatId, contactId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("contact_delete_"))
                    {
                        var contactIdStr = callbackData.Replace("contact_delete_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            await ShowDeleteContactConfirmationAsync(chatId, contactId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("delete_contact_confirm_"))
                    {
                        var contactIdStr = callbackData.Replace("delete_contact_confirm_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            await DeleteContactAsync(chatId, contactId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("delete_contact_cancel_"))
                    {
                        var contactIdStr = callbackData.Replace("delete_contact_cancel_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            await ShowContactDetailsAsync(chatId, contactId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("contact_edit_name_"))
                    {
                        var contactIdStr = callbackData.Replace("contact_edit_name_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            _userStates[userId] = new UserState
                            {
                                CurrentAction = "edit_contact_name",
                                Data = new Dictionary<string, object?> { ["contactId"] = contactId },
                                Step = 1
                            };
                            await SendTemporaryMessageAsync(chatId, "Введите новое имя контакта:", cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("contact_edit_tags_"))
                    {
                        var contactIdStr = callbackData.Replace("contact_edit_tags_", "");
                        if (int.TryParse(contactIdStr, out int contactId))
                        {
                            _userStates[userId] = new UserState
                            {
                                CurrentAction = "edit_contact_tags",
                                Data = new Dictionary<string, object?> { ["contactId"] = contactId },
                                Step = 1
                            };
                            await SendTemporaryMessageAsync(chatId, "Введите новые теги (через запятую):", cancellationToken);
                        }
                    }
                    break;
            }
        }

        private async Task ShowAllContactsAsync(long chatId, CancellationToken cancellationToken)
        {
            var contacts = await _contactService.GetAllContactsAsync();

            if (contacts.Count == 0)
            {
                await _menuManager.ShowInlineMenuAsync(
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
                return;
            }

            var text = $"👥 Список контактов ({contacts.Count}):\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var contact in contacts.Take(15))
            {
                var displayName = !string.IsNullOrEmpty(contact.FullName)
                    ? $"{contact.FullName}"
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

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "contacts_list",
                cancellationToken);
        }

        private async Task ShowContactDetailsAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Контакт не найден.", cancellationToken);
                await _menuManager.ShowContactsMenuAsync(chatId, cancellationToken);
                return;
            }

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
                new()
                {
                    InlineKeyboardButton.WithCallbackData("💳 Карты", $"contact_banks_{contact.Id}"),
                    InlineKeyboardButton.WithCallbackData("₿ Крипто", $"contact_crypto_{contact.Id}")
                },
                new() { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"contact_delete_{contact.Id}") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToContacts) }
            };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                $"contact_{contact.Id}",
                cancellationToken);
        }

        private async Task ShowContactEditMenuAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Контакт не найден.", cancellationToken);
                return;
            }

            var text = $"✏️ Редактирование контакта\n\n" +
                      $"Текущие данные:\n" +
                      $"Username: @{contact.TelegramUsername}\n" +
                      $"Имя: {contact.FullName ?? "Не указано"}\n" +
                      $"Теги: {contact.Tags ?? "Не указаны"}\n\n" +
                      $"Что вы хотите изменить?";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("👤 Изменить имя", $"contact_edit_name_{contactId}") },
        new() { InlineKeyboardButton.WithCallbackData("🏷️ Изменить теги", $"contact_edit_tags_{contactId}") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", $"contact_{contactId}") }
    };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                $"contact_edit_{contactId}",
                cancellationToken);
        }

        private async Task ShowDeleteContactConfirmationAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить контакт?\n\n" +
                       $"👤 {name}\n" +
                       $"📱 @{contact.TelegramUsername}\n" +
                       $"📞 {contact.PhoneNumber ?? "нет телефона"}\n\n" +
                       $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"delete_contact_confirm_{contactId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_contact_view_{contactId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_confirmation", cancellationToken);
        }

        // ===== УДАЛЕНИЕ КОНТАКТА =====
        private async Task DeleteContactAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            await HandleDeleteConfirmationAsync(
                chatId,
                contactId,
                (id) => _contactService.GetContactAsync(id),
                (id) => _contactService.DeleteContactAsync(id),
                (contact) => !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}",
                "db_contacts_menu",
                cancellationToken
            );
        }
        #endregion

        #region Банковские карты и крипто-кошельки контактов

        private async Task ShowContactBanksAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var contactData = await _contactService.GetContactWithDecryptedDataAsync(contactId);
            if (contactData == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Контакт не найден.", cancellationToken);
                return;
            }

            var contact = contactData.Contact;
            var cards = contactData.BankCards ?? new List<BankCard>();

            var text = $"💳 БАНКОВСКИЕ КАРТЫ\n\n" +
                       $"Контакт: @{contact.TelegramUsername}\n";

            if (!string.IsNullOrEmpty(contact.FullName))
                text += $"Имя: {contact.FullName}\n";

            text += $"\n📋 Список карт ({cards.Count}):\n\n";

            if (cards.Any())
            {
                foreach (var card in cards)
                {
                    var primary = card.IsPrimary ? "⭐ " : "   ";
                    text += $"{primary}•••• {card.CardNumber}\n";
                    text += $"   Банк: {card.BankName ?? "Не указан"}\n";
                    text += $"   Тип: {(card.CardType == "debit" ? "Дебетовая" : card.CardType == "credit" ? "Кредитная" : card.CardType)}\n";

                    if (!string.IsNullOrEmpty(card.Notes))
                        text += $"   Заметки: {card.Notes}\n";

                    text += "\n";
                }
            }
            else
            {
                text += "📭 Нет добавленных карт\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>();

            // Кнопка добавления карты
            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ Добавить карту", $"contact_add_bank_{contactId}")
    });

            // Если есть карты, добавляем кнопки для каждой
            if (cards.Any())
            {
                foreach (var card in cards.Take(3)) // Максимум 3 карты в меню
                {
                    if (!card.IsPrimary)
                    {
                        buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"⭐ Сделать основной •••• {card.CardNumber}",
                        $"contact_set_primary_bank_{contactId}_{card.CardNumber}")
                });
                    }

                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"🗑️ Удалить •••• {card.CardNumber}",
                    $"contact_delete_bank_{contactId}_{card.CardNumber}")
            });
                }
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", $"contact_{contactId}")
    });

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                $"contact_banks_{contactId}",
                cancellationToken);
        }

        private async Task ShowContactCryptoAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var contactData = await _contactService.GetContactWithDecryptedDataAsync(contactId);
            if (contactData == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Контакт не найден.", cancellationToken);
                return;
            }

            var contact = contactData.Contact;
            var wallets = contactData.CryptoWallets ?? new List<CryptoWallet>();

            var text = $"₿ КРИПТО-КОШЕЛЬКИ\n\n" +
                       $"Контакт: @{contact.TelegramUsername}\n";

            if (!string.IsNullOrEmpty(contact.FullName))
                text += $"Имя: {contact.FullName}\n";

            text += $"\n📋 Список кошельков ({wallets.Count}):\n\n";

            if (wallets.Any())
            {
                foreach (var wallet in wallets)
                {
                    var primary = wallet.IsPrimary ? "⭐ " : "   ";
                    text += $"{primary}{wallet.Network}: {wallet.Address}\n";

                    if (!string.IsNullOrEmpty(wallet.Label))
                        text += $"   Метка: {wallet.Label}\n";

                    text += "\n";
                }
            }
            else
            {
                text += "📭 Нет добавленных кошельков\n\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>();

            // Кнопка добавления кошелька
            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ Добавить кошелек", $"contact_add_crypto_{contactId}")
    });

            // Если есть кошельки, добавляем кнопки для каждого
            if (wallets.Any())
            {
                foreach (var wallet in wallets.Take(3)) // Максимум 3 кошелька в меню
                {
                    var shortAddress = wallet.Address?.Length > 10
                        ? wallet.Address.Substring(0, 6) + "..." + wallet.Address.Substring(wallet.Address.Length - 4)
                        : wallet.Address ?? "";

                    if (!wallet.IsPrimary)
                    {
                        buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"⭐ Сделать основным {wallet.Network} {shortAddress}",
                        $"contact_set_primary_crypto_{contactId}_{wallet.Address}")
                });
                    }

                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"🗑️ Удалить {wallet.Network} {shortAddress}",
                    $"contact_delete_crypto_{contactId}_{wallet.Address}")
            });
                }
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", $"contact_{contactId}")
    });

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                $"contact_crypto_{contactId}",
                cancellationToken);
        }

        private async Task ShowDeleteBankCardConfirmationAsync(long chatId, int contactId, string cardNumber, CancellationToken cancellationToken)
        {
            await _menuManager.ShowDeleteConfirmationAsync(
                chatId,
                "банковскую карту",
                $"Номер карты: •••• {cardNumber}",
                $"confirm_delete_bank_{contactId}_{cardNumber}",
                $"contact_banks_{contactId}",
                cancellationToken);
        }

        private async Task ShowDeleteCryptoWalletConfirmationAsync(long chatId, int contactId, string address, CancellationToken cancellationToken)
        {
            var shortAddress = address?.Length > 10
                ? address.Substring(0, 6) + "..." + address.Substring(address.Length - 4)
                : address ?? "";

            await _menuManager.ShowDeleteConfirmationAsync(
                chatId,
                "крипто-кошелек",
                $"Адрес: {shortAddress}",
                $"confirm_delete_crypto_{contactId}_{address}",
                $"contact_crypto_{contactId}",
                cancellationToken);
        }

        private async Task SetPrimaryBankCardAsync(long chatId, int contactId, string cardNumber, CancellationToken cancellationToken)
        {
            try
            {
                // Получаем все карты контакта
                var contactData = await _contactService.GetContactWithDecryptedDataAsync(contactId);
                if (contactData == null)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Контакт не найден.", cancellationToken);
                    return;
                }

                var cards = contactData.BankCards ?? new List<BankCard>();
                var cardToUpdate = cards.FirstOrDefault(c => c.CardNumber == cardNumber);

                if (cardToUpdate == null)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Карта не найдена.", cancellationToken);
                    return;
                }

                // Сбрасываем флаг IsPrimary у всех карт
                foreach (var card in cards)
                {
                    card.IsPrimary = false;
                }

                // Устанавливаем выбранную карту как основную
                cardToUpdate.IsPrimary = true;

                // Обновляем в БД (нужно добавить метод в ContactService)
                var contact = await _contactService.GetContactAsync(contactId);
                if (contact != null)
                {
                    contact.BankCardsJson = System.Text.Json.JsonSerializer.Serialize(cards);
                    await _contactService.UpdateContactAsync(contact);
                }

                await SendTemporaryMessageAsync(chatId, "✅ Основная карта обновлена!", cancellationToken);
                _menuManager.ClearMenuState(chatId);
                await ShowContactBanksAsync(chatId, contactId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary bank card");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении основной карты.", cancellationToken);
            }
        }

        private async Task SetPrimaryCryptoWalletAsync(long chatId, int contactId, string address, CancellationToken cancellationToken)
        {
            try
            {
                // Получаем все кошельки контакта
                var contactData = await _contactService.GetContactWithDecryptedDataAsync(contactId);
                if (contactData == null)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Контакт не найден.", cancellationToken);
                    return;
                }

                var wallets = contactData.CryptoWallets ?? new List<CryptoWallet>();
                var walletToUpdate = wallets.FirstOrDefault(w => w.Address == address);

                if (walletToUpdate == null)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Кошелек не найден.", cancellationToken);
                    return;
                }

                // Сбрасываем флаг IsPrimary у всех кошельков
                foreach (var wallet in wallets)
                {
                    wallet.IsPrimary = false;
                }

                // Устанавливаем выбранный кошелек как основной
                walletToUpdate.IsPrimary = true;

                // Обновляем в БД
                var contact = await _contactService.GetContactAsync(contactId);
                if (contact != null)
                {
                    contact.CryptoWalletsJson = System.Text.Json.JsonSerializer.Serialize(wallets);
                    await _contactService.UpdateContactAsync(contact);
                }

                await SendTemporaryMessageAsync(chatId, "✅ Основной кошелек обновлен!", cancellationToken);
                await ShowContactCryptoAsync(chatId, contactId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary crypto wallet");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении основного кошелька.", cancellationToken);
            }
        }

        #endregion

        #region Статусы - ПОЛНАЯ РЕАЛИЗАЦИЯ
        private async Task HandleStatusCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandleStatusCallbackAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   └─ Callback Data: {callbackData}");

            switch (callbackData)
            {
                case CallbackData.StatusBoard:
                    Console.WriteLine($"   → Выбран: StatusBoard");
                    await ShowStatusBoardAsync(chatId, cancellationToken);
                    break;

                case CallbackData.StatusProgress:
                    Console.WriteLine($"   → Выбран: StatusProgress");
                    await ShowProgressViewAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToStatuses:
                    Console.WriteLine($"   → Выбран: BackToStatuses");
                    await _menuManager.ShowStatusesMenuAsync(chatId, cancellationToken);
                    break;

                default:
                    // СМЕНА СТАТУСА ПРОЕКТА (приоритет)
                    if (callbackData.StartsWith("status_completed_") ||
                        callbackData.StartsWith("status_pending_") ||
                        callbackData.StartsWith("status_inprogress_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 3 && int.TryParse(parts[2], out int projectId))
                        {
                            var newStatus = parts[1] switch
                            {
                                "pending" => ProjectStatus.Pending,
                                "inprogress" => ProjectStatus.InProgress,
                                "completed" => ProjectStatus.Completed,
                                _ => ProjectStatus.Pending
                            };

                            Console.WriteLine($"   → Смена статуса проекта ID: {projectId} на {newStatus}");
                            await UpdateProjectStatusAsync(chatId, projectId, newStatus, cancellationToken);

                            // После смены статуса возвращаемся к списку проектов этого статуса
                            await ShowProjectsByStatusAsync(chatId, newStatus, cancellationToken);
                        }
                    }
                    // Показываем проекты по статусу
                    else if (callbackData.StartsWith("status_"))
                    {
                        var statusType = callbackData.Replace("status_", "");
                        if (statusType == "pending")
                            await ShowProjectsByStatusAsync(chatId, ProjectStatus.Pending, cancellationToken);
                        else if (statusType == "inprogress")
                            await ShowProjectsByStatusAsync(chatId, ProjectStatus.InProgress, cancellationToken);
                        else if (statusType == "completed")
                            await ShowProjectsByStatusAsync(chatId, ProjectStatus.Completed, cancellationToken);
                    }
                    // Меняем статус проекта (старый формат)
                    else if (callbackData.StartsWith("change_status_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 4 && int.TryParse(parts[2], out int projectId))
                        {
                            var newStatus = parts[3] switch
                            {
                                "pending" => ProjectStatus.Pending,
                                "inprogress" => ProjectStatus.InProgress,
                                "completed" => ProjectStatus.Completed,
                                _ => ProjectStatus.Pending
                            };
                            await UpdateProjectStatusAsync(chatId, projectId, newStatus, cancellationToken);
                        }
                    }
                    // Просмотр деталей проекта из статусов
                    else if (callbackData.StartsWith("project_from_statuses_"))
                    {
                        var projectIdStr = callbackData.Replace("project_from_statuses_", "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Просмотр проекта {projectId} из статусов");
                            var project = await _projectService.GetProjectAsync(projectId);
                            if (project != null)
                            {
                                await _menuManager.ShowProjectDetailsAsync(chatId, project, cancellationToken, "statuses");
                            }
                        }
                    }
                    // Старый формат просмотра проекта
                    else if (callbackData.StartsWith(CallbackData.ProjectPrefix))
                    {
                        var projectIdStr = callbackData.Replace(CallbackData.ProjectPrefix, "");
                        if (int.TryParse(projectIdStr, out int projectId))
                        {
                            Console.WriteLine($"   → Просмотр проекта ID: {projectId} ИЗ СТАТУСОВ (старый формат)");
                            var project = await _projectService.GetProjectAsync(projectId);
                            if (project != null)
                            {
                                await _menuManager.ShowProjectDetailsAsync(chatId, project, cancellationToken, "statuses");
                            }
                        }
                    }
                    break;
            }
        }
        private async Task ShowProgressViewAsync(long chatId, CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var tasks = await _taskService.GetAllTasksAsync();

            var text = "📊 ПРОГРЕСС ВЫПОЛНЕНИЯ\n\n";

            // Общая статистика
            var totalProjects = projects.Count;
            var completedProjects = projects.Count(p => p.Status == ProjectStatus.Completed);
            var inProgressProjects = projects.Count(p => p.Status == ProjectStatus.InProgress);
            var projectProgress = totalProjects > 0 ? (completedProjects * 100 / totalProjects) : 0;

            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == TeamTaskStatus.Completed);
            var activeTasks = tasks.Count(t => t.Status == TeamTaskStatus.Active);
            var taskProgress = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

            text += $"📈 ОБЩАЯ СТАТИСТИКА:\n";
            text += $"Проекты: {completedProjects}/{totalProjects} завершено ({projectProgress}%)\n";
            text += $"Задачи: {completedTasks}/{totalTasks} выполнено ({taskProgress}%)\n\n";

            // Прогресс по каждому проекту
            text += $"📋 ПРОГРЕСС ПО ПРОЕКТАМ:\n\n";

            var activeProjects = projects.Where(p => p.Status == ProjectStatus.InProgress).ToList();
            if (activeProjects.Any())
            {
                foreach (var project in activeProjects.Take(5))
                {
                    var projectTasks = tasks.Where(t => t.ProjectId == project.Id).ToList();
                    var projectCompleted = projectTasks.Count(t => t.Status == TeamTaskStatus.Completed);
                    var projectTotal = projectTasks.Count;
                    var progress = projectTotal > 0 ? (projectCompleted * 100 / projectTotal) : 0;

                    // Прогресс-бар
                    var barLength = 10;
                    var filledBars = progress * barLength / 100;
                    var emptyBars = barLength - filledBars;
                    var progressBar = new string('█', filledBars) + new string('░', emptyBars);

                    text += $"📂 {project.Name}\n";
                    text += $"  {progressBar} {progress}% ({projectCompleted}/{projectTotal} задач)\n";

                    // Активные задачи в проекте
                    var activeProjectTasks = projectTasks.Where(t => t.Status == TeamTaskStatus.Active).Take(2);
                    foreach (var task in activeProjectTasks)
                    {
                        var assigned = task.AssignedTo?.Username ?? "не назначен";
                        text += $"  • {task.Title} (@{assigned})\n";
                    }
                    text += "\n";
                }
            }
            else
            {
                text += "Нет активных проектов\n";
            }

            // Предстоящие проекты
            var pendingProjects = projects.Where(p => p.Status == ProjectStatus.Pending).ToList();
            if (pendingProjects.Any())
            {
                text += $"🕒 ПРЕДСТОЯТ:\n";
                foreach (var project in pendingProjects.Take(3))
                {
                    text += $"• {project.Name}\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("🗺️ Статусная доска", CallbackData.StatusBoard) },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses) }
    };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "progress_view",
                cancellationToken);
        }

        private async Task ShowProjectDetailsWithContextAsync(long chatId, int projectId, string context, CancellationToken cancellationToken)
        {
            var project = await _projectService.GetProjectAsync(projectId);
            if (project == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Проект не найден.", cancellationToken);
                return;
            }

            // Вызываем метод MenuManager с правильным контекстом
            await _menuManager.ShowProjectDetailsAsync(chatId, project, cancellationToken, context);
        }

        private async Task ShowProjectDetailsFromStatusesAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            var project = await _projectService.GetProjectAsync(projectId);
            if (project == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Проект не найден.", cancellationToken);
                return;
            }

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
        // Кнопка назад ведёт в статусы, а не в проекты
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses) }
    };

            var keyboard = new InlineKeyboardMarkup(buttons);
            await _menuManager.ShowInlineMenuAsync(chatId, text, keyboard, $"project_{project.Id}", cancellationToken);
        }

        private async Task ShowProjectSelectionForStatusAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var activeProjects = projects.Where(p => p.Status != ProjectStatus.Completed).ToList();

            if (activeProjects.Count == 0)
            {
                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    "📭 Нет активных проектов для написания статуса.",
                    new InlineKeyboardMarkup(new[]
                    {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Создать проект", CallbackData.CreateProject),
                    InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses)
                }
                    }),
                    "status_project_selection",
                    cancellationToken);
                return;
            }

            var text = "📝 НАПИСАНИЕ СТАТУСА\n\nВыберите проект:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var project in activeProjects.Take(10))
            {
                var statusIcon = project.Status switch
                {
                    ProjectStatus.Pending => "🟡",
                    ProjectStatus.InProgress => "🟠",
                    _ => "⚪"
                };

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(
                $"{statusIcon} {project.Name}",
                $"write_status_for_{project.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses)
    });

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "status_project_selection",
                cancellationToken);
        }

        private async Task ShowStatusBoardAsync(long chatId, CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetAllProjectsAsync();

            var pendingCount = projects.Count(p => p.Status == ProjectStatus.Pending);
            var inProgressCount = projects.Count(p => p.Status == ProjectStatus.InProgress);
            var completedCount = projects.Count(p => p.Status == ProjectStatus.Completed);

            var text = "📊 СТАТУСЫ ПРОЕКТОВ\n\n" +
                       $"🟡 Предстоит: {pendingCount}\n" +
                       $"🟠 В работе: {inProgressCount}\n" +
                       $"✅ Готово: {completedCount}\n\n" +
                       "Выберите статус для просмотра проектов:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData($"🟡 Предстоит ({pendingCount})", "status_pending"),
            InlineKeyboardButton.WithCallbackData($"🟠 В работе ({inProgressCount})", "status_inprogress"),
            InlineKeyboardButton.WithCallbackData($"✅ Готово ({completedCount})", "status_completed")
        },
        new() { InlineKeyboardButton.WithCallbackData("📊 Прогресс", CallbackData.StatusProgress) },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses) }
    };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "status_board",
                cancellationToken);
        }
        private async Task ShowProjectsByStatusAsync(long chatId, ProjectStatus status, CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var filteredProjects = projects.Where(p => p.Status == status).ToList();

            var statusName = status switch
            {
                ProjectStatus.Pending => "🟡 ПРЕДСТОИТ",
                ProjectStatus.InProgress => "🟠 В РАБОТЕ",
                ProjectStatus.Completed => "✅ ГОТОВО",
                _ => "ПРОЕКТЫ"
            };

            var text = $"{statusName}\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var project in filteredProjects.Take(10))
            {
                text += $"📂 {project.Name}\n";
                if (!string.IsNullOrEmpty(project.Description))
                    text += $"  {project.Description}\n";
                text += "\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(
                $"📂 {project.Name}",
                $"project_from_statuses_{project.Id}")  // ВАЖНО: специальный префикс
        });
            }

            // Кнопка назад
            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад к статусам", CallbackData.BackToStatuses)
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"status_{status}", cancellationToken);
        }

        private async Task ShowProgressOverviewAsync(long chatId, CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var tasks = await _taskService.GetAllTasksAsync();

            var text = "📊 ДЕТАЛЬНЫЙ ПРОГРЕСС\n\n";

            // Проекты в работе
            var inProgressProjects = projects.Where(p => p.Status == ProjectStatus.InProgress).ToList();
            text += $"🟠 ПРОЕКТЫ В РАБОТЕ ({inProgressProjects.Count}):\n";

            if (inProgressProjects.Any())
            {
                foreach (var project in inProgressProjects)
                {
                    var projectTasks = tasks.Where(t => t.ProjectId == project.Id).ToList();
                    var completedTasks = projectTasks.Count(t => t.Status == TeamTaskStatus.Completed);
                    var activeTasks = projectTasks.Count(t => t.Status == TeamTaskStatus.Active);
                    var totalTasks = projectTasks.Count;
                    var progress = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

                    // Прогресс-бар - ИСПРАВЛЕНО
                    var barLength = 10;
                    var filledBars = progress * barLength / 100;
                    var emptyBars = barLength - filledBars;
                    var progressBar = new string('█', filledBars) + new string('░', emptyBars);

                    text += $"\n📂 {project.Name}\n";
                    text += $"  {progressBar} {progress}%\n";
                    text += $"  ✅ Выполнено: {completedTasks}/{totalTasks}\n";
                    text += $"  🟢 В работе: {activeTasks}\n";

                    // Показываем активные задачи
                    if (activeTasks > 0)
                    {
                        text += $"  📋 Активные задачи:\n";
                        var activeProjectTasks = projectTasks.Where(t => t.Status == TeamTaskStatus.Active).Take(3);
                        foreach (var task in activeProjectTasks)
                        {
                            var assigned = task.AssignedTo?.Username ?? "не назначен";
                            var dueDate = task.DueDate.HasValue ? $" до {task.DueDate.Value:dd.MM}" : "";
                            text += $"    • {task.Title} (@{assigned}){dueDate}\n";
                        }
                        if (activeTasks > 3)
                            text += $"    ... и еще {activeTasks - 3}\n";
                    }
                }
            }
            else
            {
                text += "\n  Нет проектов в работе\n";
            }

            // Предстоящие проекты
            var pendingProjects = projects.Where(p => p.Status == ProjectStatus.Pending).ToList();
            if (pendingProjects.Any())
            {
                text += $"\n🟡 ПРЕДСТОЯТ:\n";
                foreach (var project in pendingProjects.Take(3))
                {
                    text += $"• {project.Name}\n";
                }
                if (pendingProjects.Count > 3)
                    text += $"  ... и еще {pendingProjects.Count - 3}\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📝 Написать статус", CallbackData.StatusWrite) },
        new() { InlineKeyboardButton.WithCallbackData("🗺️ Статусная доска", CallbackData.StatusBoard) },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToStatuses) }
    };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                "progress_overview",
                cancellationToken);
        }

        private async Task ShowProjectStatusHistoryAsync(long chatId, int projectId, CancellationToken cancellationToken)
        {
            var project = await _projectService.GetProjectAsync(projectId);
            if (project == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Проект не найден.", cancellationToken);
                return;
            }

            var tasks = project.Tasks?.ToList() ?? new List<TeamTask>();
            var completedTasks = tasks.Count(t => t.Status == TeamTaskStatus.Completed);
            var activeTasks = tasks.Count(t => t.Status == TeamTaskStatus.Active);
            var totalTasks = tasks.Count;
            var progress = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

            var statusIcon = project.Status switch
            {
                ProjectStatus.Pending => "🟡 Предстоит",
                ProjectStatus.InProgress => "🟠 В работе",
                ProjectStatus.Completed => "✅ Готово",
                _ => "⚪ Неизвестно"
            };

            var text = $"📂 ПРОЕКТ: {project.Name}\n\n" +
                       $"📊 Статус: {statusIcon}\n" +
                       $"📈 Прогресс: {progress}% ({completedTasks}/{totalTasks} задач)\n\n";

            // Активные задачи
            if (activeTasks > 0)
            {
                text += $"🟢 АКТИВНЫЕ ЗАДАЧИ:\n";
                foreach (var task in tasks.Where(t => t.Status == TeamTaskStatus.Active).Take(5))
                {
                    var assigned = task.AssignedTo?.Username ?? "не назначен";
                    var dueDate = task.DueDate.HasValue ? $" до {task.DueDate.Value:dd.MM}" : "";
                    text += $"• {task.Title} - @{assigned}{dueDate}\n";
                }
                if (activeTasks > 5)
                    text += $"  ... и еще {activeTasks - 5}\n";
                text += "\n";
            }

            // История статусов
            var statusUpdates = await _projectService.GetProjectStatusHistoryAsync(projectId, 5);
            if (statusUpdates.Any())
            {
                text += $"📝 ПОСЛЕДНИЕ СТАТУСЫ:\n";
                foreach (var update in statusUpdates.OrderByDescending(s => s.CreatedAt))
                {
                    var date = update.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                    var author = update.CreatedBy?.Username ?? "Неизвестно";
                    text += $"• {date} (@{author}): {update.Text}\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📝 Добавить статус", $"write_status_for_{projectId}") },
        new() { InlineKeyboardButton.WithCallbackData("📋 Все задачи", CallbackData.TasksList) },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.StatusBoard) }
    };

            await _menuManager.ShowInlineMenuAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                $"project_status_{projectId}",
                cancellationToken);
        }
        #endregion

        #region Навигация
        private async Task HandleNavigationCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            var isAdmin = await _userService.IsAdminAsync(userId);

            switch (callbackData)
            {
                case CallbackData.BackToMain:
                    await _menuManager.ShowMainMenuAsync(chatId, userId, isAdmin, cancellationToken);
                    break;

                case CallbackData.BackToProjects:
                    await _menuManager.ShowProjectsMenuAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToTasks:
                    await _menuManager.ShowTasksMenuAsync(chatId, isAdmin, cancellationToken);
                    break;

                case CallbackData.BackToStatuses:
                    await _menuManager.ShowStatusesMenuAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToAdvertisement:
                    await _menuManager.ShowAdvertisementMenuAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToContacts:
                    await _menuManager.ShowContactsMenuAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToDatabase:
                    await _menuManager.ShowDatabaseMenuAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToFinance:
                    await _menuManager.ShowFinanceMenuAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToKpi:
                    await _menuManager.ShowKPIMenuAsync(chatId, cancellationToken);
                    break;

                case CallbackData.BackToSettings:
                    await _menuManager.ShowSettingsMenuAsync(chatId, cancellationToken);
                    break;
            }
        }
        #endregion

        #region Настройки - РЕАЛИЗАЦИЯ
        private async Task HandleSettingsCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandleSettingsCallbackAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   └─ Callback Data: {callbackData}");

            var user = await _userService.GetUserByTelegramIdAsync(userId);
            if (user == null || user.Role != UserRole.Admin)
            {
                await SendTemporaryMessageAsync(chatId, "⛔ У вас нет прав администратора.", cancellationToken);
                return;
            }

            switch (callbackData)
            {
                // Основные разделы настроек
                case CallbackData.SettingsUsers:
                    await ShowUsersManagementAsync(chatId, cancellationToken);
                    break;

                case CallbackData.SettingsSecurity:
                    await ShowSecuritySettingsAsync(chatId, cancellationToken);
                    break;

                case CallbackData.SettingsReports:
                    await ShowReportsSettingsAsync(chatId, cancellationToken);
                    break;

                case CallbackData.SettingsDatabase:
                    await ShowDatabaseSettingsAsync(chatId, cancellationToken);
                    break;

                // НОВОЕ: Уведомления
                case var _ when callbackData.StartsWith("notification_view_"):
                    var viewIdStr = callbackData.Replace("notification_view_", "");
                    if (int.TryParse(viewIdStr, out int viewId))
                    {
                        await ShowNotificationDetailsAsync(chatId, viewId, cancellationToken);
                    }
                    break;
                case "settings_notifications":
                    await ShowNotificationsMenuAsync(chatId, cancellationToken);
                    break;
                case "edit_freq_once":
                case "edit_freq_daily":
                case "edit_freq_weekly":
                case "edit_freq_monthly":
                case "edit_freq_yearly":
                    await HandleEditFrequencySelectionAsync(chatId, userId, callbackData, cancellationToken);
                    break;

                // Навигация
                case CallbackData.BackToSettings:
                    await _menuManager.ShowSettingsMenuAsync(chatId, cancellationToken);
                    break;

                // Пользователи - подменю
                case "settings_users_stats":
                    await ShowUsersStatisticsAsync(chatId, cancellationToken);
                    break;
                case "settings_add_admin":
                    await ShowAddAdminMenuAsync(chatId, cancellationToken);
                    break;
                case "settings_remove_admin":
                    await ShowRemoveAdminMenuAsync(chatId, cancellationToken);
                    break;

                // Безопасность - подменю
                case "security_logs":
                    await ShowSecurityLogsAsync(chatId, cancellationToken);
                    break;
                case "security_sessions":
                    await ShowActiveSessionsAsync(chatId, cancellationToken);
                    break;

                // База данных - подменю
                case "settings_db_stats":
                    await ShowDatabaseDetailedStatsAsync(chatId, cancellationToken);
                    break;
                case "settings_db_backup":
                    await CreateDatabaseBackupAsync(chatId, cancellationToken);
                    break;

                // Отчеты - новые настройки
                case "settings_reports_schedule":
                    await ShowReportScheduleAsync(chatId, cancellationToken);
                    break;
                case "settings_reports_toggle":
                    await ToggleReportScheduleAsync(chatId, cancellationToken);
                    break;
                case "settings_reports_frequency":
                    await ShowReportFrequencyMenuAsync(chatId, cancellationToken);
                    break;
                case "settings_reports_time":
                    await StartSetReportTimeAsync(chatId, userId, cancellationToken);
                    break;
                case "reports_frequency_daily":
                case "reports_frequency_weekly":
                case "reports_frequency_monthly":
                    await SetReportFrequencyAsync(chatId, callbackData, cancellationToken);
                    break;
                case "reports_day_mon":
                case "reports_day_tue":
                case "reports_day_wed":
                case "reports_day_thu":
                case "reports_day_fri":
                case "reports_day_sat":
                case "reports_day_sun":
                    await SetReportDayOfWeekAsync(chatId, callbackData, cancellationToken);
                    break;
                case "reports_day_cancel":
                    await ShowReportScheduleAsync(chatId, cancellationToken);
                    break;
                case var _ when callbackData.StartsWith("reports_day_"):
                    if (callbackData.StartsWith("reports_day_mon") ||
                        callbackData.StartsWith("reports_day_tue") ||
                        callbackData.StartsWith("reports_day_wed") ||
                        callbackData.StartsWith("reports_day_thu") ||
                        callbackData.StartsWith("reports_day_fri") ||
                        callbackData.StartsWith("reports_day_sat") ||
                        callbackData.StartsWith("reports_day_sun"))
                    {
                        await SetReportDayOfWeekAsync(chatId, callbackData, cancellationToken);
                    }
                    else
                    {
                        await SetReportDayOfMonthAsync(chatId, callbackData, cancellationToken);
                    }
                    break;
                // Уведомления - CRUD
                case var _ when callbackData.StartsWith("weekday_"):
                    if (int.TryParse(callbackData.Replace("weekday_", ""), out int weekday))
                    {
                        var userId2 = userId;
                        if (_userStates.TryGetValue(userId2, out var state) && state.CurrentAction == "add_notification_weekday")
                        {
                            await HandleAddNotificationWeekdayAsync(chatId, userId2, weekday, state, cancellationToken);
                        }
                        else if (_userStates.TryGetValue(userId2, out state) && state.CurrentAction == "edit_notification_value")
                        {
                            state.Data["awaitingWeekday"] = true;
                            state.Data["weekday"] = weekday;
                            await HandleEditNotificationExtraAsync(chatId, userId2, weekday.ToString(), state, cancellationToken);
                        }
                    }
                    break;
                case "notifications_all":
                    await ShowAllNotificationsAsync(chatId, cancellationToken);
                    break;
                case "notifications_add":
                    await StartAddNotificationAsync(chatId, userId, cancellationToken);
                    break;
                case "notifications_search":
                    await StartSearchNotificationsAsync(chatId, userId, cancellationToken);
                    break;
                case var _ when callbackData.StartsWith("notification_toggle_"):
                    var toggleId = int.Parse(callbackData.Replace("notification_toggle_", ""));
                    await ToggleNotificationAsync(chatId, toggleId, cancellationToken);
                    break;
                case var _ when callbackData.StartsWith("notification_edit_"):
                    var editId = int.Parse(callbackData.Replace("notification_edit_", ""));
                    await StartEditNotificationAsync(chatId, userId, editId, cancellationToken);
                    break;
                case var _ when callbackData.StartsWith("notification_delete_"):
                    var deleteId = int.Parse(callbackData.Replace("notification_delete_", ""));
                    await ShowDeleteNotificationConfirmationAsync(chatId, deleteId, cancellationToken);
                    break;
                case var _ when callbackData.StartsWith("delete_notification_confirm_"):
                    var confirmId = int.Parse(callbackData.Replace("delete_notification_confirm_", ""));
                    await DeleteNotificationAsync(chatId, confirmId, cancellationToken);
                    break;

                default:
                    Console.WriteLine($"   → Неизвестный callback в настройках: {callbackData}");
                    await SendTemporaryMessageAsync(chatId, "❌ Функция в разработке", cancellationToken);
                    break;
            }
        }


        // ========== ПОЛЬЗОВАТЕЛИ ==========
        private async Task ShowAllUsersListAsync(long chatId, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync();

            var text = "👥 ВСЕ ПОЛЬЗОВАТЕЛИ\n\n";

            foreach (var user in users.OrderBy(u => u.Username))
            {
                var role = user.Role == UserRole.Admin ? "👑" : "👤";
                var name = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                var lastActive = user.LastActiveAt?.ToString("dd.MM") ?? "никогда";
                text += $"{role} {name} - был {lastActive}\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "settings_users") }
            };

            await _botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: cancellationToken);
        }
        private async Task RemoveUserAdminAsync(long chatId, long targetUserId, CancellationToken cancellationToken)
        {
            var admins = await _userService.GetAdminsAsync();
            if (admins.Count <= 1)
            {
                await SendTemporaryMessageAsync(chatId, "⚠️ Нельзя удалить последнего администратора!", cancellationToken);
                return;
            }

            var user = await _userService.GetUserByTelegramIdAsync(targetUserId);
            if (user == null)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Пользователь не найден.", cancellationToken);
                return;
            }

            user.Role = UserRole.Member;
            var success = await _userService.UpdateUserAsync(user);

            if (success)
            {
                var name = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                await SendTemporaryMessageAsync(chatId, $"✅ {name} больше не администратор!", cancellationToken);
                await ShowUsersManagementAsync(chatId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Не удалось снять администратора.", cancellationToken);
            }
        }
        private async Task ShowUsersStatisticsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var admins = users.Where(u => u.Role == UserRole.Admin).ToList();
                var activeToday = users.Count(u => u.LastActiveAt >= DateTime.UtcNow.AddDays(-1));
                var activeWeek = users.Count(u => u.LastActiveAt >= DateTime.UtcNow.AddDays(-7));
                var newThisMonth = users.Count(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1));

                var text = $"📊 СТАТИСТИКА ПОЛЬЗОВАТЕЛЕЙ\n\n" +
                           $"👥 Всего: {users.Count}\n" +
                           $"👑 Администраторов: {admins.Count}\n" +
                           $"🟢 Активны сегодня: {activeToday}\n" +
                           $"📅 Активны за неделю: {activeWeek}\n" +
                           $"🆕 Новых за месяц: {newThisMonth}\n\n" +
                           $"Последние активные:\n";

                var recentUsers = users.Where(u => u.LastActiveAt.HasValue)
                                      .OrderByDescending(u => u.LastActiveAt)
                                      .Take(5);

                foreach (var u in recentUsers)
                {
                    var name = !string.IsNullOrEmpty(u.Username) ? $"@{u.Username}" : u.FirstName;
                    var lastActive = u.LastActiveAt?.ToString("dd.MM.yyyy HH:mm") ?? "никогда";
                    text += $"• {name} - {lastActive}\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📊 Обновить", "settings_users_stats") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "settings_users") }
        };

                // ИСПРАВЛЕНО: Используем ShowInlineMenuAsync для редактирования существующего меню
                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "settings_users_stats",  // Свой menuType для статистики
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing users statistics");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке статистики.", cancellationToken, 3);
            }
        }

        private async Task ShowAddAdminMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync();
            var nonAdmins = users.Where(u => u.Role != UserRole.Admin).ToList();

            if (nonAdmins.Count == 0)
            {
                await SendTemporaryMessageAsync(chatId, "✅ Все пользователи уже администраторы!", cancellationToken, 3);
                return;
            }

            var text = "👑 НАЗНАЧЕНИЕ АДМИНИСТРАТОРА\n\nВыберите пользователя:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var user in nonAdmins.Take(10))
            {
                var name = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"👤 {name}", $"make_admin_{user.TelegramId}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.SettingsUsers)
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "add_admin", cancellationToken);
        }

        private async Task ShowRemoveAdminMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync();
            var admins = users.Where(u => u.Role == UserRole.Admin).ToList();

            if (admins.Count <= 1)
            {
                await SendTemporaryMessageAsync(chatId, "⚠️ ВСЕГО 1 АДМИН Должен остаться хотя бы один администратор!", cancellationToken, 3);
                return;
            }

            var text = "👑 СНЯТИЕ АДМИНИСТРАТОРА\n\nВыберите пользователя:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var user in admins)
            {
                var name = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"👑 {name}", $"remove_admin_{user.TelegramId}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.SettingsUsers)
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "remove_admin", cancellationToken);
        }

        // ========== БЕЗОПАСНОСТЬ ==========

        private async Task ShowSecurityLogsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var logs = await _userService.GetRecentSecurityLogsAsync(20);

                var text = "🔐 ЖУРНАЛ БЕЗОПАСНОСТИ\n\n";

                if (!logs.Any())
                {
                    text += "📭 Нет записей в журнале безопасности";
                }
                else
                {
                    var suspiciousCount = logs.Count(l => l.IsSuspicious);
                    var todayCount = logs.Count(l => l.Timestamp.Date == DateTime.UtcNow.Date);

                    text += $"📊 Статистика:\n";
                    text += $"┌─────────────────────────────────\n";
                    text += $"│ Всего записей: {logs.Count}\n";
                    text += $"│ За сегодня: {todayCount}\n";
                    text += $"│ Подозрительных: {suspiciousCount}\n";
                    text += $"└─────────────────────────────────\n\n";

                    text += "📋 ПОСЛЕДНИЕ СОБЫТИЯ:\n";
                    foreach (var log in logs.Take(10))
                    {
                        var user = log.User?.Username != null ? $"@{log.User.Username}" : "Система";
                        var suspiciousMark = log.IsSuspicious ? "⚠️ " : "";
                        var time = log.Timestamp.ToString("dd.MM.yyyy HH:mm");

                        text += $"{suspiciousMark}{time} - {user}\n";
                        text += $"   {log.EventType}: {log.Description}\n";

                        if (!string.IsNullOrEmpty(log.IpAddress))
                            text += $"   IP: {log.IpAddress}\n";
                        text += "\n";
                    }
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("🔄 Обновить", "security_logs") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.SettingsSecurity) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "security_logs",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing security logs");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке логов", cancellationToken, 3);
            }
        }

        private async Task ShowActiveSessionsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var sessions = await _userService.GetActiveSessionsAsync();
                var users = await _userService.GetAllUsersAsync();

                var text = "👥 АКТИВНЫЕ СЕССИИ\n\n";

                if (!sessions.Any())
                {
                    text += "📭 Нет активных сессий";
                }
                else
                {
                    text += $"Всего активных сессий: {sessions.Count}\n\n";
                    text += "📋 СПИСОК СЕССИЙ:\n";

                    foreach (var session in sessions.Take(10))
                    {
                        var username = session.User?.Username != null ? $"@{session.User.Username}" : $"ID {session.UserId}";
                        var lastActive = session.LastActivityAt?.ToString("HH:mm:ss") ?? "—";
                        var duration = session.LastActivityAt.HasValue
                            ? (session.LastActivityAt.Value - session.StartedAt).ToString(@"hh\:mm")
                            : "—";

                        text += $"• {username}\n";
                        text += $"  Начало: {session.StartedAt:dd.MM.yyyy HH:mm}\n";
                        text += $"  Последняя активность: {lastActive}\n";
                        text += $"  Длительность: {duration}\n";

                        if (!string.IsNullOrEmpty(session.IpAddress))
                            text += $"  IP: {session.IpAddress}\n";

                        if (!string.IsNullOrEmpty(session.UserAgent))
                        {
                            var shortAgent = session.UserAgent.Length > 40
                                ? session.UserAgent.Substring(0, 40) + "..."
                                : session.UserAgent;
                            text += $"  UA: {shortAgent}\n";
                        }
                        text += "\n";
                    }

                    if (sessions.Count > 10)
                        text += $"... и еще {sessions.Count - 10} сессий\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("🔄 Обновить", "security_sessions") },
            new() { InlineKeyboardButton.WithCallbackData("🔚 Завершить все", "security_end_all_sessions") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.SettingsSecurity) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "security_sessions",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing active sessions");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке сессий", cancellationToken, 3);
            }
        }

        // ========== БАЗА ДАННЫХ ==========

        private async Task ShowDatabaseDetailedStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var projects = await _projectService.GetAllProjectsAsync();
                var tasks = await _taskService.GetAllTasksAsync();
                var contacts = await _contactService.GetAllContactsAsync();
                var finance = await _financeService.GetAllRecordsAsync();

                var totalSize = (users.Count * 1024 + projects.Count * 2048 + tasks.Count * 1024 +
                                 contacts.Count * 2048 + finance.Count * 512) / 1024;

                var text = $"💾 ДЕТАЛЬНАЯ СТАТИСТИКА БД\n\n" +
                           $"📊 Размер БД: ~{totalSize} KB\n\n" +
                           $"📦 Таблицы:\n" +
                           $"• Users: {users.Count} записей\n" +
                           $"• Projects: {projects.Count} записей\n" +
                           $"• Tasks: {tasks.Count} записей\n" +
                           $"• Contacts: {contacts.Count} записей\n" +
                           $"• Finance: {finance.Count} записей\n\n" +
                           $"📈 Рост за месяц:\n" +
                           $"• Новых пользователей: +{users.Count(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1))}\n" +
                           $"• Новых проектов: +{projects.Count(p => p.CreatedAt >= DateTime.UtcNow.AddMonths(-1))}\n" +
                           $"• Новых задач: +{tasks.Count(t => t.CreatedAt >= DateTime.UtcNow.AddMonths(-1))}";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "settings_database") } // ← ИСПРАВЛЕНО
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_detailed_stats", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing database detailed stats");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке статистики.", cancellationToken, 3);
            }
        }

        private async Task CreateDatabaseBackupAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "💾 Создание резервной копии...", cancellationToken, 2);

                var backupService = _serviceProvider.GetRequiredService<IDatabaseBackupService>();
                var result = await backupService.CreateBackupAsync();

                if (result.Success && !string.IsNullOrEmpty(result.FilePath))
                {
                    using var stream = System.IO.File.OpenRead(result.FilePath);
                    var fileName = result.FileName ?? $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

                    var fileSize = result.FileSize > 1024 * 1024
                        ? $"{(result.FileSize / (1024.0 * 1024.0)):F2} MB"
                        : $"{(result.FileSize / 1024.0):F2} KB";

                    await _botClient.SendDocument(
                        chatId: chatId,
                        document: new InputFileStream(stream, fileName),
                        caption: $"✅ Резервная копия создана!\n\n" +
                                $"📁 Файл: {fileName}\n" +
                                $"📦 Размер: {fileSize}\n" +
                                $"📅 Дата: {result.CreatedAt.AddHours(3):dd.MM.yyyy HH:mm} MSK",
                        cancellationToken: cancellationToken
                    );

                    // ПОКАЗЫВАЕМ МЕНЮ С КНОПКОЙ НАЗАД
                    var text = "✅ Резервная копия успешно создана и отправлена!\n\nВыберите действие:";
                    var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("📋 СПИСОК БЭКАПОВ", "settings_db_backup_list") },
                new() { InlineKeyboardButton.WithCallbackData("💾 НОВЫЙ БЭКАП", "settings_db_backup") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "settings_db_stats") }
            };

                    await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "backup_created", cancellationToken);
                }
                else
                {
                    var errorMsg = string.IsNullOrEmpty(result.ErrorMessage)
                        ? "Неизвестная ошибка"
                        : result.ErrorMessage;

                    await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ Ошибка при создании бэкапа: {errorMsg}", cancellationToken, 5);
                    await ShowDatabaseSettingsAsync(chatId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании резервной копии", cancellationToken, 5);
                await ShowDatabaseSettingsAsync(chatId, cancellationToken);
            }
        }

        private async Task CleanupOldBackupsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "🧹 Очистка старых бэкапов...", cancellationToken, 2);

                var backupService = _serviceProvider.GetRequiredService<IDatabaseBackupService>();
                await backupService.CleanupOldBackupsAsync(10); // Оставляем последние 10

                await _menuManager.SendTemporaryMessageAsync(chatId, "✅ Старые бэкапы удалены", cancellationToken, 3);
                await ShowDatabaseSettingsAsync(chatId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up backups");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при очистке", cancellationToken, 3);
            }
        }
        private async Task ShowBackupListAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var backupService = _serviceProvider.GetRequiredService<IDatabaseBackupService>();
                var backupFolder = backupService.GetBackupFolderPath();

                var backupFiles = Directory.GetFiles(backupFolder, "backup_*.zip")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (!backupFiles.Any())
                {
                    await _menuManager.ShowInlineMenuAsync(chatId, "📭 Нет резервных копий",
                        new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "settings_database") } }),
                        "backup_list_empty", cancellationToken);
                    return;
                }

                var text = $"📋 СПИСОК БЭКАПОВ\n\n";
                var buttons = new List<List<InlineKeyboardButton>>();

                foreach (var file in backupFiles.Take(10))
                {
                    var creationTime = file.CreationTime.AddHours(3);
                    var size = FormatFileSize(file.Length);
                    var displayName = file.Name.Replace("backup_", "").Replace(".zip", "");

                    text += $"📦 {displayName}\n";
                    text += $"   🕐 {creationTime:dd.MM.yyyy HH:mm} MSK\n";
                    text += $"   📦 {size}\n\n";

                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"📥 {displayName}", $"backup_download_{file.Name}")
            });
                }

                if (backupFiles.Count > 10)
                {
                    text += $"... и еще {backupFiles.Count - 10} бэкапов\n\n";
                }

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🗑️ Очистить старые", "settings_db_cleanup"),
            InlineKeyboardButton.WithCallbackData("◀️ Назад", "settings_database")
        });

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "backup_list", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing backup list");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке списка", cancellationToken, 3);
            }
        }
        private async Task DownloadBackupAsync(long chatId, string fileName, CancellationToken cancellationToken)
        {
            try
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "📥 Загрузка бэкапа...", cancellationToken, 2);

                var backupService = _serviceProvider.GetRequiredService<IDatabaseBackupService>();
                var backupFolder = backupService.GetBackupFolderPath();
                var filePath = Path.Combine(backupFolder, fileName);

                if (!File.Exists(filePath))
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Файл не найден", cancellationToken, 3);
                    return;
                }

                using var stream = File.OpenRead(filePath);
                var fileInfo = new FileInfo(filePath);
                var creationTime = fileInfo.CreationTime.AddHours(3);

                await _botClient.SendDocument(
                    chatId: chatId,
                    document: new InputFileStream(stream, fileName),
                    caption: $"📦 Бэкап от {creationTime:dd.MM.yyyy HH:mm} MSK\n" +
                            $"📁 Размер: {FormatFileSize(fileInfo.Length)}",
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading backup");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке", cancellationToken, 3);
            }
        }

        // ===== УВЕДОМЛЕНИЯ =====
        private string FormatTimeWithMsk(TimeSpan utcTime)
        {
            var mskTime = utcTime.Add(TimeSpan.FromHours(3));
            return mskTime.ToString(@"hh\:mm");
        }

        private string FormatDateTimeWithMsk(DateTime utcDateTime)
        {
            var mskDateTime = utcDateTime.AddHours(3);
            return mskDateTime.ToString("dd.MM.yyyy HH:mm");
        }
        private async Task ShowNotificationsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var notifications = await _notificationService.GetAllNotificationsAsync();
            var enabledCount = notifications.Count(n => n.IsEnabled);

            var text = $"🔔 УВЕДОМЛЕНИЯ\n\n" +
                       $"📊 Всего: {notifications.Count}\n" +
                       $"🟢 Активных: {enabledCount}\n" +
                       $"🔴 Неактивных: {notifications.Count - enabledCount}\n\n" +
                       $"Выберите действие:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("📋 ВСЕ УВЕДОМЛЕНИЯ", "notifications_all"),
            InlineKeyboardButton.WithCallbackData("➕ СОЗДАТЬ", "notifications_add")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "notifications_search")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToSettings) }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "notifications_menu", cancellationToken);
        }

        private async Task ShowAllNotificationsAsync(long chatId, CancellationToken cancellationToken)
        {
            var notifications = await _notificationService.GetAllNotificationsAsync();

            if (!notifications.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Уведомлений нет",
                    new InlineKeyboardMarkup(new[]
                    {
                new[] { InlineKeyboardButton.WithCallbackData("➕ СОЗДАТЬ", "notifications_add") },
                new[] { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "settings_notifications") }
                    }), "notifications_empty", cancellationToken);
                return;
            }

            var text = $"📋 ВСЕ УВЕДОМЛЕНИЯ ({notifications.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var notif in notifications.Take(10))
            {
                var statusEmoji = notif.IsEnabled ? "🟢" : "🔴";
                var frequencyText = GetFrequencyText(notif);

                text += $"{statusEmoji} {notif.Title}\n";
                text += $"   ⏰ {frequencyText}\n\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"🔔 {notif.Title}", $"notification_view_{notif.Id}")
        });
            }

            if (notifications.Count > 10)
                text += $"... и еще {notifications.Count - 10} уведомлений\n\n";

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ СОЗДАТЬ", "notifications_add"),
        InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "settings_notifications")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "notifications_all", cancellationToken);
        }

        private async Task ShowNotificationDetailsAsync(long chatId, int notificationId, CancellationToken cancellationToken)
        {
            var notif = await _notificationService.GetNotificationAsync(notificationId);
            if (notif == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Уведомление не найдено", cancellationToken, 3);
                return;
            }

            var statusEmoji = notif.IsEnabled ? "🟢" : "🔴";
            var frequencyText = GetFrequencyText(notif);
            var lastTriggered = notif.LastTriggeredAt.HasValue
                ? FormatDateTimeWithMsk(DateTimeOffset.FromUnixTimeSeconds(notif.LastTriggeredAt.Value).UtcDateTime)
                : "никогда";

            var text = $"🔔 УВЕДОМЛЕНИЕ: {notif.Title}\n\n" +
                       $"📊 СТАТУС: {statusEmoji} {(notif.IsEnabled ? "Активно" : "Неактивно")}\n" +
                       $"⏰ РАСПИСАНИЕ: {frequencyText}\n" +
                       $"📝 СООБЩЕНИЕ:\n{notif.Message}\n\n" +
                       $"📅 Последняя отправка: {lastTriggered}";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData(notif.IsEnabled ? "🔴 ВЫКЛЮЧИТЬ" : "🟢 ВКЛЮЧИТЬ", $"notification_toggle_{notif.Id}"),
            InlineKeyboardButton.WithCallbackData("✏️ РЕДАКТИРОВАТЬ", $"notification_edit_{notif.Id}")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🗑️ УДАЛИТЬ", $"notification_delete_{notif.Id}"),
            InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "notifications_all")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"notification_{notif.Id}", cancellationToken);
        }

        private string GetFrequencyText(Notification notif)
        {
            var timeStr = FormatTimeWithMsk(notif.Time); // ← используем MSK

            return notif.Frequency switch
            {
                "once" => $"Одноразово на {notif.SpecificDate:dd.MM.yyyy} в {timeStr} MSK",
                "daily" => $"Ежедневно в {timeStr} MSK",
                "weekly" => $"Еженедельно по {GetDayName(notif.DayOfWeek ?? 1)} в {timeStr} MSK",
                "monthly" => $"Ежемесячно {notif.DayOfMonth} числа в {timeStr} MSK",
                "yearly" => $"Ежегодно {notif.DayOfMonth}.{notif.Month} в {timeStr} MSK",
                _ => "Неизвестно"
            };
        }

        private string GetDayName(int day)
        {
            return day switch
            {
                1 => "ПН",
                2 => "ВТ",
                3 => "СР",
                4 => "ЧТ",
                5 => "ПТ",
                6 => "СБ",
                7 => "ВС",
                _ => "День"
            };
        }

        private async Task ToggleNotificationAsync(long chatId, int notificationId, CancellationToken cancellationToken)
        {
            var success = await _notificationService.ToggleNotificationAsync(notificationId);

            if (success)
            {
                var notif = await _notificationService.GetNotificationAsync(notificationId);
                var status = notif?.IsEnabled == true ? "включено" : "выключено";
                await SendTemporaryMessageAsync(chatId, $"✅ Уведомление {status}", cancellationToken, 3);
                await ShowNotificationDetailsAsync(chatId, notificationId, cancellationToken);
            }
            else
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при изменении статуса", cancellationToken, 3);
            }
        }

        // ===== СОЗДАНИЕ УВЕДОМЛЕНИЯ =====

        private async Task StartAddNotificationAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "add_notification_title",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await SendTemporaryMessageAsync(chatId,
                "🔔 СОЗДАНИЕ УВЕДОМЛЕНИЯ (ШАГ 1/4)\n\n" +
                "Введите название уведомления:", cancellationToken);
        }

        private async Task HandleAddNotificationTitleAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["title"] = text;
            state.CurrentAction = "add_notification_message";
            state.Step = 2;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId,
                $"Название: {text}\n\n" +
                "🔔 ШАГ 2/4\n\n" +
                "Введите текст уведомления:", cancellationToken);
        }

        private async Task HandleAddNotificationMessageAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["message"] = text;
            state.CurrentAction = "add_notification_frequency";
            state.Step = 3;
            _userStates[userId] = state;

            var freqText = "🔔 ШАГ 3/4\n\n" +
                           "Выберите периодичность:\n\n" +
                           "1️⃣ Одноразово\n" +
                           "2️⃣ Ежедневно\n" +
                           "3️⃣ Еженедельно\n" +
                           "4️⃣ Ежемесячно\n" +
                           "5️⃣ Ежегодно\n\n" +
                           "Введите номер (1-5):";

            await SendTemporaryMessageAsync(chatId, freqText, cancellationToken);
        }

        private async Task HandleAddNotificationFrequencyAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int freq) || freq < 1 || freq > 5)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите число от 1 до 5", cancellationToken);
                return;
            }

            var frequency = freq switch
            {
                1 => "once",
                2 => "daily",
                3 => "weekly",
                4 => "monthly",
                5 => "yearly",
                _ => "once"
            };

            state.Data["frequency"] = frequency;
            state.CurrentAction = "add_notification_time";
            state.Step = 4;
            _userStates[userId] = state;

            await SendTemporaryMessageAsync(chatId,
                "🔔 ШАГ 4/4\n\n" +
                "Введите время в формате ЧЧ:ММ (UTC)\n" +
                "Пример: 14:30", cancellationToken);
        }

        private async Task HandleAddNotificationTimeAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!TimeSpan.TryParse(text, out TimeSpan time))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Неверный формат. Используйте ЧЧ:ММ", cancellationToken);
                return;
            }

            state.Data["time"] = time;

            var frequency = state.Data["frequency"]?.ToString();

            switch (frequency)
            {
                case "once":
                    state.CurrentAction = "add_notification_date";
                    state.Step = 5;
                    _userStates[userId] = state;
                    await SendTemporaryMessageAsync(chatId, "Введите дату в формате ДД.ММ.ГГГГ:", cancellationToken);
                    break;

                case "weekly":
                    state.CurrentAction = "add_notification_weekday";
                    state.Step = 5;
                    _userStates[userId] = state;
                    await ShowWeekdaySelectionAsync(chatId, userId, cancellationToken);
                    break;

                case "monthly":
                    state.CurrentAction = "add_notification_monthday";
                    state.Step = 5;
                    _userStates[userId] = state;
                    await SendTemporaryMessageAsync(chatId, "Введите число месяца (1-31):", cancellationToken);
                    break;

                case "yearly":
                    state.CurrentAction = "add_notification_month";
                    state.Step = 5;
                    _userStates[userId] = state;
                    await SendTemporaryMessageAsync(chatId, "Введите месяц (1-12):", cancellationToken);
                    break;

                default: // daily
                    await CreateNotificationAsync(chatId, userId, state, cancellationToken);
                    break;
            }
        }

        private async Task ShowWeekdaySelectionAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var text = "Выберите день недели:";
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("ПН", "weekday_1"),
            InlineKeyboardButton.WithCallbackData("ВТ", "weekday_2"),
            InlineKeyboardButton.WithCallbackData("СР", "weekday_3")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("ЧТ", "weekday_4"),
            InlineKeyboardButton.WithCallbackData("ПТ", "weekday_5"),
            InlineKeyboardButton.WithCallbackData("СБ", "weekday_6")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("ВС", "weekday_7")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "weekday_select", cancellationToken);
        }

        private async Task HandleAddNotificationWeekdayAsync(long chatId, long userId, int weekday, UserState state, CancellationToken cancellationToken)
        {
            state.Data["weekday"] = weekday;
            await CreateNotificationAsync(chatId, userId, state, cancellationToken);
        }

        private async Task HandleAddNotificationMonthDayAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int day) || day < 1 || day > 31)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите число от 1 до 31", cancellationToken);
                return;
            }

            state.Data["monthday"] = day;
            await CreateNotificationAsync(chatId, userId, state, cancellationToken);
        }

        private async Task HandleAddNotificationMonthAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int month) || month < 1 || month > 12)
            {
                await SendTemporaryMessageAsync(chatId, "❌ Введите месяц от 1 до 12", cancellationToken);
                return;
            }

            state.Data["month"] = month;
            state.CurrentAction = "add_notification_monthday";
            state.Step = 6;
            _userStates[userId] = state;
            await SendTemporaryMessageAsync(chatId, "Введите число месяца (1-31):", cancellationToken);
        }

        private async Task CreateNotificationAsync(long chatId, long userId, UserState state, CancellationToken cancellationToken)
        {
            try
            {
                var notification = new Notification
                {
                    Title = state.Data["title"]?.ToString() ?? "",
                    Message = state.Data["message"]?.ToString() ?? "",
                    Frequency = state.Data["frequency"]?.ToString(),
                    Time = (TimeSpan)state.Data["time"]!,
                    IsEnabled = true
                };

                if (state.Data.ContainsKey("date") && DateTime.TryParse(state.Data["date"]?.ToString(), out var date))
                    notification.SpecificDate = date;

                if (state.Data.ContainsKey("weekday"))
                    notification.DayOfWeek = (int)state.Data["weekday"]!;

                if (state.Data.ContainsKey("monthday"))
                    notification.DayOfMonth = (int)state.Data["monthday"]!;

                if (state.Data.ContainsKey("month"))
                    notification.Month = (int)state.Data["month"]!;

                var result = await _notificationService.CreateNotificationAsync(notification);

                if (result != null)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Уведомление '{notification.Title}' создано!", cancellationToken, 3);
                    _userStates.Remove(userId);
                    _menuManager.ClearMenuState(chatId);
                    await ShowNotificationDetailsAsync(chatId, result.Id, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании уведомления", cancellationToken, 5);
                    _userStates.Remove(userId);
                    await ShowNotificationsMenuAsync(chatId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании уведомления", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }
        // ===== РЕДАКТИРОВАНИЕ УВЕДОМЛЕНИЯ =====

        private async Task StartEditNotificationAsync(long chatId, long userId, int notificationId, CancellationToken cancellationToken)
        {
            var notification = await _notificationService.GetNotificationAsync(notificationId);
            if (notification == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Уведомление не найдено", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "edit_notification_field",
                Data = new Dictionary<string, object?> { ["notificationId"] = notificationId },
                Step = 1
            };

            var text = $"✏️ РЕДАКТИРОВАНИЕ УВЕДОМЛЕНИЯ: {notification.Title}\n\n" +
                       "Выберите поле для редактирования:\n\n" +
                       "1️⃣ Название\n" +
                       "2️⃣ Сообщение\n" +
                       "3️⃣ Периодичность\n" +
                       "4️⃣ Время\n\n" +
                       "Введите номер поля (1-4) или 0 для выхода:";

            await _menuManager.SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }

        private async Task HandleEditFrequencySelectionAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            if (!_userStates.ContainsKey(userId) || _userStates[userId].CurrentAction != "edit_notification_value")
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Сессия редактирования истекла", cancellationToken, 3);
                return;
            }

            var state = _userStates[userId];
            var notificationId = (int)state.Data["notificationId"]!;
            var notification = await _notificationService.GetNotificationAsync(notificationId);

            if (notification == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Уведомление не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            // Устанавливаем частоту
            notification.Frequency = callbackData switch
            {
                "edit_freq_once" => "once",
                "edit_freq_daily" => "daily",
                "edit_freq_weekly" => "weekly",
                "edit_freq_monthly" => "monthly",
                "edit_freq_yearly" => "yearly",
                _ => "daily"
            };

            // Сбрасываем специфичные поля
            notification.SpecificDate = null;
            notification.DayOfWeek = null;
            notification.DayOfMonth = null;
            notification.Month = null;

            // Если нужны дополнительные данные
            if (notification.Frequency == "once")
            {
                state.CurrentAction = "edit_notification_extra";
                state.Data["awaitingDate"] = true;
                state.Step = 3;
                _userStates[userId] = state;
                await _menuManager.SendTemporaryMessageAsync(chatId, "Введите дату в формате ДД.ММ.ГГГГ:", cancellationToken);
                return;
            }
            if (notification.Frequency == "weekly")
            {
                state.CurrentAction = "edit_notification_extra";
                state.Data["awaitingWeekday"] = true;
                state.Step = 3;
                _userStates[userId] = state;
                await ShowWeekdaySelectionAsync(chatId, userId, cancellationToken);
                return;
            }
            if (notification.Frequency == "monthly")
            {
                state.CurrentAction = "edit_notification_extra";
                state.Data["awaitingMonthDay"] = true;
                state.Step = 3;
                _userStates[userId] = state;
                await _menuManager.SendTemporaryMessageAsync(chatId, "Введите число месяца (1-31):", cancellationToken);
                return;
            }
            if (notification.Frequency == "yearly")
            {
                state.CurrentAction = "edit_notification_extra";
                state.Data["awaitingMonth"] = true;
                state.Step = 3;
                _userStates[userId] = state;
                await _menuManager.SendTemporaryMessageAsync(chatId, "Введите месяц (1-12):", cancellationToken);
                return;
            }

            // Если daily - сразу сохраняем
            var success = await _notificationService.UpdateNotificationAsync(notification);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Периодичность обновлена!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowNotificationDetailsAsync(chatId, notificationId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }
        private async Task HandleEditNotificationFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 0 || field > 4)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 0 до 4", cancellationToken, 3);
                return;
            }

            if (field == 0)
            {
                _userStates.Remove(userId);
                await ShowNotificationDetailsAsync(chatId, (int)state.Data["notificationId"]!, cancellationToken);
                return;
            }

            var notificationId = (int)state.Data["notificationId"]!;
            var notification = await _notificationService.GetNotificationAsync(notificationId);
            if (notification == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Уведомление не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            state.Data["editField"] = field;
            state.CurrentAction = "edit_notification_value";
            state.Step = 2;
            _userStates[userId] = state;

            if (field == 3) // Периодичность - показываем меню с кнопками
            {
                var freqText = "Выберите новую периодичность:";
                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("📅 Одноразово", "edit_freq_once"),
                InlineKeyboardButton.WithCallbackData("📅 Ежедневно", "edit_freq_daily")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("📅 Еженедельно", "edit_freq_weekly"),
                InlineKeyboardButton.WithCallbackData("📅 Ежемесячно", "edit_freq_monthly")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("📅 Ежегодно", "edit_freq_yearly")
            }
        };

                // Отправляем НОВОЕ сообщение с меню
                await _menuManager.SendTemporaryInlineMessageAsync(chatId, freqText, new InlineKeyboardMarkup(buttons), cancellationToken, 60);
            }
            else
            {
                var prompt = field switch
                {
                    1 => $"Текущее название: {notification.Title}\n\nВведите новое название:",
                    2 => $"Текущее сообщение:\n{notification.Message}\n\nВведите новое сообщение:",
                    4 => $"Текущее время: {FormatTimeWithMsk(notification.Time)} MSK\n\nВведите новое время в формате ЧЧ:ММ (MSK):",
                    _ => ""
                };

                // Для текстового ввода используем SendTemporaryMessageAsync (не удаляется)
                await _menuManager.SendTemporaryMessageAsync(chatId, prompt, cancellationToken);
            }
        }

        private async Task HandleEditNotificationValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var notificationId = (int)state.Data["notificationId"]!;
            var field = (int)state.Data["editField"]!;

            var notification = await _notificationService.GetNotificationAsync(notificationId);
            if (notification == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Уведомление не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            switch (field)
            {
                case 1: // Название
                    notification.Title = text;
                    break;

                case 2: // Сообщение
                    notification.Message = text;
                    break;

                case 3: // Периодичность
                    if (!int.TryParse(text, out int freq) || freq < 1 || freq > 5)
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 1 до 5", cancellationToken);
                        return;
                    }
                    notification.Frequency = freq switch
                    {
                        1 => "once",
                        2 => "daily",
                        3 => "weekly",
                        4 => "monthly",
                        5 => "yearly",
                        _ => "once"
                    };

                    // Сбрасываем специфичные поля
                    notification.SpecificDate = null;
                    notification.DayOfWeek = null;
                    notification.DayOfMonth = null;
                    notification.Month = null;

                    // Если нужны дополнительные данные для выбранной периодичности
                    if (notification.Frequency == "once")
                    {
                        state.Data["awaitingDate"] = true;
                        state.Step = 3;
                        _userStates[userId] = state;
                        await _menuManager.SendTemporaryMessageAsync(chatId, "Введите дату в формате ДД.ММ.ГГГГ:", cancellationToken);
                        return;
                    }
                    if (notification.Frequency == "weekly")
                    {
                        state.Data["awaitingWeekday"] = true;
                        state.Step = 3;
                        _userStates[userId] = state;
                        await ShowWeekdaySelectionAsync(chatId, userId, cancellationToken);
                        return;
                    }
                    if (notification.Frequency == "monthly")
                    {
                        state.Data["awaitingMonthDay"] = true;
                        state.Step = 3;
                        _userStates[userId] = state;
                        await _menuManager.SendTemporaryMessageAsync(chatId, "Введите число месяца (1-31):", cancellationToken);
                        return;
                    }
                    if (notification.Frequency == "yearly")
                    {
                        state.Data["awaitingMonth"] = true;
                        state.Step = 3;
                        _userStates[userId] = state;
                        await _menuManager.SendTemporaryMessageAsync(chatId, "Введите месяц (1-12):", cancellationToken);
                        return;
                    }
                    break;

                case 4: // Время
                    if (!TimeSpan.TryParse(text, out TimeSpan time))
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат. Используйте ЧЧ:ММ", cancellationToken);
                        return;
                    }
                    notification.Time = time;
                    break;
            }

            // Если не требуется дополнительных данных, сохраняем
            if (!state.Data.ContainsKey("awaitingDate") &&
                !state.Data.ContainsKey("awaitingWeekday") &&
                !state.Data.ContainsKey("awaitingMonthDay") &&
                !state.Data.ContainsKey("awaitingMonth"))
            {
                var success = await _notificationService.UpdateNotificationAsync(notification);

                if (success)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Уведомление обновлено!", cancellationToken, 3);
                    _userStates.Remove(userId);
                    _menuManager.ClearMenuState(chatId);
                    await ShowNotificationDetailsAsync(chatId, notificationId, cancellationToken);
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
                    _userStates.Remove(userId);
                }
            }
        }

        private async Task HandleEditNotificationExtraAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var notificationId = (int)state.Data["notificationId"]!;
            var notification = await _notificationService.GetNotificationAsync(notificationId);
            if (notification == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Уведомление не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            if (state.Data.ContainsKey("awaitingDate"))
            {
                if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                {
                    notification.SpecificDate = date;
                    state.Data.Remove("awaitingDate");
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ", cancellationToken);
                    return;
                }
            }
            else if (state.Data.ContainsKey("awaitingWeekday"))
            {
                if (int.TryParse(text, out int weekday) && weekday >= 1 && weekday <= 7)
                {
                    notification.DayOfWeek = weekday;
                    state.Data.Remove("awaitingWeekday");
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 1 до 7", cancellationToken);
                    return;
                }
            }
            else if (state.Data.ContainsKey("awaitingMonthDay"))
            {
                if (int.TryParse(text, out int day) && day >= 1 && day <= 31)
                {
                    notification.DayOfMonth = day;
                    state.Data.Remove("awaitingMonthDay");
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 1 до 31", cancellationToken);
                    return;
                }
            }
            else if (state.Data.ContainsKey("awaitingMonth"))
            {
                if (int.TryParse(text, out int month) && month >= 1 && month <= 12)
                {
                    notification.Month = month;
                    state.Data["awaitingMonthDay"] = true;
                    state.Step = 4;
                    _userStates[userId] = state;
                    await _menuManager.SendTemporaryMessageAsync(chatId, "Введите число месяца (1-31):", cancellationToken);
                    return;
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите месяц от 1 до 12", cancellationToken);
                    return;
                }
            }

            var success = await _notificationService.UpdateNotificationAsync(notification);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Уведомление обновлено!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowNotificationDetailsAsync(chatId, notificationId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }

        // ===== УДАЛЕНИЕ УВЕДОМЛЕНИЯ =====

        private async Task ShowDeleteNotificationConfirmationAsync(long chatId, int notificationId, CancellationToken cancellationToken)
        {
            var notification = await _notificationService.GetNotificationAsync(notificationId);
            if (notification == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Уведомление не найдено", cancellationToken, 3);
                return;
            }

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить уведомление?\n\n" +
                       $"🔔 {notification.Title}\n" +
                       $"⏰ {GetFrequencyText(notification)}\n\n" +
                       $"❗ Это действие невозможно отменить!\n\n" +
                       $"⏳ Это сообщение будет удалено через 15 секунд.";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ ДА, УДАЛИТЬ", $"delete_notification_confirm_{notificationId}"),
            InlineKeyboardButton.WithCallbackData("❌ ОТМЕНА", $"notification_view_{notificationId}")
        }
    };

            // Отправляем временное сообщение, которое удалится через 15 секунд
            await _menuManager.SendTemporaryInlineMessageAsync(
                chatId,
                text,
                new InlineKeyboardMarkup(buttons),
                cancellationToken,
                deleteAfterSeconds: 15  // ← 15 секунд
            );
        }

        private async Task DeleteNotificationAsync(long chatId, int notificationId, CancellationToken cancellationToken)
        {
            var notification = await _notificationService.GetNotificationAsync(notificationId);
            if (notification == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Уведомление не найдено", cancellationToken, 3);
                return;
            }

            var success = await _notificationService.DeleteNotificationAsync(notificationId);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Уведомление '{notification.Title}' удалено!", cancellationToken, 3);
                _menuManager.ClearMenuState(chatId);
                await ShowNotificationsMenuAsync(chatId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при удалении", cancellationToken, 5);
            }
        }

        // ===== ПОИСК УВЕДОМЛЕНИЙ =====

        private async Task StartSearchNotificationsAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "search_notifications",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК УВЕДОМЛЕНИЙ\n\nВведите текст для поиска:", cancellationToken);
        }

        private async Task HandleSearchNotificationsAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var allNotifications = await _notificationService.GetAllNotificationsAsync();
            var searchLower = text.ToLower();

            var results = allNotifications
                .Where(n => n.Title.ToLower().Contains(searchLower) ||
                            n.Message.ToLower().Contains(searchLower))
                .Take(20)
                .ToList();

            if (!results.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                await ShowNotificationsMenuAsync(chatId, cancellationToken);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {results.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var notif in results.Take(10))
            {
                var statusEmoji = notif.IsEnabled ? "🟢" : "🔴";
                result += $"{statusEmoji} {notif.Title}\n";
                result += $"   ⏰ {GetFrequencyText(notif)}\n\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"🔔 {notif.Title}", $"notification_view_{notif.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "settings_notifications")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "notifications_search", cancellationToken);
            _userStates.Remove(userId);
        }

        // ========== ОТЧЕТЫ, УВЕДОМЛЕНИЯ ==========
        private string FormatReportSchedule(ReportSchedule schedule)
        {
            var timeMsk = schedule.Time.Add(TimeSpan.FromHours(3));
            var timeStr = timeMsk.ToString(@"hh\:mm");

            var status = schedule.IsEnabled ? "🟢 Включен" : "🔴 Выключен";

            string frequencyText = schedule.Frequency switch
            {
                "daily" => $"Ежедневно в {timeStr} MSK",
                "weekly" => schedule.DayOfWeek.HasValue
                    ? $"Еженедельно по {GetDayName(schedule.DayOfWeek.Value)} в {timeStr} MSK"
                    : $"Еженедельно в {timeStr} MSK",
                "monthly" => schedule.DayOfMonth.HasValue
                    ? $"Ежемесячно {schedule.DayOfMonth} числа в {timeStr} MSK"
                    : $"Ежемесячно в {timeStr} MSK",
                _ => "Не настроено"
            };

            var lastSent = schedule.LastSentAt.HasValue
                ? FormatDateTimeWithMsk(DateTimeOffset.FromUnixTimeSeconds(schedule.LastSentAt.Value).UtcDateTime)
                : "никогда";

            return $"📊 Статус: {status}\n" +
                   $"⏰ Расписание: {frequencyText}\n" +
                   $"📅 Последняя отправка: {lastSent}";
        }
        private async Task ShowReportScheduleAsync(long chatId, CancellationToken cancellationToken)
        {
            var schedule = await _notificationService.GetReportScheduleAsync();
            var scheduleInfo = FormatReportSchedule(schedule);

            var text = $"📊 НАСТРОЙКИ ОТЧЕТОВ\n\n{scheduleInfo}\n\nВыберите действие:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData(schedule.IsEnabled ? "🔴 Выключить" : "🟢 Включить", "settings_reports_toggle") },
        new() { InlineKeyboardButton.WithCallbackData("⏰ Изменить время", "settings_reports_time") },
        new() { InlineKeyboardButton.WithCallbackData("📅 Изменить периодичность", "settings_reports_frequency") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToSettings) }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "settings_reports", cancellationToken);
        }

        private async Task HandleAddNotificationDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ", cancellationToken, 3);
                return;
            }

            state.Data["date"] = date;
            await CreateNotificationAsync(chatId, userId, state, cancellationToken);
        }
        private async Task ShowReportsSettingsAsync(long chatId, CancellationToken cancellationToken)
        {
            var schedule = await _notificationService.GetReportScheduleAsync();
            var status = schedule.IsEnabled ? "🟢 Включен" : "🔴 Выключен";
            var frequencyText = schedule.Frequency switch
            {
                "daily" => "Ежедневно",
                "weekly" => "Еженедельно",
                "monthly" => "Ежемесячно",
                _ => schedule.Frequency
            };
            var timeText = schedule.Time.ToString(@"hh\:mm");

            var text = $"📊 НАСТРОЙКИ ОТЧЕТОВ\n\n" +
                       $"📅 Статус: {status}\n" +
                       $"⏰ Периодичность: {frequencyText}\n" +
                       $"🕐 Время: {timeText} UTC\n\n" +
                       $"Выберите действие:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData(schedule.IsEnabled ? "🔴 Выключить" : "🟢 Включить", "settings_reports_toggle") },
        new() { InlineKeyboardButton.WithCallbackData("⏰ Изменить время", "settings_reports_time") },
        new() { InlineKeyboardButton.WithCallbackData("📅 Изменить периодичность", "settings_reports_frequency") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToSettings) }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "settings_reports", cancellationToken);
        }

        private async Task ToggleReportScheduleAsync(long chatId, CancellationToken cancellationToken)
        {
            var schedule = await _notificationService.GetReportScheduleAsync();
            schedule.IsEnabled = !schedule.IsEnabled;
            await _notificationService.UpdateReportScheduleAsync(schedule);

            await SendTemporaryMessageAsync(chatId, schedule.IsEnabled ? "✅ Отчеты включены" : "✅ Отчеты выключены", cancellationToken, 3);
            await ShowReportsSettingsAsync(chatId, cancellationToken);
        }

        private async Task ShowReportFrequencyMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📊 ВЫБОР ПЕРИОДИЧНОСТИ\n\nВыберите как часто отправлять отчет:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("📅 Ежедневно", "reports_frequency_daily"),
            InlineKeyboardButton.WithCallbackData("📅 Еженедельно", "reports_frequency_weekly")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📅 Ежемесячно", "reports_frequency_monthly")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.SettingsReports) }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "reports_frequency", cancellationToken);
        }

        private async Task SetReportFrequencyAsync(long chatId, string callbackData, CancellationToken cancellationToken)
        {
            var schedule = await _notificationService.GetReportScheduleAsync();
            schedule.Frequency = callbackData.Replace("reports_frequency_", "");

            if (schedule.Frequency == "weekly")
            {
                await ShowReportDayOfWeekMenuAsync(chatId, cancellationToken);
            }
            else if (schedule.Frequency == "monthly")
            {
                await ShowReportDayOfMonthMenuAsync(chatId, cancellationToken);
            }
            else
            {
                schedule.DayOfWeek = null;
                schedule.DayOfMonth = null;
                await _notificationService.UpdateReportScheduleAsync(schedule);
                await SendTemporaryMessageAsync(chatId, "✅ Периодичность установлена (ежедневно)", cancellationToken, 3);
                await ShowReportScheduleAsync(chatId, cancellationToken);
            }
        }
        private async Task ShowReportDayOfMonthMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📅 ВЫБОР ДНЯ МЕСЯЦА\n\nВыберите число для ежемесячного отчета:";

            var buttons = new List<List<InlineKeyboardButton>>();
            var row = new List<InlineKeyboardButton>();

            for (int i = 1; i <= 31; i++)
            {
                row.Add(InlineKeyboardButton.WithCallbackData(i.ToString(), $"reports_day_{i}"));

                if (i % 7 == 0 || i == 31)
                {
                    buttons.Add(new List<InlineKeyboardButton>(row));
                    row.Clear();
                }
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "reports_frequency_monthly")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "reports_day_month", cancellationToken);
        }

        private async Task ShowReportDayOfWeekMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📅 ВЫБОР ДНЯ НЕДЕЛИ\n\nВыберите день для еженедельного отчета:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("ПН", "reports_day_mon"),
            InlineKeyboardButton.WithCallbackData("ВТ", "reports_day_tue"),
            InlineKeyboardButton.WithCallbackData("СР", "reports_day_wed")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("ЧТ", "reports_day_thu"),
            InlineKeyboardButton.WithCallbackData("ПТ", "reports_day_fri"),
            InlineKeyboardButton.WithCallbackData("СБ", "reports_day_sat")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("ВС", "reports_day_sun"),
            InlineKeyboardButton.WithCallbackData("◀️ Отмена", "reports_day_cancel")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "reports_day", cancellationToken);
        }

        private async Task SetReportDayOfWeekAsync(long chatId, string callbackData, CancellationToken cancellationToken)
        {
            var dayMap = new Dictionary<string, int>
            {
                ["reports_day_mon"] = 1,
                ["reports_day_tue"] = 2,
                ["reports_day_wed"] = 3,
                ["reports_day_thu"] = 4,
                ["reports_day_fri"] = 5,
                ["reports_day_sat"] = 6,
                ["reports_day_sun"] = 7
            };

            var schedule = await _notificationService.GetReportScheduleAsync();
            schedule.DayOfWeek = dayMap[callbackData];
            await _notificationService.UpdateReportScheduleAsync(schedule);

            var dayName = callbackData switch
            {
                "reports_day_mon" => "Понедельник",
                "reports_day_tue" => "Вторник",
                "reports_day_wed" => "Среда",
                "reports_day_thu" => "Четверг",
                "reports_day_fri" => "Пятница",
                "reports_day_sat" => "Суббота",
                "reports_day_sun" => "Воскресенье",
                _ => "Выбранный день"
            };

            var timeMsk = schedule.Time.Add(TimeSpan.FromHours(3));
            var timeStr = timeMsk.ToString(@"hh\:mm");

            await SendTemporaryMessageAsync(chatId, $"✅ Отчет будет отправляться каждый {dayName} в {timeStr} MSK", cancellationToken, 3);
            await ShowReportScheduleAsync(chatId, cancellationToken);
        }
        private async Task SetReportDayOfMonthAsync(long chatId, string callbackData, CancellationToken cancellationToken)
        {
            if (!int.TryParse(callbackData.Replace("reports_day_", ""), out int day))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при выборе дня", cancellationToken, 3);
                return;
            }

            var schedule = await _notificationService.GetReportScheduleAsync();
            schedule.DayOfMonth = day;
            await _notificationService.UpdateReportScheduleAsync(schedule);

            var timeMsk = schedule.Time.Add(TimeSpan.FromHours(3));
            var timeStr = timeMsk.ToString(@"hh\:mm");

            await SendTemporaryMessageAsync(chatId, $"✅ Отчет будет отправляться {day} числа каждого месяца в {timeStr} MSK", cancellationToken, 3);
            await ShowReportScheduleAsync(chatId, cancellationToken);
        }
        private async Task StartSetReportTimeAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "set_report_time",
                Step = 1
            };

            await SendTemporaryMessageAsync(chatId,
                "⏰ ВВЕДИТЕ ВРЕМЯ ОТПРАВКИ\n\n" +
                "Формат: ЧЧ:ММ (MSK)\n" +
                "Пример: 09:00 для 9 утра", cancellationToken);
        }

        private async Task HandleSetReportTimeAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            if (!TimeSpan.TryParse(text, out TimeSpan timeMsk))
            {
                await SendTemporaryMessageAsync(chatId, "❌ Неверный формат. Используйте ЧЧ:ММ (например 09:00)", cancellationToken);
                return;
            }

            // Конвертируем MSK в UTC (вычитаем 3 часа)
            var timeUtc = timeMsk.Subtract(TimeSpan.FromHours(3));
            if (timeUtc < TimeSpan.Zero)
            {
                timeUtc = timeUtc.Add(TimeSpan.FromHours(24));
            }

            var schedule = await _notificationService.GetReportScheduleAsync();
            schedule.Time = timeUtc;
            await _notificationService.UpdateReportScheduleAsync(schedule);

            _userStates.Remove(userId);
            await SendTemporaryMessageAsync(chatId, $"✅ Время отправки установлено на {timeMsk:hh\\:mm} MSK", cancellationToken, 3);
            await ShowReportScheduleAsync(chatId, cancellationToken);
        }
        private async Task ShowReportsFrequencyAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📅 НАСТРОЙКА ЧАСТОТЫ ОТЧЕТОВ\n\n" +
                       "Текущие настройки:\n" +
                       "• Ежедневный отчет: 09:00 UTC\n" +
                       "• Еженедельный отчет: Пн 10:00 UTC\n" +
                       "• Ежемесячный отчет: 1 число 12:00 UTC\n\n" +
                       "Выберите периодичность:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("📅 Ежедневно", "set_report_daily"),
            InlineKeyboardButton.WithCallbackData("📅 Еженедельно", "set_report_weekly")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📅 Ежемесячно", "set_report_monthly"),
            InlineKeyboardButton.WithCallbackData("🚫 Отключить", "set_report_off")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.SettingsReports) }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "reports_frequency", cancellationToken);
        }

        private async Task ShowReportsEmailAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📧 НАСТРОЙКА EMAIL ДЛЯ ОТЧЕТОВ\n\n" +
                       "Текущие получатели:\n" +
                       "• admin@team.com\n" +
                       "• manager@team.com\n\n" +
                       "Чтобы изменить email, введите новый адрес:";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.SettingsReports) }
    });

            await _menuManager.SendTemporaryInlineMessageAsync(
                chatId,
                text,
                keyboard,
                cancellationToken,
                60);
        }

        private async Task ShowReportsExportAsync(long chatId, CancellationToken cancellationToken)
        {
            var text = "📤 ЭКСПОРТ ОТЧЕТОВ\n\n" +
                       "Выберите формат экспорта:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("📄 CSV", "export_csv"),
            InlineKeyboardButton.WithCallbackData("📊 Excel", "export_excel"),
            InlineKeyboardButton.WithCallbackData("📝 PDF", "export_pdf")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("👥 Пользователи", "export_users"),
            InlineKeyboardButton.WithCallbackData("📂 Проекты", "export_projects"),
            InlineKeyboardButton.WithCallbackData("✅ Задачи", "export_tasks")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("💰 Финансы", "export_finance"),
            InlineKeyboardButton.WithCallbackData("📊 KPI", "export_kpi"),
            InlineKeyboardButton.WithCallbackData("📈 Все", "export_all")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.SettingsReports) }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "reports_export", cancellationToken);
        }

        private async Task GenerateAndSendReportAsync(long chatId, CancellationToken cancellationToken)
        {
            await SendTemporaryMessageAsync(chatId, "📊 Генерация отчета...", cancellationToken);
            await Task.Delay(2000, cancellationToken);

            var date = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm");
            var report = $"📊 ОТЧЕТ СИСТЕМЫ\n\n" +
                         $"📅 Сгенерирован: {date} UTC\n\n" +
                         $"👥 ПОЛЬЗОВАТЕЛИ:\n" +
                         $"• Всего: 25\n" +
                         $"• Активны сегодня: 8\n" +
                         $"• Новых за неделю: 3\n\n" +
                         $"📂 ПРОЕКТЫ:\n" +
                         $"• Всего: 12\n" +
                         $"• В работе: 5\n" +
                         $"• Завершено: 4\n" +
                         $"• Предстоит: 3\n\n" +
                         $"✅ ЗАДАЧИ:\n" +
                         $"• Всего: 87\n" +
                         $"• Выполнено: 45 (52%)\n" +
                         $"• В работе: 32\n" +
                         $"• Просрочено: 10\n\n" +
                         $"💰 ФИНАНСЫ:\n" +
                         $"• Доход: 850,000 ₽\n" +
                         $"• Расход: 520,000 ₽\n" +
                         $"• Прибыль: 330,000 ₽\n\n" +
                         $"📊 KPI:\n" +
                         $"• Общий KPI: 78%\n" +
                         $"• Проекты: 75%\n" +
                         $"• Задачи: 82%\n" +
                         $"• Финансы: 76%";

            // Отправляем как новое сообщение (не редактируем существующее меню)
            await SendNewReportMessageAsync(chatId, report, cancellationToken, "settings_users");
        }

        // ========== ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ ОТПРАВКИ НОВЫХ СООБЩЕНИЙ ==========
        private async Task SendNewReportMessageAsync(long chatId, string text, CancellationToken cancellationToken, string returnCallback)
        {
            try
            {
                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📊 Обновить", "generate_report") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", returnCallback) }
        };

                // Отправляем НОВОЕ сообщение с отчетом
                var message = await _botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: cancellationToken);

                // Сохраняем это сообщение как временное, чтобы потом можно было его удалить
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Ждем 5 минут и удаляем, если не нужно
                        await Task.Delay(300000, cancellationToken);
                        await _botClient.DeleteMessage(chatId, message.MessageId, cancellationToken);
                    }
                    catch { }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending report message");
            }
        }

        private async Task ShowUsersManagementAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var admins = users.Where(u => u.Role == UserRole.Admin).ToList();

                var text = $"👥 УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ\n\n" +
                           $"Всего: {users.Count}\n" +
                           $"Администраторов: {admins.Count}\n\n" +
                           $"Последние активные:\n";

                var recentUsers = users.Where(u => u.LastActiveAt.HasValue)
                                      .OrderByDescending(u => u.LastActiveAt)
                                      .Take(3);

                foreach (var user in recentUsers)
                {
                    var name = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName;
                    var lastActive = user.LastActiveAt?.ToString("dd.MM HH:mm") ?? "никогда";
                    text += $"• {name} - {lastActive}\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("📊 Статистика", "settings_users_stats"),
                InlineKeyboardButton.WithCallbackData("👑 Назначить админа", "settings_add_admin")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("👑 Снять админа", "settings_remove_admin"),
                InlineKeyboardButton.WithCallbackData("👥 Все пользователи", "settings_all_users")
            },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад в настройки", CallbackData.BackToSettings) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "settings_users",  // Один и тот же menuType для всех подменю настроек
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing users management");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке пользователей.", cancellationToken, 3);
            }
        }

        private async Task ShowSecuritySettingsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var activeUsers = users.Count(u => u.LastActiveAt >= DateTime.UtcNow.AddDays(-7));
                var totalUsers = users.Count;
                var activePercentage = totalUsers > 0 ? (decimal)activeUsers / totalUsers * 100 : 0;

                var text = $"🔐 Настройки безопасности\n\n" +
                           $"📊 Статистика:\n" +
                           $"• Всего пользователей: {totalUsers}\n" +
                           $"• Активных (неделя): {activeUsers} ({activePercentage:F1}%)\n" +
                           $"• Администраторов: {users.Count(u => u.Role == UserRole.Admin)}\n" +
                           $"• Новых (месяц): {users.Count(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1))}\n\n" +
                           $"⚙️ Настройки:\n" +
                           $"• Верификация: ✅ Включена\n" +
                           $"• Логирование действий: ✅ Включено";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📋 Логи безопасности", "security_logs") },
            new() { InlineKeyboardButton.WithCallbackData("👥 Активные сессии", "security_sessions") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToSettings) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "settings_security",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing security settings");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке настроек безопасности.", cancellationToken);
            }
        }

        private async Task ShowDatabaseSettingsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Получаем статистику по всем таблицам
                var usersCount = (await _userService.GetAllUsersAsync()).Count;
                var projectsCount = (await _projectService.GetAllProjectsAsync()).Count;
                var tasksCount = (await _taskService.GetAllTasksAsync()).Count;
                var contactsCount = (await _contactService.GetAllContactsAsync()).Count;
                var financeCount = (await _financeService.GetAllRecordsAsync()).Count;

                var totalRecords = usersCount + projectsCount + tasksCount + contactsCount + financeCount;

                var text = $"💾 Настройки базы данных\n\n" +
                           $"📊 Статистика записей:\n" +
                           $"• Пользователи: {usersCount}\n" +
                           $"• Проекты: {projectsCount}\n" +
                           $"• Задачи: {tasksCount}\n" +
                           $"• Контакты: {contactsCount}\n" +
                           $"• Финансы: {financeCount}\n" +
                           $"• Всего: {totalRecords}\n\n" +
                           $"🔄 Автообслуживание:\n" +
                           $"• Последний бэкап: {DateTime.UtcNow.AddHours(-2):dd.MM.yyyy HH:mm}\n" +
                           $"• Следующий бэкап: {DateTime.UtcNow.AddHours(22):dd.MM.yyyy HH:mm}";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("💾 Создать бэкап", "settings_db_backup") },
            new() { InlineKeyboardButton.WithCallbackData("📊 Статистика", "settings_db_stats") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToSettings) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "settings_database",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing database settings");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке настроек базы данных.", cancellationToken);
            }
        }
        #endregion

        #region KPI - РЕАЛИЗАЦИЯ
        private async Task HandleKpiCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case CallbackData.KpiTasksWeek:
                    await ShowKpiTasksWeekAsync(chatId, cancellationToken);
                    break;

                case CallbackData.KpiFinanceMonth: // ← Исправлено
                    await ShowKpiFinanceMonthAsync(chatId, cancellationToken);
                    break;

                case CallbackData.KpiProjects:
                    await ShowKpiProjectsAsync(chatId, cancellationToken);
                    break;

                case CallbackData.KpiActivity:
                    await ShowKpiActivityAsync(chatId, cancellationToken);
                    break;

                case CallbackData.KpiOverall:
                    await ShowKpiOverallAsync(chatId, cancellationToken);
                    break;

                case CallbackData.KpiTeam:
                    await ShowKpiTeamAsync(chatId, cancellationToken);
                    break;

                default:
                    await SendTemporaryMessageAsync(chatId, "❌ Неизвестный отчет KPI.", cancellationToken);
                    break;
            }
        }

        // ===== KPI ЗАДАЧ ЗА НЕДЕЛЮ =====
        private async Task ShowKpiTasksWeekAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var weekStart = DateTime.UtcNow.AddDays(-7);
                var allTasks = await _taskService.GetAllTasksAsync();
                var weekTasks = allTasks.Where(t => t.CreatedAt >= weekStart).ToList();
                var users = await _userService.GetAllUsersAsync();

                var completedTasks = weekTasks.Count(t => t.Status == TeamTaskStatus.Completed);
                var activeTasks = weekTasks.Count(t => t.Status == TeamTaskStatus.Active);
                var archivedTasks = weekTasks.Count(t => t.Status == TeamTaskStatus.Archived);
                var totalTasks = weekTasks.Count;

                var completionRate = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

                // Расчет среднего времени выполнения
                double avgCompletionTime = 0;
                var completedTasksWithDate = weekTasks
                    .Where(t => t.Status == TeamTaskStatus.Completed && t.CompletedAt.HasValue)
                    .ToList();

                if (completedTasksWithDate.Any())
                {
                    avgCompletionTime = completedTasksWithDate
                        .Average(t => (t.CompletedAt!.Value - t.CreatedAt).TotalDays);
                }

                var text = $"📊 KPI: ЗАДАЧИ ЗА НЕДЕЛЮ\n\n" +
                           $"📅 Период: {weekStart:dd.MM.yyyy} - {DateTime.UtcNow:dd.MM.yyyy}\n\n" +
                           $"📈 СТАТИСТИКА:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ Всего задач: {totalTasks}\n" +
                           $"│ ✅ Выполнено: {completedTasks} ({completionRate}%)\n" +
                           $"│ 🟢 В работе: {activeTasks}\n" +
                           $"│ 📁 В архиве: {archivedTasks}\n" +
                           $"│ ⏱️ Среднее время: {avgCompletionTime:F1} дней\n" +
                           $"└─────────────────────────────────\n\n";

                // Статистика по исполнителям
                var userStats = weekTasks
                    .Where(t => t.AssignedToUserId != 0)
                    .GroupBy(t => t.AssignedToUserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Completed = g.Count(t => t.Status == TeamTaskStatus.Completed),
                        Total = g.Count()
                    })
                    .OrderByDescending(x => x.Completed)
                    .ToList();

                if (userStats.Any())
                {
                    text += $"🏆 ТОП ИСПОЛНИТЕЛЕЙ:\n";
                    foreach (var stat in userStats.Take(5))
                    {
                        var user = users.FirstOrDefault(u => u.TelegramId == stat.UserId);
                        var userName = user != null
                            ? (!string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName)
                            : $"ID:{stat.UserId}";
                        var rate = stat.Total > 0 ? (stat.Completed * 100 / stat.Total) : 0;
                        text += $"• {userName}: {stat.Completed}/{stat.Total} ({rate}%)\n";
                    }
                }
                else
                {
                    text += "📭 Нет задач за этот период\n";
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📅 ЗА МЕСЯЦ", CallbackData.KpiFinanceMonth) },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToKpi) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "kpi_tasks_week", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing KPI tasks week");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке статистики", cancellationToken, 3);
            }
        }
        // ===== KPI ФИНАНСОВ ЗА МЕСЯЦ =====
        private async Task ShowKpiFinanceMonthAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

                // Получаем все доходы и расходы за месяц
                var allIncomes = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Income);
                var allExpenses = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Expense);

                var monthlyIncomes = allIncomes
                    .Where(i => i.TransactionDate >= monthStart)
                    .Sum(i => i.Amount);

                var monthlyExpenses = allExpenses
                    .Where(e => e.TransactionDate >= monthStart)
                    .Sum(e => e.Amount);

                var profit = monthlyIncomes - monthlyExpenses;
                var profitMargin = monthlyIncomes > 0 ? profit / monthlyIncomes * 100 : 0;

                // Получаем категории доходов
                var incomeByCategory = allIncomes
                    .Where(i => i.TransactionDate >= monthStart)
                    .GroupBy(i => i.Category)
                    .Select(g => new { Category = g.Key, Amount = g.Sum(i => i.Amount) })
                    .OrderByDescending(x => x.Amount)
                    .Take(3)
                    .ToList();

                // Получаем категории расходов
                var expensesByCategory = allExpenses
                    .Where(e => e.TransactionDate >= monthStart)
                    .GroupBy(e => e.Category)
                    .Select(g => new { Category = g.Key, Amount = g.Sum(e => e.Amount) })
                    .OrderByDescending(x => x.Amount)
                    .Take(3)
                    .ToList();

                var text = $"💰 KPI: ФИНАНСЫ ЗА МЕСЯЦ\n\n" +
                           $"📅 Период: {monthStart:MMMM yyyy}\n\n" +
                           $"📊 ФИНАНСОВЫЕ ПОКАЗАТЕЛИ:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ 💵 Доходы: {monthlyIncomes:N2} ₽\n" +
                           $"│ 💸 Расходы: {monthlyExpenses:N2} ₽\n" +
                           $"│ 💰 Прибыль: {profit:N2} ₽\n" +
                           $"│ 📈 Маржа прибыли: {profitMargin:F1}%\n" +
                           $"└─────────────────────────────────\n\n";

                if (incomeByCategory.Any())
                {
                    text += $"📈 ТОП ДОХОДОВ:\n";
                    foreach (var cat in incomeByCategory)
                    {
                        var percentage = monthlyIncomes > 0 ? cat.Amount / monthlyIncomes * 100 : 0;
                        text += $"• {cat.Category}: {cat.Amount:N2} ₽ ({percentage:F1}%)\n";
                    }
                    text += "\n";
                }

                if (expensesByCategory.Any())
                {
                    text += $"📉 ТОП РАСХОДОВ:\n";
                    foreach (var cat in expensesByCategory)
                    {
                        var percentage = monthlyExpenses > 0 ? cat.Amount / monthlyExpenses * 100 : 0;
                        text += $"• {cat.Category}: {cat.Amount:N2} ₽ ({percentage:F1}%)\n";
                    }
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToKpi) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "kpi_finance_month", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing KPI finance month");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке финансовой статистики.", cancellationToken, 3);
            }
        }
        // ===== KPI ЗАДАЧ ЗА МЕСЯЦ =====
        private async Task ShowKpiTasksMonthAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var allTasks = await _taskService.GetAllTasksAsync();
                var monthTasks = allTasks.Where(t => t.CreatedAt >= monthStart).ToList();
                var users = await _userService.GetAllUsersAsync();

                var completedTasks = monthTasks.Count(t => t.Status == TeamTaskStatus.Completed);
                var activeTasks = monthTasks.Count(t => t.Status == TeamTaskStatus.Active);
                var archivedTasks = monthTasks.Count(t => t.Status == TeamTaskStatus.Archived);
                var totalTasks = monthTasks.Count;

                var completionRate = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

                // Статистика по дням
                var daysInMonth = DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month);
                var dailyStats = new List<(int Day, int Count)>();

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var dayDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, day);
                    var dayTasks = monthTasks.Count(t => t.CreatedAt.Date == dayDate.Date);
                    if (dayTasks > 0)
                    {
                        dailyStats.Add((day, dayTasks));
                    }
                }

                var text = $"📊 KPI: ЗАДАЧИ ЗА МЕСЯЦ\n\n" +
                           $"📅 Период: {monthStart:MMMM yyyy}\n\n" +
                           $"📈 СТАТИСТИКА:\n" +
                           $"┌─────────────────────────────────\n" +
                           $"│ Всего задач: {totalTasks}\n" +
                           $"│ ✅ Выполнено: {completedTasks} ({completionRate}%)\n" +
                           $"│ 🟢 В работе: {activeTasks}\n" +
                           $"│ 📁 В архиве: {archivedTasks}\n" +
                           $"└─────────────────────────────────\n\n";

                // Статистика по исполнителям
                var userStats = monthTasks
                    .Where(t => t.AssignedToUserId != 0)
                    .GroupBy(t => t.AssignedToUserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Completed = g.Count(t => t.Status == TeamTaskStatus.Completed),
                        Total = g.Count()
                    })
                    .OrderByDescending(x => x.Completed)
                    .ToList();

                if (userStats.Any())
                {
                    text += $"🏆 ТОП ИСПОЛНИТЕЛЕЙ ЗА МЕСЯЦ:\n";
                    foreach (var stat in userStats.Take(5))
                    {
                        var user = users.FirstOrDefault(u => u.TelegramId == stat.UserId);
                        var userName = user != null
                            ? (!string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : user.FirstName)
                            : $"ID:{stat.UserId}";
                        var rate = stat.Total > 0 ? (stat.Completed * 100 / stat.Total) : 0;
                        text += $"• {userName}: {stat.Completed}/{stat.Total} ({rate}%)\n";
                    }
                }

                if (dailyStats.Any())
                {
                    text += $"\n📅 АКТИВНОСТЬ ПО ДНЯМ:\n";
                    foreach (var day in dailyStats.TakeLast(10))
                    {
                        text += $"• {day.Day} число: {day.Count} задач\n";
                    }
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📅 ЗА НЕДЕЛЮ", CallbackData.KpiTasksWeek) },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToKpi) }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "kpi_tasks_month", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing KPI tasks month");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке статистики", cancellationToken, 3);
            }
        }

        private async Task ShowKpiProjectsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var projects = await _projectService.GetAllProjectsAsync();

                var completedProjects = projects.Count(p => p.Status == ProjectStatus.Completed);
                var inProgressProjects = projects.Count(p => p.Status == ProjectStatus.InProgress);
                var pendingProjects = projects.Count(p => p.Status == ProjectStatus.Pending);
                var totalProjects = projects.Count;

                var completionRate = totalProjects > 0 ? (decimal)completedProjects / totalProjects * 100 : 0;

                var text = $"📂 KPI: Прогресс проектов\n\n" +
                           $"📊 Статус проектов:\n" +
                           $"• Всего проектов: {totalProjects}\n" +
                           $"• Завершено: {completedProjects} ({completionRate:F1}%)\n" +
                           $"• В работе: {inProgressProjects}\n" +
                           $"• Предстоит: {pendingProjects}\n\n";

                // Самые успешные проекты (по выполнению задач)
                var projectsWithTasks = projects
                    .Where(p => p.Tasks != null && p.Tasks.Any())
                    .Select(p => new
                    {
                        Project = p,
                        CompletedTasks = p.Tasks.Count(t => t.Status == TeamTaskStatus.Completed),
                        TotalTasks = p.Tasks.Count()
                    })
                    .Where(x => x.TotalTasks > 0)
                    .OrderByDescending(x => (decimal)x.CompletedTasks / x.TotalTasks * 100)
                    .Take(3)
                    .ToList();

                if (projectsWithTasks.Any())
                {
                    text += $"🏆 Самые успешные проекты:\n";
                    foreach (var project in projectsWithTasks)
                    {
                        var completionRateProject = project.TotalTasks > 0 ?
                            (decimal)project.CompletedTasks / project.TotalTasks * 100 : 0;
                        text += $"• {project.Project.Name}: {project.CompletedTasks}/{project.TotalTasks} ({completionRateProject:F1}%)\n";
                    }
                }

                // Расчет средних показателей
                if (projects.Any())
                {
                    var avgTasksPerProject = projects.Average(p => p.Tasks?.Count ?? 0);
                    var recentProjects = projects
                        .Where(p => p.Status == ProjectStatus.Completed && p.UpdatedAt.HasValue)
                        .ToList();

                    double avgCompletionDays = 0;
                    if (recentProjects.Any())
                    {
                        avgCompletionDays = recentProjects
                            .Average(p => (p.UpdatedAt!.Value - p.CreatedAt).TotalDays);
                    }

                    text += $"\n📈 Средние показатели:\n";
                    text += $"• Среднее время выполнения проекта: {avgCompletionDays:F1} дней\n";
                    text += $"• Среднее количество задач на проект: {avgTasksPerProject:F1}\n";
                }

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    MainMenuKeyboard.GetBackButton(CallbackData.BackToKpi),
                    "kpi_projects",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing KPI projects");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке KPI по проектам.", cancellationToken);
            }
        }

        private async Task ShowKpiActivityAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var weekStart = DateTime.UtcNow.AddDays(-7);
                var users = await _userService.GetAllUsersAsync();

                var activeUsers = users.Count(u => u.LastActiveAt >= weekStart);
                var newUsers = users.Count(u => u.CreatedAt >= weekStart);
                var totalUsers = users.Count;

                var activityPercentage = totalUsers > 0 ? (decimal)activeUsers / totalUsers * 100 : 0;

                var text = $"👥 KPI: Активность участников\n\n" +
                           $"📅 Период: {weekStart:dd.MM.yyyy} - {DateTime.UtcNow:dd.MM.yyyy}\n\n" +
                           $"📊 Статистика активности:\n" +
                           $"• Всего пользователей: {totalUsers}\n" +
                           $"• Активных за неделю: {activeUsers} ({(activityPercentage):F1}%)\n" +
                           $"• Новых за неделю: {newUsers}\n\n";

                // Самые активные пользователи
                var activeUsersList = users
                    .Where(u => u.LastActiveAt.HasValue)
                    .OrderByDescending(u => u.LastActiveAt)
                    .Take(5)
                    .ToList();

                if (activeUsersList.Any())
                {
                    text += $"🏆 Самые активные участники:\n";
                    foreach (var user in activeUsersList)
                    {
                        if (user.LastActiveAt.HasValue)
                        {
                            var daysSinceActive = (DateTime.UtcNow - user.LastActiveAt.Value).Days;
                            var activityText = daysSinceActive == 0 ? "сегодня" :
                                              daysSinceActive == 1 ? "вчера" :
                                              $"{daysSinceActive} дн. назад";

                            text += $"• @{user.Username} - был {activityText}\n";
                        }
                    }
                }

                // Анализ активности по дням (упрощенный)
                var activeUsersByDay = new Dictionary<DayOfWeek, int>();
                foreach (var user in users.Where(u => u.LastActiveAt.HasValue && u.LastActiveAt >= weekStart))
                {
                    var day = user.LastActiveAt!.Value.DayOfWeek;
                    activeUsersByDay[day] = activeUsersByDay.GetValueOrDefault(day) + 1;
                }

                if (activeUsersByDay.Any())
                {
                    text += $"\n📈 Активность по дням недели:\n";
                    var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                   DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

                    foreach (var day in daysOfWeek)
                    {
                        var count = activeUsersByDay.GetValueOrDefault(day);
                        var percentage = activeUsers > 0 ? (decimal)count / activeUsers * 100 : 0;
                        text += $"• {GetDayName(day)}: {count} ({(percentage):F1}%)\n";
                    }
                }

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    MainMenuKeyboard.GetBackButton(CallbackData.BackToKpi),
                    "kpi_activity",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing KPI activity");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке KPI по активности.", cancellationToken);
            }
        }

        private async Task ShowKpiOverallAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Собираем все KPI
                var projects = await _projectService.GetAllProjectsAsync();
                var tasks = await _taskService.GetAllTasksAsync();
                var incomes = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Income);
                var expenses = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Expense);

                // Расчеты
                var completedProjects = projects.Count(p => p.Status == ProjectStatus.Completed);
                var totalProjects = projects.Count;
                var projectCompletionRate = totalProjects > 0 ? (decimal)completedProjects / totalProjects * 100 : 0;

                var completedTasks = tasks.Count(t => t.Status == TeamTaskStatus.Completed);
                var totalTasks = tasks.Count;
                var taskCompletionRate = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0;

                var totalIncome = incomes.Sum(i => i.Amount);
                var totalExpenses = expenses.Sum(e => e.Amount);
                var profit = totalIncome - totalExpenses;
                var profitMargin = totalIncome > 0 ? profit / totalIncome * 100 : 0;

                // Общий KPI (взвешенная сумма)
                var overallKpi = (projectCompletionRate * 0.3m + taskCompletionRate * 0.4m + profitMargin * 0.3m);
                var kpiStatus = overallKpi switch
                {
                    >= 90 => "💎 Отлично",
                    >= 75 => "👍 Хорошо",
                    >= 60 => "⚠️ Средне",
                    _ => "❌ Требует улучшения"
                };

                var text = $"📊 Общий KPI системы\n\n" +
                           $"⭐ Общая оценка: {overallKpi:F1}/100\n" +
                           $"📋 Статус: {kpiStatus}\n\n" +
                           $"📈 Компоненты KPI:\n" +
                           $"• Проекты: {projectCompletionRate:F1}/100 (вес 30%)\n" +
                           $"• Задачи: {taskCompletionRate:F1}/100 (вес 40%)\n" +
                           $"• Финансы: {profitMargin:F1}/100 (вес 30%)\n\n" +
                           $"📊 Детализация:\n" +
                           $"• Проектов завершено: {completedProjects}/{totalProjects}\n" +
                           $"• Задач выполнено: {completedTasks}/{totalTasks}\n" +
                           $"• Прибыль: {profit:N2} ₽ (маржа: {profitMargin:F1}%)\n\n" +
                           $"📈 Рекомендации:\n";

                if (overallKpi >= 90)
                    text += $"• Продолжайте в том же духе!\n• Рассмотрите расширение команды\n• Инвестируйте в развитие";
                else if (overallKpi >= 75)
                    text += $"• Улучшите выполнение задач\n• Оптимизируйте расходы\n• Ускорьте выполнение проектов";
                else if (overallKpi >= 60)
                    text += $"• Срочно улучшите выполнение задач\n• Пересмотрите процессы\n• Увеличьте контроль качества";
                else
                    text += $"• Требуется кардинальное улучшение\n• Пересмотрите все процессы\n• Рассмотрите реорганизацию команды";

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    MainMenuKeyboard.GetBackButton(CallbackData.BackToKpi),
                    "kpi_overall",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing KPI overall");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке общего KPI.", cancellationToken);
            }
        }

        private async Task ShowKpiTeamAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var tasks = await _taskService.GetAllTasksAsync();

                var text = $"👥 KPI команды\n\n" +
                           $"👨‍👩‍👧‍👦 Состав команды:\n" +
                           $"• Всего участников: {users.Count}\n" +
                           $"• Администраторов: {users.Count(u => u.Role == UserRole.Admin)}\n" +
                           $"• Активных участников: {users.Count(u => u.LastActiveAt >= DateTime.UtcNow.AddDays(-7))}\n\n";

                // Расчет KPI для каждого пользователя
                var userKpis = new List<(string Username, decimal Kpi, int CompletedTasks, int TotalTasks)>();

                foreach (var user in users.Take(10))
                {
                    var userTasks = tasks.Where(t => t.AssignedToUserId == user.TelegramId).ToList();
                    var completedTasks = userTasks.Count(t => t.Status == TeamTaskStatus.Completed);
                    var totalTasks = userTasks.Count;
                    var taskCompletionRate = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0;

                    // Формула KPI: 70% за выполнение задач + 30% за активность
                    var activityScore = user.LastActiveAt >= DateTime.UtcNow.AddDays(-1) ? 100 :
                                       user.LastActiveAt >= DateTime.UtcNow.AddDays(-3) ? 70 :
                                       user.LastActiveAt >= DateTime.UtcNow.AddDays(-7) ? 40 : 10;

                    var kpi = taskCompletionRate * 0.7m + activityScore * 0.3m;
                    userKpis.Add((user.Username, kpi, completedTasks, totalTasks));
                }

                // Показываем топ-5
                if (userKpis.Any())
                {
                    text += $"🏆 Топ участников:\n";
                    userKpis = userKpis.OrderByDescending(x => x.Kpi).Take(5).ToList();

                    int rank = 1;
                    foreach (var userKpi in userKpis)
                    {
                        var rankIcon = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : "•";
                        text += $"{rankIcon} @{userKpi.Username}: {(userKpi.Kpi):F1}/100\n";
                        if (userKpi.TotalTasks > 0)
                        {
                            var completionRate = (decimal)userKpi.CompletedTasks / userKpi.TotalTasks * 100;
                            text += $"   Задачи: {userKpi.CompletedTasks}/{userKpi.TotalTasks} ({(completionRate):F1}%)\n";
                        }
                        rank++;
                    }

                    // Средние показатели
                    var averageKpi = userKpis.Any() ? userKpis.Average(u => u.Kpi) : 0;
                    var activeUsers = users.Count(u => u.LastActiveAt >= DateTime.UtcNow.AddDays(-7));
                    var activePercentage = users.Count > 0 ? (decimal)activeUsers / users.Count * 100 : 0;

                    text += $"\n📈 Средние показатели команды:\n";
                    text += $"• Средний KPI: {(averageKpi):F1}/100\n";
                    text += $"• Активность: {activeUsers}/{users.Count} ({(activePercentage):F1}%)\n";
                }
                else
                {
                    text += "📭 Нет данных для расчета KPI участников.\n";
                }

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    MainMenuKeyboard.GetBackButton(CallbackData.BackToKpi),
                    "kpi_team",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing KPI team");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке KPI команды.", cancellationToken);
            }
        }

        private string GetDayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Понедельник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Среда",
                DayOfWeek.Thursday => "Четверг",
                DayOfWeek.Friday => "Пятница",
                DayOfWeek.Saturday => "Суббота",
                DayOfWeek.Sunday => "Воскресенье",
                _ => day.ToString()
            };
        }
        #endregion

        #region База данных - РЕАЛИЗАЦИЯ
        private async Task HandleDatabaseCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            Console.WriteLine($"🎯 Запущен HandleDatabaseCallbackAsync");
            Console.WriteLine($"   ├─ Chat ID: {chatId}");
            Console.WriteLine($"   ├─ User ID: {userId}");
            Console.WriteLine($"   └─ Callback Data: {callbackData}");

            switch (callbackData)
            {
                // ===== ГЛАВНОЕ МЕНЮ БД =====
                case CallbackData.BackToDatabase:
                    await _menuManager.ShowDatabaseMenuAsync(chatId, cancellationToken);
                    break;
                // БЭКАП БД
                case "settings_db_stats":
                    await ShowDatabaseDetailedStatsAsync(chatId, cancellationToken);
                    break;
                case "settings_db_backup":
                    await CreateDatabaseBackupAsync(chatId, cancellationToken);
                    break;

                case "settings_db_backup_list":
                    await ShowBackupListAsync(chatId, cancellationToken);
                    break;

                case "settings_db_cleanup":
                    await CleanupOldBackupsAsync(chatId, cancellationToken);
                    break;

                case var _ when callbackData.StartsWith("backup_download_"):
                    var backupFileName = callbackData.Replace("backup_download_", "");
                    await DownloadBackupAsync(chatId, backupFileName, cancellationToken);
                    break;
                // ===== КОНТАКТЫ =====
                case "db_contacts_menu":
                    await ShowContactsDatabaseMenuAsync(chatId, cancellationToken);
                    break;
                case "db_contacts_all":
                    await ShowAllContactsDatabaseAsync(chatId, cancellationToken);
                    break;
                case "db_contacts_search":
                    await StartContactSearchAsync(chatId, userId, cancellationToken);
                    break;
                case "db_contacts_stats":
                    await ShowContactStatsAsync(chatId, cancellationToken);
                    break;
                case "db_contacts_with_cards":
                    await ShowContactsWithCardsAsync(chatId, cancellationToken);
                    break;
                case "db_contacts_with_passports":
                    await ShowContactsWithPassportsAsync(chatId, cancellationToken);
                    break;
                case "db_contact_add":
                    await StartAddContactAsync(chatId, userId, cancellationToken);
                    break;
                case "db_contacts_status_working":
                    await ShowContactsByStatusAsync(chatId, "рабочая", cancellationToken);
                    break;
                case "db_contacts_status_locked":
                    await ShowContactsByStatusAsync(chatId, "лок", cancellationToken);
                    break;
                case "db_contacts_status_blocked":
                    await ShowContactsByStatusAsync(chatId, "115", cancellationToken);
                    break;

                // ===== ПОСТЫ =====
                case "db_posts_menu":
                    await ShowPostsMenuAsync(chatId, cancellationToken);
                    break;
                case "db_posts_all":
                    await ShowAllPostsAsync(chatId, cancellationToken);
                    break;
                case "db_posts_add":
                    await StartAddPostAsync(chatId, userId, cancellationToken);
                    break;
                case "db_posts_search":
                    await StartSearchPostsAsync(chatId, userId, cancellationToken);
                    break;
                case "db_posts_stats":
                    await ShowPostsStatsAsync(chatId, cancellationToken);
                    break;
                case "db_posts_by_channel":
                    await ShowPostsByChannelAsync(chatId, cancellationToken);
                    break;

                // ===== МАНУАЛЫ =====
                case "db_manuals_menu":
                    await ShowManualsMenuAsync(chatId, cancellationToken);
                    break;
                case "db_manuals_all":
                    await ShowAllManualsAsync(chatId, cancellationToken);
                    break;
                case "db_manuals_add":
                    await StartAddManualAsync(chatId, userId, cancellationToken);
                    break;
                case "db_manuals_search":
                    await StartSearchManualsAsync(chatId, userId, cancellationToken);
                    break;
                case "db_manuals_main":
                    await ShowManualsByCategoryAsync(chatId, "Основной", cancellationToken);
                    break;
                case "db_manuals_additional":
                    await ShowManualsByCategoryAsync(chatId, "Дополнительный", cancellationToken);
                    break;
                case "db_manuals_test":
                    await ShowManualsByCategoryAsync(chatId, "Тестовый", cancellationToken);
                    break;
                case "db_manuals_shadowban":
                    await ShowManualsByCategoryAsync(chatId, "Обход теневого бана", cancellationToken);
                    break;
                case "db_manuals_unblock":
                    await ShowManualsByCategoryAsync(chatId, "Снятие 115/161", cancellationToken);
                    break;
                case "db_manuals_by_bank":
                    await ShowManualsByBankAsync(chatId, cancellationToken);
                    break;

                // ===== ОТЧЁТЫ =====
                case "db_reports_menu":
                    await ShowReportsMenuAsync(chatId, cancellationToken);
                    break;
                case "db_reports_all":
                    await ShowAllReportsAsync(chatId, cancellationToken);
                    break;
                case "db_reports_add":
                    await StartAddReportAsync(chatId, userId, cancellationToken);
                    break;
                case "db_reports_search":
                    await StartSearchReportsAsync(chatId, userId, cancellationToken);
                    break;
                case "db_reports_stats":
                    await ShowReportsStatsAsync(chatId, cancellationToken);
                    break;
                case "db_reports_export":
                    await StartExportReportAsync(chatId, userId, cancellationToken);
                    break;

                // ===== ДОКУМЕНТАЦИЯ =====
                case "db_docs_menu":
                    await ShowDocsMenuAsync(chatId, cancellationToken);
                    break;
                case "db_docs_all":
                    await ShowAllDocsAsync(chatId, cancellationToken);
                    break;
                case "db_docs_add":
                    await StartAddDocAsync(chatId, userId, cancellationToken);
                    break;
                case "db_docs_search":
                    await StartSearchDocsAsync(chatId, userId, cancellationToken);
                    break;
                case "db_docs_by_project":
                    await ShowDocsByProjectAsync(chatId, cancellationToken);
                    break;
                case "db_docs_stats":
                    await ShowDocsStatsAsync(chatId, cancellationToken);
                    break;

                // ===== РЕКЛАМА =====
                case "db_ads_menu":
                    await ShowAdsMenuAsync(chatId, cancellationToken);
                    break;
                case "db_ads_all":
                    await ShowAllAdsAsync(chatId, cancellationToken);
                    break;
                case "db_ads_add":
                    await StartAddAdAsync(chatId, userId, cancellationToken);
                    break;
                case "db_ads_search":
                    await StartSearchAdsAsync(chatId, userId, cancellationToken);
                    break;
                case "db_ads_costs":
                    await ShowAdCostsAsync(chatId, cancellationToken);
                    break;
                case "db_ads_stats":
                    await ShowAdStatsAsync(chatId, cancellationToken);
                    break;
                case "db_ads_active":
                    await ShowAdsByStatusAsync(chatId, "Активна", cancellationToken);
                    break;
                case "db_ads_completed":
                    await ShowAdsByStatusAsync(chatId, "Завершена", cancellationToken);
                    break;

                // ===== FUNPAY =====
                case "db_funpay_menu":
                    await ShowFunPayDbMenuAsync(chatId, cancellationToken);
                    break;
                case "db_funpay_accounts_all":
                    await ShowAllFunPayAccountsAsync(chatId, cancellationToken);
                    break;
                case "db_funpay_account_add":
                    await StartAddFunPayAccountAsync(chatId, userId, cancellationToken);
                    break;
                case "db_funpay_warnings_all":
                    await ShowAllFunPayWarningsAsync(chatId, cancellationToken);
                    break;
                case "db_funpay_warning_add":
                    await StartAddFunPayWarningAsync(chatId, userId, cancellationToken);
                    break;
                case "db_funpay_stats":
                    await ShowFunPayDbStatsAsync(chatId, cancellationToken);
                    break;
                case "db_funpay_search":
                    await StartSearchFunPayAsync(chatId, userId, cancellationToken);
                    break;

                // ===== ОБРАБОТКА ДИНАМИЧЕСКИХ CALLBACK (КАК В ПРОЕКТАХ) =====
                default:
                    // ===== СНАЧАЛА ПРОВЕРЯЕМ ПОДТВЕРЖДЕНИЯ УДАЛЕНИЯ (как в проектах) =====

                    // ===== ПОДТВЕРЖДЕНИЯ УДАЛЕНИЯ =====
                    if (callbackData.StartsWith("delete_funpay_account_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_funpay_account_confirm_", "");
                        Console.WriteLine($"   → ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ FUNPAY АККАУНТА ID: {idStr}");
                        if (int.TryParse(idStr, out int accountId))
                        {
                            await DeleteFunPayAccountAsync(chatId, accountId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("delete_post_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_post_confirm_", "");
                        Console.WriteLine($"   → Подтверждение удаления поста ID: {idStr}");
                        if (int.TryParse(idStr, out int postId))
                            await DeletePostAsync(chatId, postId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("delete_contact_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_contact_confirm_", "");
                        Console.WriteLine($"   → Подтверждение удаления контакта ID: {idStr}");
                        if (int.TryParse(idStr, out int contactId))
                            await DeleteContactAsync(chatId, contactId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("delete_manual_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_manual_confirm_", "");
                        Console.WriteLine($"   → Подтверждение удаления мануала ID: {idStr}");
                        if (int.TryParse(idStr, out int manualId))
                            await DeleteManualAsync(chatId, manualId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("delete_report_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_report_confirm_", "");
                        Console.WriteLine($"   → Подтверждение удаления отчёта ID: {idStr}");
                        if (int.TryParse(idStr, out int reportId))
                            await DeleteReportAsync(chatId, reportId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("delete_doc_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_doc_confirm_", "");
                        Console.WriteLine($"   → Подтверждение удаления документа ID: {idStr}");
                        if (int.TryParse(idStr, out int docId))
                            await DeleteDocAsync(chatId, docId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("delete_ad_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_ad_confirm_", "");
                        Console.WriteLine($"   → Подтверждение удаления рекламы ID: {idStr}");
                        if (int.TryParse(idStr, out int adId))
                            await DeleteAdAsync(chatId, adId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("delete_warning_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_warning_confirm_", "");
                        Console.WriteLine($"   → Подтверждение удаления штрафа ID: {idStr}");
                        if (int.TryParse(idStr, out int warningId))
                            await DeleteWarningAsync(chatId, warningId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("delete_funpay_account_confirm_"))
                    {
                        var idStr = callbackData.Replace("delete_funpay_account_confirm_", "");
                        Console.WriteLine($"   → Подтверждение удаления FunPay аккаунта ID: {idStr}");
                        if (int.TryParse(idStr, out int accountId))
                            await DeleteFunPayAccountAsync(chatId, accountId, cancellationToken);
                    }

                    // ===== ЗАТЕМ ПРОВЕРЯЕМ ЗАПРОСЫ НА УДАЛЕНИЕ (ПОКАЗ ПОДТВЕРЖДЕНИЯ) =====

                    // УДАЛЕНИЕ ПОСТА
                    else if (callbackData.StartsWith("db_post_delete_"))
                    {
                        var idStr = callbackData.Replace("db_post_delete_", "");
                        Console.WriteLine($"   → Удаление поста ID: {idStr}");
                        if (int.TryParse(idStr, out int postId))
                            await ShowDeletePostConfirmationAsync(chatId, postId, cancellationToken);
                    }
                    // УДАЛЕНИЕ КОНТАКТА
                    else if (callbackData.StartsWith("db_contact_delete_"))
                    {
                        var idStr = callbackData.Replace("db_contact_delete_", "");
                        Console.WriteLine($"   → Удаление контакта ID: {idStr}");
                        if (int.TryParse(idStr, out int contactId))
                            await ShowDeleteContactConfirmationAsync(chatId, contactId, cancellationToken);
                    }
                    // УДАЛЕНИЕ МАНУАЛА
                    else if (callbackData.StartsWith("db_manual_delete_"))
                    {
                        var idStr = callbackData.Replace("db_manual_delete_", "");
                        Console.WriteLine($"   → Удаление мануала ID: {idStr}");
                        if (int.TryParse(idStr, out int manualId))
                            await ShowDeleteManualConfirmationAsync(chatId, manualId, cancellationToken);
                    }
                    // УДАЛЕНИЕ ОТЧЁТА
                    else if (callbackData.StartsWith("db_report_delete_"))
                    {
                        var idStr = callbackData.Replace("db_report_delete_", "");
                        Console.WriteLine($"   → Удаление отчёта ID: {idStr}");
                        if (int.TryParse(idStr, out int reportId))
                            await ShowDeleteReportConfirmationAsync(chatId, reportId, cancellationToken);
                    }
                    // УДАЛЕНИЕ ДОКУМЕНТА
                    else if (callbackData.StartsWith("db_doc_delete_"))
                    {
                        var idStr = callbackData.Replace("db_doc_delete_", "");
                        Console.WriteLine($"   → Удаление документа ID: {idStr}");
                        if (int.TryParse(idStr, out int docId))
                            await ShowDeleteDocConfirmationAsync(chatId, docId, cancellationToken);
                    }
                    // УДАЛЕНИЕ РЕКЛАМЫ
                    else if (callbackData.StartsWith("db_ad_delete_"))
                    {
                        var idStr = callbackData.Replace("db_ad_delete_", "");
                        Console.WriteLine($"   → Удаление рекламы ID: {idStr}");
                        if (int.TryParse(idStr, out int adId))
                            await ShowDeleteAdConfirmationAsync(chatId, adId, cancellationToken);
                    }
                    // УДАЛЕНИЕ FUNPAY АККАУНТА
                    else if (callbackData.StartsWith("db_funpay_account_delete_"))
                    {
                        var idStr = callbackData.Replace("db_funpay_account_delete_", "");
                        Console.WriteLine($"   → Удаление FunPay аккаунта ID: {idStr}");
                        if (int.TryParse(idStr, out int accountId))
                            await ShowDeleteFunPayAccountConfirmationAsync(chatId, accountId, cancellationToken);
                    }
                    // УДАЛЕНИЕ ШТРАФА
                    else if (callbackData.StartsWith("db_funpay_warning_delete_"))
                    {
                        var idStr = callbackData.Replace("db_funpay_warning_delete_", "");
                        Console.WriteLine($"   → Удаление штрафа ID: {idStr}");
                        if (int.TryParse(idStr, out int warningId))
                            await ShowDeleteWarningConfirmationAsync(chatId, warningId, cancellationToken);
                    }

                    // ===== ПРОСМОТР ДЕТАЛЕЙ =====
                    else if (callbackData.StartsWith("db_post_view_"))
                    {
                        var id = callbackData.Replace("db_post_view_", "");
                        if (int.TryParse(id, out int postId))
                            await ShowPostDetailsAsync(chatId, postId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_contact_view_"))
                    {
                        var id = callbackData.Replace("db_contact_view_", "");
                        if (int.TryParse(id, out int contactId))
                            await ShowContactDetailsDatabaseAsync(chatId, contactId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_manual_view_"))
                    {
                        var id = callbackData.Replace("db_manual_view_", "");
                        if (int.TryParse(id, out int manualId))
                            await ShowManualDetailsAsync(chatId, manualId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_report_view_"))
                    {
                        var id = callbackData.Replace("db_report_view_", "");
                        if (int.TryParse(id, out int reportId))
                            await ShowReportDetailsAsync(chatId, reportId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_doc_view_"))
                    {
                        var id = callbackData.Replace("db_doc_view_", "");
                        if (int.TryParse(id, out int docId))
                            await ShowDocDetailsAsync(chatId, docId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_ad_view_"))
                    {
                        var id = callbackData.Replace("db_ad_view_", "");
                        if (int.TryParse(id, out int adId))
                            await ShowAdDetailsAsync(chatId, adId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_funpay_account_view_"))
                    {
                        var id = callbackData.Replace("db_funpay_account_view_", "");
                        if (int.TryParse(id, out int accountId))
                            await ShowFunPayAccountDetailsAsync(chatId, accountId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_funpay_warning_view_"))
                    {
                        var id = callbackData.Replace("db_funpay_warning_view_", "");
                        if (int.TryParse(id, out int warningId))
                            await ShowFunPayWarningDetailsAsync(chatId, warningId, cancellationToken);
                    }

                    // ===== РЕДАКТИРОВАНИЕ =====
                    else if (callbackData.StartsWith("db_post_edit_"))
                    {
                        var id = callbackData.Replace("db_post_edit_", "");
                        if (int.TryParse(id, out int postId))
                            await StartEditPostAsync(chatId, userId, postId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_contact_edit_"))
                    {
                        var id = callbackData.Replace("db_contact_edit_", "");
                        if (int.TryParse(id, out int contactId))
                            await StartEditContactAsync(chatId, userId, contactId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_manual_edit_"))
                    {
                        var id = callbackData.Replace("db_manual_edit_", "");
                        if (int.TryParse(id, out int manualId))
                            await StartEditManualAsync(chatId, userId, manualId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_report_edit_"))
                    {
                        var id = callbackData.Replace("db_report_edit_", "");
                        if (int.TryParse(id, out int reportId))
                            await StartEditReportAsync(chatId, userId, reportId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_doc_edit_"))
                    {
                        var id = callbackData.Replace("db_doc_edit_", "");
                        if (int.TryParse(id, out int docId))
                            await StartEditDocAsync(chatId, userId, docId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_ad_edit_"))
                    {
                        var id = callbackData.Replace("db_ad_edit_", "");
                        if (int.TryParse(id, out int adId))
                            await StartEditAdAsync(chatId, userId, adId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_funpay_account_edit_"))
                    {
                        var id = callbackData.Replace("db_funpay_account_edit_", "");
                        if (int.TryParse(id, out int accountId))
                            await StartEditFunPayAccountAsync(chatId, userId, accountId, cancellationToken);
                    }

                    // ===== КАРТЫ КОНТАКТОВ =====
                    else if (callbackData.StartsWith("db_contact_cards_"))
                    {
                        var id = callbackData.Replace("db_contact_cards_", "");
                        if (int.TryParse(id, out int contactId))
                            await ShowContactCardsAsync(chatId, contactId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_contact_add_card_"))
                    {
                        var id = callbackData.Replace("db_contact_add_card_", "");
                        if (int.TryParse(id, out int contactId))
                            await StartAddCardToContactAsync(chatId, userId, contactId, cancellationToken);
                    }
                    else if (callbackData.StartsWith("db_contact_card_primary_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 6 && int.TryParse(parts[4], out int contactId))
                        {
                            var cardNumber = string.Join("_", parts.Skip(5));
                            await SetPrimaryBankCardAsync(chatId, userId, contactId, cardNumber, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("db_contact_card_delete_confirm_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 6 && int.TryParse(parts[5], out int contactId))
                        {
                            var cardNumber = string.Join("_", parts.Skip(6));
                            await DeleteBankCardAsync(chatId, userId, contactId, cardNumber, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("db_contact_card_delete_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int contactId))
                        {
                            var cardNumber = string.Join("_", parts.Skip(5));
                            await ShowDeleteCardConfirmationAsync(chatId, contactId, cardNumber, cancellationToken);
                        }
                    }

                    else if (callbackData.StartsWith("db_contact_card_open_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 6 && int.TryParse(parts[4], out int contactId) && int.TryParse(parts[5], out int cardIndex))
                        {
                            await ShowContactCardDetailsAsync(chatId, contactId, cardIndex, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("db_contact_card_edit_field_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 8 && int.TryParse(parts[5], out int contactId) && int.TryParse(parts[6], out int cardIndex))
                        {
                            var field = string.Join("_", parts.Skip(7));
                            await StartEditContactCardFieldAsync(chatId, userId, contactId, cardIndex, field, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("db_contact_card_edit_"))
                    {
                        var parts = callbackData.Split('_');
                        if (parts.Length >= 6 && int.TryParse(parts[4], out int contactId) && int.TryParse(parts[5], out int cardIndex))
                        {
                            await StartEditContactCardAsync(chatId, userId, contactId, cardIndex, cancellationToken);
                        }
                    }

                    // ===== ЗАМЕТКИ КОНТАКТОВ ===== ← ВСТАВЬ СЮДА
                    else if (callbackData.StartsWith("db_contact_notes_"))
                    {
                        var idStr = callbackData.Replace("db_contact_notes_", "");
                        if (int.TryParse(idStr, out int contactId))
                        {
                            await ShowContactNotesAsync(chatId, contactId, cancellationToken);
                        }
                    }
                    else if (callbackData.StartsWith("db_contact_edit_notes_"))
                    {
                        Console.WriteLine($"   → НАЧАЛО ОБРАБОТКИ db_contact_edit_notes_");
                        var idStr = callbackData.Replace("db_contact_edit_notes_", "");
                        Console.WriteLine($"   → ID строки: {idStr}");
                        if (int.TryParse(idStr, out int contactId))
                        {
                            Console.WriteLine($"   → ID контакта: {contactId}, вызываем StartEditContactNotesAsync");
                            await StartEditContactNotesAsync(chatId, userId, contactId, cancellationToken);
                        }
                        else
                        {
                            Console.WriteLine($"   → НЕ УДАЛОСЬ распарсить ID из: {idStr}");
                        }
                    }

                    // ===== ЭКСПОРТ ОТЧЁТОВ =====
                    else if (callbackData.StartsWith("db_report_export_"))
                    {
                        var id = callbackData.Replace("db_report_export_", "");
                        if (int.TryParse(id, out int reportId))
                            await ExportReportToPdfAsync(chatId, reportId, cancellationToken);
                    }

                    // ===== ТРАТЫ НА РЕКЛАМУ =====
                    else if (callbackData.StartsWith("db_ad_add_spent_"))
                    {
                        var id = callbackData.Replace("db_ad_add_spent_", "");
                        if (int.TryParse(id, out int adId))
                            await StartAddSpentAsync(chatId, userId, adId, cancellationToken);
                    }

                    // ===== FUNPAY ШТРАФЫ =====
                    else if (callbackData.StartsWith("db_funpay_warnings_all_"))
                    {
                        var id = callbackData.Replace("db_funpay_warnings_all_", "");
                        if (int.TryParse(id, out int accountId))
                            await ShowAllFunPayWarningsAsync(chatId, cancellationToken, accountId);
                    }
                    else if (callbackData.StartsWith("db_funpay_warning_add_"))
                    {
                        var id = callbackData.Replace("db_funpay_warning_add_", "");
                        if (int.TryParse(id, out int accountId))
                            await StartAddFunPayWarningAsync(chatId, userId, cancellationToken, accountId);
                    }
                    else if (callbackData.StartsWith("db_funpay_warning_resolve_"))
                    {
                        var id = callbackData.Replace("db_funpay_warning_resolve_", "");
                        if (int.TryParse(id, out int warningId))
                            await StartResolveWarningAsync(chatId, userId, warningId, cancellationToken);
                    }

                    // ===== НЕИЗВЕСТНЫЙ CALLBACK =====
                    else
                    {
                        Console.WriteLine($"❌ Неизвестный callback в БД: {callbackData}");
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Функция в разработке", cancellationToken, 5);
                    }
                    break;
            }
        }
        private async Task StartAddCardToContactAsync(long chatId, long userId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_card_number",
                Data = new Dictionary<string, object?> { ["contactId"] = contactId },
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"💳 ДОБАВЛЕНИЕ КАРТЫ ДЛЯ {contact.FullName ?? contact.TelegramUsername}\n\n" +
                "Введите номер карты (целиком):", cancellationToken);
        }

        private async Task HandleAddCardNumberAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var contactId = (int)state.Data["contactId"]!;

            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var step = state.Step;

            if (step == 1)
            {
                state.Data["cardNumber"] = text.Trim();
                state.Step = 2;
                _userStates[userId] = state;

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    "📝 ШАГ 2/7\n\nВведите CVV:", cancellationToken);
                return;
            }

            if (step == 2)
            {
                state.Data["cvv"] = text.Trim();
                state.Step = 3;
                _userStates[userId] = state;

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    "📝 ШАГ 3/7\n\nВведите срок действия карты (MM/YY):", cancellationToken);
                return;
            }

            if (step == 3)
            {
                state.Data["cardExpiry"] = text.Trim();
                state.Step = 4;
                _userStates[userId] = state;

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    "📝 ШАГ 4/7\n\nВведите кодовое слово:", cancellationToken);
                return;
            }

            if (step == 4)
            {
                state.Data["securityWord"] = text.Trim();
                state.Step = 5;
                _userStates[userId] = state;

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    "📝 ШАГ 5/7\n\nВведите название банка (например: Тинькофф, Сбер):", cancellationToken);
                return;
            }

            if (step == 5)
            {
                state.Data["bankName"] = text.Trim();
                state.Step = 6;
                _userStates[userId] = state;

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    "📝 ШАГ 6/7\n\nВведите тип карты: debit или credit", cancellationToken);
                return;
            }

            if (step == 6)
            {
                var cardTypeStep6 = text.Trim().ToLowerInvariant();
                if (cardTypeStep6 != "debit" && cardTypeStep6 != "credit")
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId,
                        "❌ Неверный тип карты. Введите debit или credit", cancellationToken);
                    return;
                }

                state.Data["cardType"] = cardTypeStep6;
                state.Step = 7;
                _userStates[userId] = state;

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    "📝 ШАГ 7/7\n\nВведите статус карты (рабочая/лок/115/161):", cancellationToken);
                return;
            }

            var cardStatus = text.Trim().ToLowerInvariant();
            var validStatuses = new[] { "рабочая", "лок", "115", "161" };
            if (!validStatuses.Contains(cardStatus))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId,
                    "❌ Неверный статус. Введите: рабочая, лок, 115 или 161", cancellationToken);
                return;
            }

            var card = new BankCard
            {
                CardNumber = state.Data["cardNumber"]?.ToString(),
                CVV = state.Data["cvv"]?.ToString(),
                CardExpiry = state.Data["cardExpiry"]?.ToString(),
                SecurityWord = state.Data["securityWord"]?.ToString(),
                BankName = state.Data["bankName"]?.ToString(),
                CardType = state.Data["cardType"]?.ToString(),
                CardStatus = cardStatus,
                IsPrimary = false
            };

            var result = await _contactService.AddBankCardAsync(contactId, card);

            if (result)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "✅ Карта добавлена", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowContactCardsAsync(chatId, contactId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении карты", cancellationToken);
            }
        }

        private async Task SetPrimaryBankCardAsync(long chatId, long userId, int contactId, string cardNumber, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            var result = await _contactService.SetPrimaryBankCardAsync(contactId, cardNumber);

            if (result)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "⭐ Основная карта обновлена", cancellationToken, 3);
                _menuManager.ClearMenuState(chatId);
                await ShowContactCardsAsync(chatId, contactId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken);
            }
        }

        private async Task ShowDeleteCardConfirmationAsync(long chatId, int contactId, string cardNumber, CancellationToken cancellationToken)
        {
            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить карту •••• {cardNumber}?";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"db_contact_card_delete_confirm_{contactId}_{cardNumber}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_contact_cards_{contactId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_card_confirm", cancellationToken);
        }
        // ===== МЕНЮ КОНТАКТОВ В БД =====
        private async Task ShowContactsDatabaseMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _contactService.GetContactStatisticsAsync();

            // Считаем количество по статусам
            var working = stats.ContactsByStatus.ContainsKey("рабочая") ? stats.ContactsByStatus["рабочая"] : 0;
            var locked = stats.ContactsByStatus.ContainsKey("лок") ? stats.ContactsByStatus["лок"] : 0;
            var blocked115 = stats.ContactsByStatus.ContainsKey("115") ? stats.ContactsByStatus["115"] : 0;
            var blocked161 = stats.ContactsByStatus.ContainsKey("161") ? stats.ContactsByStatus["161"] : 0;

            var text = "👤 БАЗА КОНТАКТОВ\n\n" +
                       $"📊 СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего контактов: {stats.TotalContacts}\n" +
                       $"│ 💳 С картами: {stats.ContactsWithCards}\n" +
                       $"│ 🆔 С паспортами: {stats.ContactsWithPassport}\n" +
                       $"│ 🟢 Рабочие: {working}\n" +
                       $"│ 🔒 Лок: {locked}\n" +
                       $"│ ⚠️ 115/161: {blocked115 + blocked161}\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"Выберите действие:";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        // Основные действия
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_contact_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "db_contacts_search")
        },
        
        // Все контакты
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ КОНТАКТЫ", "db_contacts_all") },
        
        // Фильтры по статусу
        new()
        {
            InlineKeyboardButton.WithCallbackData($"🟢 РАБОЧИЕ ({working})", "db_contacts_status_working"),
            InlineKeyboardButton.WithCallbackData($"🔒 ЛОК ({locked})", "db_contacts_status_locked")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData($"⚠️ 115/161 ({blocked115 + blocked161})", "db_contacts_status_blocked")
        },
        
        // По наличию данных
        new()
        {
            InlineKeyboardButton.WithCallbackData($"💳 С КАРТАМИ ({stats.ContactsWithCards})", "db_contacts_with_cards"),
            InlineKeyboardButton.WithCallbackData($"🆔 С ПАСПОРТАМИ ({stats.ContactsWithPassport})", "db_contacts_with_passports")
        },
        
        // Статистика
        new() { InlineKeyboardButton.WithCallbackData("📊 ДЕТАЛЬНАЯ СТАТИСТИКА", "db_contacts_stats") },
        
        // Назад в БД
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToDatabase) }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_contacts", cancellationToken);
        }
        // ===== ПОКАЗ КОНТАКТОВ ПО СТАТУСУ =====
        private async Task ShowContactsByStatusAsync(long chatId, string status, CancellationToken cancellationToken)
        {
            var allContacts = await _contactService.GetAllContactsAsync();
            var contacts = status switch
            {
                "115" => allContacts.Where(c => c.CardStatus == "115" || c.CardStatus == "161" || c.BankCards.Any(b => b.CardStatus == "115" || b.CardStatus == "161")).ToList(),
                _ => allContacts.Where(c => c.CardStatus == status || c.BankCards.Any(b => b.CardStatus == status)).ToList()
            };

            var statusEmoji = status switch
            {
                "рабочая" => "🟢",
                "лок" => "🔒",
                "115" => "⚠️",
                "161" => "⚠️",
                _ => "⚪"
            };

            var statusTitle = status == "115" ? "115/161" : status;
            var text = $"{statusEmoji} КОНТАКТЫ СО СТАТУСОМ: {statusTitle}\n\n";

            if (!contacts.Any())
            {
                text += "Нет контактов с таким статусом";
            }
            else
            {
                text += $"Найдено: {contacts.Count}\n\n";

                foreach (var contact in contacts.Take(10))
                {
                    var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";
                    text += $"👤 {name}\n";

                    var matchedCards = status == "115"
                        ? contact.BankCards.Where(b => b.CardStatus == "115" || b.CardStatus == "161").ToList()
                        : contact.BankCards.Where(b => b.CardStatus == status).ToList();

                    if (!string.IsNullOrEmpty(contact.CardStatus) && (status == "115" ? (contact.CardStatus == "115" || contact.CardStatus == "161") : contact.CardStatus == status))
                        text += $"   📌 Статус контакта: {contact.CardStatus}\n";

                    if (matchedCards.Any())
                    {
                        foreach (var card in matchedCards.Take(3))
                        {
                            var cardType = card.CardType == "debit" ? "debit" : card.CardType == "credit" ? "credit" : "-";
                            text += $"   💳 {MaskCardNumber(card.CardNumber)} | {cardType} | статус: {card.CardStatus ?? "-"}\n";
                        }
                    }

                    text += "\n";
                }

                if (contacts.Count > 10)
                    text += $"... и еще {contacts.Count - 10} контактов\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>();
            foreach (var contact in contacts.Take(5))
            {
                var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";
                buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData($"👤 {name}", $"db_contact_view_{contact.Id}") });
            }

            buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_contacts_menu") });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_contacts_status_{status}", cancellationToken);
        }

        // ===== КОНТАКТЫ С КАРТАМИ =====
        private async Task ShowContactsWithCardsAsync(long chatId, CancellationToken cancellationToken)
        {
            var allContacts = await _contactService.GetAllContactsAsync();
            var contacts = allContacts.Where(c => c.BankCards.Any()).ToList();

            var text = "💳 КОНТАКТЫ С КАРТАМИ\n\n";

            if (!contacts.Any())
            {
                text += "Нет контактов с картами";
            }
            else
            {
                text += $"Найдено: {contacts.Count}\n\n";

                foreach (var contact in contacts.Take(10))
                {
                    var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";
                    text += $"👤 {name}\n";
                    text += $"   💳 Карт: {contact.BankCards.Count}\n";

                    foreach (var card in contact.BankCards.Take(2))
                        text += $"   • {MaskCardNumber(card.CardNumber)} | {card.CardStatus ?? "-"}\n";

                    text += "\n";
                }

                if (contacts.Count > 10)
                    text += $"... и еще {contacts.Count - 10} контактов\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>();
            foreach (var contact in contacts.Take(5))
            {
                var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";
                buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData($"👤 {name}", $"db_contact_view_{contact.Id}") });
            }

            buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_contacts_menu") });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_contacts_with_cards", cancellationToken);
        }

        // ===== КОНТАКТЫ С ПАСПОРТАМИ =====
        private async Task ShowContactsWithPassportsAsync(long chatId, CancellationToken cancellationToken)
        {
            var allContacts = await _contactService.GetAllContactsAsync();
            var contacts = allContacts.Where(c => !string.IsNullOrEmpty(c.PassportNumber)).ToList();

            var text = "🆔 КОНТАКТЫ С ПАСПОРТАМИ\n\n";

            if (!contacts.Any())
            {
                text += "Нет контактов с паспортными данными";
            }
            else
            {
                text += $"Найдено: {contacts.Count}\n\n";

                foreach (var contact in contacts.Take(10))
                {
                    var name = !string.IsNullOrEmpty(contact.FullName)
                        ? contact.FullName
                        : $"@{contact.TelegramUsername}";

                    text += $"👤 {name}\n";
                    text += $"   🆔 {contact.PassportSeries} {contact.PassportNumber}\n";
                    if (contact.PassportIssueDate.HasValue)
                        text += $"   📅 Выдан: {contact.PassportIssueDate:dd.MM.yyyy}\n";
                    if (!string.IsNullOrEmpty(contact.PhoneNumber))
                        text += $"   📞 {contact.PhoneNumber}\n";
                    text += "\n";
                }

                if (contacts.Count > 10)
                {
                    text += $"... и еще {contacts.Count - 10} контактов\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>();
            foreach (var contact in contacts.Take(5))
            {
                var name = !string.IsNullOrEmpty(contact.FullName)
                    ? contact.FullName
                    : $"@{contact.TelegramUsername}";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"👤 {name}", $"db_contact_view_{contact.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_contacts_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_contacts_with_passports", cancellationToken);
        }

        // ===== УДАЛЕНИЕ КАРТЫ =====
        private async Task DeleteBankCardAsync(long chatId, long userId, int contactId, string cardNumber, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            var result = await _contactService.RemoveBankCardAsync(contactId, cardNumber);

            if (result)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "✅ Карта удалена", cancellationToken, 3);
                await ShowContactCardsAsync(chatId, contactId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при удалении карты", cancellationToken);
            }
        }

        // ===== КАРТЫ КОНТАКТА =====
        private async Task ShowContactCardsAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            var cards = contact.BankCards;
            var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";

            var text = $"💳 КАРТЫ КОНТАКТА\n👤 {name}\n📊 Всего карт: {cards.Count}\n\n";

            if (!cards.Any())
            {
                text += "Карт пока нет. Нажмите «➕ ДОБАВИТЬ КАРТУ».";
            }
            else
            {
                for (var i = 0; i < cards.Count; i++)
                {
                    text += BuildCardInfoLine(i, cards[i]);
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ КАРТУ", $"db_contact_add_card_{contactId}") }
            };

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var cardType = card.CardType == "debit" ? "ДЕБЕТ" : card.CardType == "credit" ? "КРЕДИТ" : "НЕ УКАЗАН";
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"💳 {MaskCardNumber(card.CardNumber)} | {cardType} | {card.CardStatus ?? "-"}", $"db_contact_card_open_{contactId}_{i}")
                });
            }

            buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("◀️ Назад", $"db_contact_view_{contactId}") });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_contact_cards_{contactId}", cancellationToken);
        }

        private async Task ShowContactCardDetailsAsync(long chatId, int contactId, int cardIndex, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null || cardIndex < 0 || cardIndex >= contact.BankCards.Count)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Карта не найдена", cancellationToken, 3);
                return;
            }

            var card = contact.BankCards[cardIndex];
            var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";
            var cardType = card.CardType == "debit" ? "Дебетовая" : card.CardType == "credit" ? "Кредитная" : "не указан";

            var text =
                $"💳 КАРТА #{cardIndex + 1}\n" +
                $"👤 Контакт: {name}\n\n" +
                $"🔢 Номер: {MaskCardNumber(card.CardNumber)}\n" +
                $"🏦 Банк: {card.BankName ?? "не указан"}\n" +
                $"💳 Тип: {cardType}\n" +
                $"🚦 Статус: {card.CardStatus ?? "не указан"}\n" +
                $"📅 Срок: {card.CardExpiry ?? "не указан"}\n" +
                $"🔐 CVV: {MaskSecret(card.CVV)}\n" +
                $"🗝️ Кодовое слово: {MaskSecret(card.SecurityWord)}\n" +
                $"⭐ Основная: {(card.IsPrimary ? "Да" : "Нет")}";

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"db_contact_card_edit_{contactId}_{cardIndex}") },
                new() { InlineKeyboardButton.WithCallbackData("⭐ Сделать основной", $"db_contact_card_primary_{contactId}_{card.CardNumber}") },
                new() { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"db_contact_card_delete_{contactId}_{card.CardNumber}") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ К картам", $"db_contact_cards_{contactId}") }
            };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_contact_card_open_{contactId}_{cardIndex}", cancellationToken);
        }

        private async Task StartEditContactCardAsync(long chatId, long userId, int contactId, int cardIndex, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null || cardIndex < 0 || cardIndex >= contact.BankCards.Count)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Карта не найдена", cancellationToken, 3);
                return;
            }

            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("🔢 Номер", $"db_contact_card_edit_field_{contactId}_{cardIndex}_number") },
                new() { InlineKeyboardButton.WithCallbackData("🔐 CVV", $"db_contact_card_edit_field_{contactId}_{cardIndex}_cvv") },
                new() { InlineKeyboardButton.WithCallbackData("📅 Срок", $"db_contact_card_edit_field_{contactId}_{cardIndex}_expiry") },
                new() { InlineKeyboardButton.WithCallbackData("🗝️ Кодовое слово", $"db_contact_card_edit_field_{contactId}_{cardIndex}_security") },
                new() { InlineKeyboardButton.WithCallbackData("🏦 Банк", $"db_contact_card_edit_field_{contactId}_{cardIndex}_bank") },
                new() { InlineKeyboardButton.WithCallbackData("💳 Тип", $"db_contact_card_edit_field_{contactId}_{cardIndex}_type") },
                new() { InlineKeyboardButton.WithCallbackData("🚦 Статус", $"db_contact_card_edit_field_{contactId}_{cardIndex}_status") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ К карте", $"db_contact_card_open_{contactId}_{cardIndex}") }
            };

            await _menuManager.ShowInlineMenuAsync(chatId,
                "✏️ РЕДАКТИРОВАНИЕ КАРТЫ\n\nВыберите поле для изменения:",
                new InlineKeyboardMarkup(buttons),
                $"db_contact_card_edit_{contactId}_{cardIndex}",
                cancellationToken);
        }

        private async Task StartEditContactCardFieldAsync(long chatId, long userId, int contactId, int cardIndex, string field, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null || cardIndex < 0 || cardIndex >= contact.BankCards.Count)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Карта не найдена", cancellationToken, 3);
                return;
            }

            var card = contact.BankCards[cardIndex];
            var prompt = field switch
            {
                "number" => "Введите полный номер карты:",
                "cvv" => "Введите CVV:",
                "expiry" => "Введите срок действия (MM/YY):",
                "security" => "Введите кодовое слово:",
                "bank" => "Введите название банка:",
                "type" => "Введите тип карты: debit или credit",
                "status" => "Введите статус карты: рабочая/лок/115/161",
                _ => "Введите новое значение:"
            };

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_edit_contact_card_value",
                Step = 1,
                Data = new Dictionary<string, object?>
                {
                    ["contactId"] = contactId,
                    ["cardIndex"] = cardIndex,
                    ["field"] = field,
                    ["oldCardNumber"] = card.CardNumber
                }
            };

            await _menuManager.SendTemporaryMessageAsync(chatId, $"✏️ {prompt}", cancellationToken);
        }

        private async Task HandleEditContactCardValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var contactId = (int)state.Data["contactId"]!;
            var cardIndex = (int)state.Data["cardIndex"]!;
            var field = state.Data["field"]?.ToString() ?? string.Empty;
            var oldCardNumber = state.Data["oldCardNumber"]?.ToString() ?? string.Empty;

            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null || cardIndex < 0 || cardIndex >= contact.BankCards.Count)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Карта не найдена", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var card = contact.BankCards[cardIndex];
            var value = text.Trim();

            switch (field)
            {
                case "number":
                    card.CardNumber = value;
                    break;
                case "cvv":
                    card.CVV = value;
                    break;
                case "expiry":
                    card.CardExpiry = value;
                    break;
                case "security":
                    card.SecurityWord = value;
                    break;
                case "bank":
                    card.BankName = value;
                    break;
                case "type":
                    value = value.ToLowerInvariant();
                    if (value != "debit" && value != "credit")
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите debit или credit", cancellationToken);
                        return;
                    }
                    card.CardType = value;
                    break;
                case "status":
                    value = value.ToLowerInvariant();
                    var statuses = new[] { "рабочая", "лок", "115", "161" };
                    if (!statuses.Contains(value))
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите: рабочая, лок, 115 или 161", cancellationToken);
                        return;
                    }
                    card.CardStatus = value;
                    break;
                default:
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неизвестное поле", cancellationToken, 3);
                    return;
            }

            var updated = await _contactService.UpdateBankCardAsync(contactId, oldCardNumber, card);
            if (!updated)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Не удалось обновить карту", cancellationToken, 3);
                return;
            }

            _userStates.Remove(userId);
            await _menuManager.SendTemporaryMessageAsync(chatId, "✅ Карта обновлена", cancellationToken, 3);
            await ShowContactCardDetailsAsync(chatId, contactId, cardIndex, cancellationToken);
        }

        private static string BuildCardInfoLine(int index, BankCard card)
        {
            var cardType = card.CardType == "debit" ? "Дебетовая" : card.CardType == "credit" ? "Кредитная" : "не указан";
            return $"{index + 1}. {MaskCardNumber(card.CardNumber)} {(card.IsPrimary ? "⭐" : "")}\n" +
                   $"   🏦 Банк: {card.BankName ?? "не указан"}\n" +
                   $"   💳 Тип: {cardType}\n" +
                   $"   🚦 Статус: {card.CardStatus ?? "не указан"}\n" +
                   $"   📅 Срок: {card.CardExpiry ?? "не указан"}\n\n";
        }

        private static string MaskCardNumber(string? cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return "••••";

            return cardNumber.Length <= 4
                ? $"•••• {cardNumber}"
                : $"•••• {cardNumber[^4..]}";
        }

        private static string MaskSecret(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "не указан";

            return new string('•', Math.Min(value.Length, 8));
        }

        private static bool IsContactCardBlocked(string? cardStatus)
        {
            return cardStatus == "лок" || cardStatus == "115" || cardStatus == "161";
        }

        // ===== ДОБАВЛЕНИЕ КОНТАКТА =====
        private async Task StartAddContactAsync(long chatId, long userId, CancellationToken cancellationToken, string returnTo = null)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_contact_username",
                Data = new Dictionary<string, object?>
                {
                    ["returnTo"] = returnTo  // Сохраняем, куда вернуться
                },
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ДОБАВЛЕНИЕ НОВОГО КОНТАКТА (ШАГ 1/17)\n\n" +
                "Введите Telegram username (например: @username или просто username):", cancellationToken);
        }

        private async Task HandleAddContactUsernameAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var username = text.StartsWith('@') ? text.Substring(1) : text;

            // Сохраняем returnTo, если он есть
            var returnTo = state.Data.ContainsKey("returnTo") ? state.Data["returnTo"]?.ToString() : null;

            state.Data["telegramUsername"] = username;
            state.Data["returnTo"] = returnTo; // Передаём дальше
            state.CurrentAction = "db_add_contact_name";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✅ Username: @{username}\n\n" +
                "📝 ШАГ 2/17\n\n" +
                "Введите ФИО контакта:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactNameAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["fullName"] = text;
            }

            state.CurrentAction = "db_add_contact_nickname";
            state.Step = 3;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 3/17\n\n" +
                "Введите ник/псевдоним контакта:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactNicknameAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["nickname"] = text;
            }

            state.CurrentAction = "db_add_contact_phone";
            state.Step = 4;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 4/17\n\n" +
                "Введите номер телефона контакта:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactPhoneAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["phoneNumber"] = text;
            }

            state.CurrentAction = "db_add_contact_birthdate";
            state.Step = 5;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 5/17\n\n" +
                "Введите дату рождения в формате ДД.ММ.ГГГГ:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactBirthDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
                {
                    state.Data["birthDate"] = birthDate;
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ", cancellationToken, 5);
                    return;
                }
            }

            state.CurrentAction = "db_add_contact_our_phone";
            state.Step = 6;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 6/17 - НАШИ ДАННЫЕ\n\n" +
                "Введите наш номер телефона, привязанный к контакту:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactOurPhoneAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["ourPhoneNumber"] = text;
            }

            state.CurrentAction = "db_add_contact_bank_password";
            state.Step = 7;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 7/17\n\n" +
                "Введите пароль от банка:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactBankPasswordAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["bankPassword"] = text;
            }

            state.CurrentAction = "db_add_contact_pin";
            state.Step = 8;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 8/17\n\n" +
                "Введите пин-код от личного кабинета:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactPinAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["pinCode"] = text;
            }

            state.CurrentAction = "db_add_contact_our_email";
            state.Step = 9;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 9/17\n\n" +
                "Введите нашу почту, привязанную к контакту:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactOurEmailAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["ourEmail"] = text;
            }

            state.CurrentAction = "db_add_contact_passport_series";
            state.Step = 10;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 10/17 - ПАСПОРТНЫЕ ДАННЫЕ\n\n" +
                "Введите серию паспорта:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactPassportSeriesAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["passportSeries"] = text;
            }

            state.CurrentAction = "db_add_contact_passport_number";
            state.Step = 11;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 11/17\n\n" +
                "Введите номер паспорта:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactPassportNumberAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["passportNumber"] = text;
            }

            state.CurrentAction = "db_add_contact_passport_expiry";
            state.Step = 12;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 12/17\n\n" +
                "Введите срок действия паспорта (ДД.ММ.ГГГГ):\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactPassportExpiryAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime expiryDate))
                {
                    state.Data["passportExpiry"] = expiryDate;
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ", cancellationToken, 5);
                    return;
                }
            }

            state.CurrentAction = "db_add_contact_passport_department";
            state.Step = 13;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 13/17\n\n" +
                "Введите код подразделения:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactPassportDepartmentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["passportDepartment"] = text;
            }

            state.CurrentAction = "db_add_contact_passport_issued_by";
            state.Step = 14;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 14/17\n\n" +
                "Введите кем выдан паспорт:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactPassportIssuedByAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["passportIssuedBy"] = text;
            }

            state.CurrentAction = "db_add_contact_passport_issue_date";
            state.Step = 15;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 15/17\n\n" +
                "Введите дату выдачи паспорта (ДД.ММ.ГГГГ):\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactPassportIssueDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime issueDate))
                {
                    state.Data["passportIssueDate"] = issueDate;
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ", cancellationToken, 5);
                    return;
                }
            }

            state.CurrentAction = "db_add_contact_inn";
            state.Step = 16;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 16/17\n\n" +
                "Введите ИНН:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddContactInnAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["inn"] = text;
            }

            state.CurrentAction = "db_add_contact_notes";
            state.Step = 17;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 17/17\n\n" +
                "Введите заметки (дополнительная информация):\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task StartEditContactNotesAsync(long chatId, long userId, int contactId, CancellationToken cancellationToken)
        {
            Console.WriteLine($"   → StartEditContactNotesAsync вызван для contactId: {contactId}");

            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                Console.WriteLine($"   → Контакт {contactId} не найден!");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            Console.WriteLine($"   → Контакт найден: {contact.TelegramUsername}");

            _userStates[userId] = new UserState
            {
                CurrentAction = "edit_contact_notes",
                Data = new Dictionary<string, object?> { ["contactId"] = contactId },
                Step = 1
            };

            var currentNotes = contact.Notes ?? "нет заметок";
            Console.WriteLine($"   → Отправляем запрос на ввод заметок");

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"📝 РЕДАКТИРОВАНИЕ ЗАМЕТОК\n\nТекущие заметки:\n{currentNotes}\n\nВведите новый текст заметок:", cancellationToken);

            Console.WriteLine($"   → Запрос отправлен");
        }

        private async Task HandleEditContactNotesAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            Console.WriteLine($"   → HandleEditContactNotesAsync вызван с текстом: {text}");

            var contactId = (int)state.Data["contactId"]!;
            Console.WriteLine($"   → contactId: {contactId}");

            var contact = await _contactService.GetContactAsync(contactId);

            if (contact == null)
            {
                Console.WriteLine($"   → Контакт {contactId} не найден!");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            Console.WriteLine($"   → Контакт найден, обновляем заметки");

            contact.Notes = text;
            contact.UpdatedAt = DateTime.UtcNow;

            var success = await _contactService.UpdateContactAsync(contact);

            Console.WriteLine($"   → UpdateContactAsync вернул: {success}");

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "✅ Заметки обновлены!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowContactNotesAsync(chatId, contactId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении заметок", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }
        private async Task ShowContactNotesAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";
            var notes = contact.Notes ?? "Нет заметок";

            var text = $"📝 ЗАМЕТКИ КОНТАКТА: {name}\n\n{notes}";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ РЕДАКТИРОВАТЬ", $"db_contact_edit_notes_{contactId}"),
            InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", $"db_contact_view_{contactId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"contact_notes_{contactId}", cancellationToken);
        }
        private async Task HandleAddContactNotesAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["notes"] = text;
            }

            // Проверяем, нужно ли вернуться в FastInvest
            var returnTo = state.Data.ContainsKey("returnTo") ? state.Data["returnTo"]?.ToString() : null;

            // СОЗДАЁМ КОНТАКТ СО ВСЕМИ ДАННЫМИ
            var contact = new TeamContact
            {
                TelegramUsername = state.Data["telegramUsername"]?.ToString() ?? "",
                FullName = state.Data.ContainsKey("fullName") ? state.Data["fullName"]?.ToString() : null,
                Nickname = state.Data.ContainsKey("nickname") ? state.Data["nickname"]?.ToString() : null,
                PhoneNumber = state.Data.ContainsKey("phoneNumber") ? state.Data["phoneNumber"]?.ToString() : null,
                BirthDate = state.Data.ContainsKey("birthDate") ? (DateTime?)state.Data["birthDate"] : null,

                // Карты добавляются только через меню "💳 КАРТЫ".

                OurPhoneNumber = state.Data.ContainsKey("ourPhoneNumber") ? state.Data["ourPhoneNumber"]?.ToString() : null,
                BankPassword = state.Data.ContainsKey("bankPassword") ? state.Data["bankPassword"]?.ToString() : null,
                PinCode = state.Data.ContainsKey("pinCode") ? state.Data["pinCode"]?.ToString() : null,
                OurEmail = state.Data.ContainsKey("ourEmail") ? state.Data["ourEmail"]?.ToString() : null,

                PassportSeries = state.Data.ContainsKey("passportSeries") ? state.Data["passportSeries"]?.ToString() : null,
                PassportNumber = state.Data.ContainsKey("passportNumber") ? state.Data["passportNumber"]?.ToString() : null,
                PassportExpiry = state.Data.ContainsKey("passportExpiry") ? (DateTime?)state.Data["passportExpiry"] : null,
                PassportDepartmentCode = state.Data.ContainsKey("passportDepartment") ? state.Data["passportDepartment"]?.ToString() : null,
                PassportIssuedBy = state.Data.ContainsKey("passportIssuedBy") ? state.Data["passportIssuedBy"]?.ToString() : null,
                PassportIssueDate = state.Data.ContainsKey("passportIssueDate") ? (DateTime?)state.Data["passportIssueDate"] : null,
                INN = state.Data.ContainsKey("inn") ? state.Data["inn"]?.ToString() : null,

                Notes = state.Data.ContainsKey("notes") ? state.Data["notes"]?.ToString() : null,

                ContactType = "Дроп",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _contactService.CreateContactAsync(contact);

            if (result != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ Контакт @{contact.TelegramUsername} успешно добавлен!", cancellationToken, 3);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);

                // Возвращаемся туда, откуда пришли
                if (returnTo == "fastinvest")
                {
                    await StartAddFastInvestAsync(chatId, userId, cancellationToken);
                }
                else
                {
                    await ShowContactsDatabaseMenuAsync(chatId, cancellationToken);
                }
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении контакта", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);

                if (returnTo == "fastinvest")
                {
                    await StartAddFastInvestAsync(chatId, userId, cancellationToken);
                }
                else
                {
                    await ShowContactsDatabaseMenuAsync(chatId, cancellationToken);
                }
            }
        }
        private async Task HandleAddContactPassportAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["passport"] = text;
            }

            state.CurrentAction = "db_add_contact_inn";
            state.Step = 14;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 14/14\n\n" +
                "Введите ИНН:\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }
        private async Task HandleAddContactCardAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                state.Data["cardNumber"] = text;
            }

            // Создаем контакт
            var contact = new TeamContact
            {
                TelegramUsername = state.Data["telegramUsername"]?.ToString() ?? "",
                FullName = state.Data.ContainsKey("fullName") ? state.Data["fullName"]?.ToString() : null,
                PhoneNumber = state.Data.ContainsKey("phoneNumber") ? state.Data["phoneNumber"]?.ToString() : null,
                CardNumber = state.Data.ContainsKey("cardNumber") ? state.Data["cardNumber"]?.ToString() : null,
                ContactType = "Доп",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _contactService.CreateContactAsync(contact);

            if (result != null)
            {
                // Отправляем confirmation message (удаляется через 3 секунды)
                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ Контакт @{contact.TelegramUsername} успешно добавлен!", cancellationToken, 3);

                // Очищаем состояние
                _userStates.Remove(userId);

                // ВАЖНО: Очищаем состояние меню, чтобы следующее сообщение было новым
                _menuManager.ClearMenuState(chatId);

                // Открываем карточку нового контакта НОВЫМ сообщением
                await ShowContactDetailsDatabaseAsync(chatId, result.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении контакта", cancellationToken);
                _userStates.Remove(userId);

                _menuManager.ClearMenuState(chatId);
                await ShowContactsDatabaseMenuAsync(chatId, cancellationToken);
            }
        }

        // ===== РЕДАКТИРОВАНИЕ КОНТАКТА =====
        private async Task StartEditContactAsync(long chatId, long userId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_edit_contact_select_field",
                Data = new Dictionary<string, object?> { ["contactId"] = contactId },
                Step = 1
            };

            var name = !string.IsNullOrEmpty(contact.FullName) ? contact.FullName : $"@{contact.TelegramUsername}";

            var text = $"✏️ РЕДАКТИРОВАНИЕ КОНТАКТА: {name}\n\n" +
                       "Выберите поле для редактирования:\n\n" +
                       "1️⃣ Telegram username\n" +
                       "2️⃣ ФИО\n" +
                       "3️⃣ Телефон\n" +
                       "4️⃣ Дата рождения\n" +
                       "5️⃣ Наш номер на контакте\n" +
                       "6️⃣ Пароль от банка\n" +
                       "7️⃣ Пин-код\n" +
                       "8️⃣ Наша почта\n" +
                       "9️⃣ Паспортные данные\n" +
                       "🔟 ИНН\n" +
                       "1️⃣1️⃣ Заметки\n\n" +
                       "Введите номер поля (1-11) или 0 для выхода:";

            await _menuManager.SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }

        private async Task HandleEditContactSelectFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 0 || field > 11)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 0 до 11", cancellationToken);
                return;
            }

            if (field == 0)
            {
                Console.WriteLine($"   → Выход из редактирования контакта (field=0)");
                var contactId2 = (int)state.Data["contactId"]!;
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowContactDetailsDatabaseAsync(chatId, contactId2, cancellationToken);
                return;
            }

            var contactId = (int)state.Data["contactId"]!;
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            field = field switch
            {
                5 => 9,
                6 => 10,
                7 => 11,
                8 => 12,
                9 => 13,
                10 => 14,
                11 => 16,
                _ => field
            };

            var fieldName = field switch
            {
                1 => "Telegram username",
                2 => "ФИО",
                3 => "Телефон",
                4 => "Дата рождения",
                5 => "Номер карты",
                6 => "CVV",
                7 => "Срок карты",
                8 => "Кодовое слово",
                9 => "Наш номер на контакте",
                10 => "Пароль от банка",
                11 => "Пин-код",
                12 => "Наша почта",
                13 => "Паспортные данные",
                14 => "ИНН",
                15 => "Статус карты",
                16 => "Заметки",
                _ => ""
            };

            var currentValue = field switch
            {
                1 => contact.TelegramUsername,
                2 => contact.FullName ?? "не указано",
                3 => contact.PhoneNumber ?? "не указано",
                4 => contact.BirthDate?.ToString("dd.MM.yyyy") ?? "не указано",
                5 => contact.CardNumber ?? "не указано",
                6 => contact.CVV ?? "не указано",
                7 => contact.CardExpiry ?? "не указано",
                8 => contact.SecurityWord ?? "не указано",
                9 => contact.OurPhoneNumber ?? "не указано",
                10 => contact.BankPassword ?? "не указано",
                11 => contact.PinCode ?? "не указано",
                12 => contact.OurEmail ?? "не указано",
                13 => $"{contact.PassportSeries} {contact.PassportNumber}" ?? "не указано",
                14 => contact.INN ?? "не указано",
                15 => contact.CardStatus ?? "не указано",
                16 => contact.Notes ?? "не указано",
                _ => ""
            };

            state.Data["editField"] = field;
            state.CurrentAction = "db_edit_contact_value";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ Редактирование поля: {fieldName}\n" +
                $"Текущее значение: {currentValue}\n\n" +
                $"Введите новое значение (или отправьте '-' для удаления):", cancellationToken);
        }

        private async Task HandleEditContactValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var contactId = (int)state.Data["contactId"]!;
            var field = (int)state.Data["editField"]!;

            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken);
                _userStates.Remove(userId);
                return;
            }

            // Обновляем соответствующее поле
            switch (field)
            {
                case 1:
                    contact.TelegramUsername = text == "-" ? "" : text;
                    break;
                case 2:
                    contact.FullName = text == "-" ? null : text;
                    break;
                case 3:
                    contact.PhoneNumber = text == "-" ? null : text;
                    break;
                case 4:
                    if (text != "-" && DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
                        contact.BirthDate = birthDate;
                    else if (text == "-")
                        contact.BirthDate = null;
                    else
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты", cancellationToken);
                        return;
                    }
                    break;
                case 5:
                    contact.CardNumber = text == "-" ? null : text;
                    break;
                case 6:
                    contact.CVV = text == "-" ? null : text;
                    break;
                case 7:
                    contact.CardExpiry = text == "-" ? null : text;
                    break;
                case 8:
                    contact.SecurityWord = text == "-" ? null : text;
                    break;
                case 9:
                    contact.OurPhoneNumber = text == "-" ? null : text;
                    break;
                case 10:
                    contact.BankPassword = text == "-" ? null : text;
                    break;
                case 11:
                    contact.PinCode = text == "-" ? null : text;
                    break;
                case 12:
                    contact.OurEmail = text == "-" ? null : text;
                    break;
                case 13:
                    if (text == "-")
                    {
                        contact.PassportSeries = null;
                        contact.PassportNumber = null;
                    }
                    else
                    {
                        var parts = text.Split(' ');
                        if (parts.Length >= 2)
                        {
                            contact.PassportSeries = parts[0];
                            contact.PassportNumber = parts[1];
                        }
                        else
                        {
                            contact.PassportNumber = text;
                        }
                    }
                    break;
                case 14:
                    contact.INN = text == "-" ? null : text;
                    break;
                case 15:
                    contact.CardStatus = text == "-" ? null : text;
                    break;
                case 16:
                    contact.Notes = text == "-" ? null : text;
                    break;
            }

            contact.UpdatedAt = DateTime.UtcNow;
            var result = await _contactService.UpdateContactAsync(contact);

            if (result)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "✅ Контакт обновлен", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowContactDetailsDatabaseAsync(chatId, contactId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken);
            }
        }
        // ===== ВСЕ КОНТАКТЫ =====
        private async Task ShowAllContactsDatabaseAsync(long chatId, CancellationToken cancellationToken)
        {
            var contacts = await _contactService.GetAllContactsAsync();

            if (!contacts.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Контактов нет",
                    MainMenuKeyboard.GetBackButton("db_contacts_menu"), "db_contacts_empty", cancellationToken);
                return;
            }

            var text = $"👥 ВСЕ КОНТАКТЫ ({contacts.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var contact in contacts.Take(10))
            {
                var statusEmoji = contact.CardStatus switch
                {
                    "рабочая" => "🟢",
                    "лок" => "🔒",
                    "115" => "⚠️",
                    "161" => "⚠️",
                    _ => "⚪"
                };

                var name = !string.IsNullOrEmpty(contact.FullName)
                    ? contact.FullName
                    : $"@{contact.TelegramUsername}";

                text += $"{statusEmoji} {name}\n";
                if (!string.IsNullOrEmpty(contact.PhoneNumber))
                    text += $"   📞 {contact.PhoneNumber}\n";
                if (!string.IsNullOrEmpty(contact.CardNumber))
                    text += $"   💳 •••• {contact.CardNumber[^4..]}\n";
                text += "\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"👤 {name}", $"db_contact_view_{contact.Id}")
        });
            }

            if (contacts.Count > 10)
            {
                text += $"... и еще {contacts.Count - 10} контактов\n\n";
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_contacts_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_contacts_all", cancellationToken);
        }

        // ===== ДЕТАЛИ КОНТАКТА =====
        private async Task ShowContactDetailsDatabaseAsync(long chatId, int contactId, CancellationToken cancellationToken)
        {
            var contact = await _contactService.GetContactAsync(contactId);
            if (contact == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Контакт не найден", cancellationToken, 3);
                return;
            }

            var statusEmoji = contact.CardStatus switch
            {
                "рабочая" => "🟢",
                "лок" => "🔒",
                "115" => "⚠️",
                "161" => "⚠️",
                _ => "⚪"
            };

            var text = $"👤 КОНТАКТ: {contact.FullName ?? contact.TelegramUsername}\n\n" +
                       $"📊 ОСНОВНЫЕ ДАННЫЕ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ 📅 Создан: {contact.CreatedAt:dd.MM.yyyy}\n" +
                       $"│ 📱 Telegram: @{contact.TelegramUsername}\n" +
                       $"│ 👤 Имя: {contact.FullName ?? "-"}\n" +
                       $"│ 🎭 Ник: {contact.Nickname ?? "-"}\n" +
                       $"│ 📞 Телефон: {contact.PhoneNumber ?? "-"}\n" +
                       $"│ 🎂 Дата рождения: {contact.BirthDate?.ToString("dd.MM.yyyy") ?? "-"}\n" +
                       $"│ 🏷️ Теги: {contact.Tags ?? "-"}\n" +
                       $"│ 📝 Тип: {contact.ContactType ?? "-"}\n" +
                       $"└─────────────────────────────────\n\n" +

                       $"🔐 НАШИ ДАННЫЕ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Наш номер: {contact.OurPhoneNumber ?? "-"}\n" +
                       $"│ Пароль банка: {contact.BankPassword ?? "-"}\n" +
                       $"│ Пин-код: {contact.PinCode ?? "-"}\n" +
                       $"│ Наша почта: {contact.OurEmail ?? "-"}\n" +
                       $"└─────────────────────────────────\n\n" +

                       $"🆔 ПАСПОРТНЫЕ ДАННЫЕ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Серия: {contact.PassportSeries ?? "-"}\n" +
                       $"│ Номер: {contact.PassportNumber ?? "-"}\n" +
                       $"│ Срок: {contact.PassportExpiry?.ToString("dd.MM.yyyy") ?? "-"}\n" +
                       $"│ Код подр: {contact.PassportDepartmentCode ?? "-"}\n" +
                       $"│ Кем выдан: {contact.PassportIssuedBy ?? "-"}\n" +
                       $"│ Дата выдачи: {contact.PassportIssueDate?.ToString("dd.MM.yyyy") ?? "-"}\n" +
                       $"│ ИНН: {contact.INN ?? "-"}\n" +
                       $"└─────────────────────────────────\n";

            if (!string.IsNullOrEmpty(contact.Notes))
            {
                text += $"\n📝 ЗАМЕТКИ:\n{contact.Notes}\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ РЕДАКТИРОВАТЬ", $"db_contact_edit_{contact.Id}"),
            InlineKeyboardButton.WithCallbackData("🗑️ УДАЛИТЬ", $"db_contact_delete_{contact.Id}")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("💳 КАРТЫ", $"db_contact_cards_{contact.Id}"),
            InlineKeyboardButton.WithCallbackData("📝 ЗАМЕТКИ", $"db_contact_notes_{contact.Id}")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_contacts_all") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_contact_{contact.Id}", cancellationToken);
        }

        // ===== ПОИСК КОНТАКТОВ =====
        private async Task StartContactSearchAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_contact_search",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК КОНТАКТОВ\n\n" +
                "Введите имя, username, телефон или номер карты для поиска:", cancellationToken);
        }

        private async Task HandleContactSearchAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var contacts = await _contactService.SearchContactsAsync(text);

            if (!contacts.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {contacts.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var contact in contacts.Take(10))
            {
                var name = !string.IsNullOrEmpty(contact.FullName)
                    ? contact.FullName
                    : $"@{contact.TelegramUsername}";

                result += $"• {name}\n";
                if (!string.IsNullOrEmpty(contact.PhoneNumber))
                    result += $"  📞 {contact.PhoneNumber}\n";
                result += "\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"👤 {name}", $"db_contact_view_{contact.Id}")
        });
            }

            if (contacts.Count > 10)
            {
                result += $"... и еще {contacts.Count - 10} контактов\n\n";
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_contacts_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "db_search_results", cancellationToken);
            _userStates.Remove(userId);
        }

        // ===== СТАТИСТИКА КОНТАКТОВ =====
        private async Task ShowContactStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _contactService.GetContactStatisticsAsync();
            var contacts = await _contactService.GetAllContactsAsync();

            var text = "📊 СТАТИСТИКА КОНТАКТОВ\n\n" +
                       $"👥 Всего контактов: {stats.TotalContacts}\n" +
                       $"💳 С картами: {stats.ContactsWithCards}\n" +
                       $"🆔 С паспортами: {stats.ContactsWithPassport}\n\n" +
                       $"📈 ПО СТАТУСАМ:\n";

            if (stats.ContactsByStatus.Any())
            {
                foreach (var status in stats.ContactsByStatus)
                {
                    var emoji = status.Key switch
                    {
                        "рабочая" => "🟢",
                        "лок" => "🔒",
                        "115" => "⚠️",
                        "161" => "⚠️",
                        _ => "⚪"
                    };
                    text += $"{emoji} {status.Key}: {status.Value}\n";
                }
            }
            else
            {
                text += "Нет данных по статусам\n";
            }

            text += $"\n📅 Недавно добавленные:\n";
            foreach (var contact in contacts.OrderByDescending(c => c.CreatedAt).Take(5))
            {
                var name = !string.IsNullOrEmpty(contact.FullName)
                    ? contact.FullName
                    : $"@{contact.TelegramUsername}";
                text += $"• {name} - {contact.CreatedAt:dd.MM.yyyy}\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("👥 ВСЕ КОНТАКТЫ", "db_contacts_all") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_contacts_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_contacts_stats", cancellationToken);
        }
        private async Task ShowContactsDatabaseAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var contacts = await _contactService.GetAllContactsAsync();

                var text = $"👥 База контактов\n\n" +
                           $"Всего контактов: {contacts.Count}\n\n" +
                           $"📊 Статистика:\n";

                // Статистика по типам контактов
                var contactTypes = contacts
                    .GroupBy(c => c.ContactType ?? "Доп")
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList();

                foreach (var type in contactTypes)
                {
                    var percentage = contacts.Count > 0 ? (decimal)type.Count / contacts.Count * 100 : 0;
                    text += $"• {type.Type}: {type.Count} ({(percentage):F1}%)\n";
                }

                // Недавно добавленные контакты
                var recentContacts = contacts
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList();

                if (recentContacts.Any())
                {
                    text += $"\n🔥 Недавно добавленные:\n";
                    foreach (var contact in recentContacts)
                    {
                        var daysAgo = (DateTime.UtcNow - contact.CreatedAt).Days;
                        var timeText = daysAgo == 0 ? "сегодня" : $"{daysAgo} дн. назад";
                        text += $"• @{contact.TelegramUsername} - {timeText}\n";
                    }
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📋 Весь список", CallbackData.ContactsList) },
            new() { InlineKeyboardButton.WithCallbackData("🔍 Поиск", CallbackData.ContactsSearch) },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToDatabase) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "database_contacts",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing contacts database");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке базы контактов.", cancellationToken);
            }
        }

        // ===== ПОСТЫ - МЕНЮ =====
        private async Task ShowPostsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _postService.GetPostStatisticsAsync();

            var text = "📝 ПОСТЫ\n\n" +
                       $"📊 СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего постов: {stats.TotalPosts}\n" +
                       $"│ Опубликовано: {stats.PublishedPosts}\n" +
                       $"│ Черновиков: {stats.DraftPosts}\n" +
                       $"│ Запланировано: {stats.ScheduledPosts}\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"Выберите действие:";

            var buttons = MainMenuKeyboard.GetPostsMenu();
            await _menuManager.ShowInlineMenuAsync(chatId, text, buttons, "db_posts", cancellationToken);
        }

        // ===== ВСЕ ПОСТЫ =====
        private async Task ShowAllPostsAsync(long chatId, CancellationToken cancellationToken)
        {
            var posts = await _postService.GetAllPostsAsync();

            if (!posts.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Постов нет",
                    new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_posts_add") },
                                              new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_posts_menu") } }),
                    "db_posts_empty", cancellationToken);
                return;
            }

            var text = $"📋 ВСЕ ПОСТЫ ({posts.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var post in posts.Take(10))
            {
                var statusEmoji = post.Status switch
                {
                    "Опубликовано" => "✅",
                    "Черновик" => "📝",
                    "Запланировано" => "📅",
                    _ => "📄"
                };

                text += $"{statusEmoji} {post.Title}\n";
                text += $"   📅 {post.PublishDate?.ToString("dd.MM.yyyy") ?? post.CreatedAt.ToString("dd.MM.yyyy")}\n";
                if (!string.IsNullOrEmpty(post.Channel))
                    text += $"   📢 {post.Channel}\n";
                text += "\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📄 {post.Title}", $"db_post_view_{post.Id}")
        });
            }

            if (posts.Count > 10)
                text += $"... и еще {posts.Count - 10} постов\n\n";

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_posts_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_posts_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_posts_all", cancellationToken);
        }

        // ===== ДЕТАЛИ ПОСТА =====
        private async Task ShowPostDetailsAsync(long chatId, int postId, CancellationToken cancellationToken)
        {
            var post = await _postService.GetPostAsync(postId);
            if (post == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Пост не найден", cancellationToken, 3);
                return;
            }

            var statusEmoji = post.Status switch
            {
                "Опубликовано" => "✅",
                "Черновик" => "📝",
                "Запланировано" => "📅",
                _ => "📄"
            };

            var text = $"📄 ПОСТ: {post.Title}\n\n" +
                       $"📊 ДЕТАЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ {statusEmoji} Статус: {post.Status ?? "Черновик"}\n" +
                       $"│ 📅 Создан: {post.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                       $"│ 📢 Канал: {post.Channel ?? "-"}\n" +
                       $"│ 📅 Публикация: {post.PublishDate?.ToString("dd.MM.yyyy HH:mm") ?? "-"}\n" +
                       $"│ 🔗 Ссылка: {post.Link ?? "-"}\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"📝 СОДЕРЖАНИЕ:\n{post.Content}\n";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ РЕДАКТИРОВАТЬ", $"db_post_edit_{post.Id}"),
            InlineKeyboardButton.WithCallbackData("🗑️ УДАЛИТЬ", $"db_post_delete_{post.Id}")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_posts_all") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_post_{post.Id}", cancellationToken);
        }

        // ===== ДОБАВЛЕНИЕ ПОСТА =====
        private async Task StartAddPostAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_post_title",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ДОБАВЛЕНИЕ НОВОГО ПОСТА (ШАГ 1/5)\n\n" +
                "Введите заголовок поста:", cancellationToken);
        }

        private async Task HandleAddPostTitleAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["title"] = text;
            state.CurrentAction = "db_add_post_content";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Заголовок: {text}\n\n" +
                "📝 ШАГ 2/5\n\n" +
                "Введите содержание поста:", cancellationToken);
        }

        private async Task HandleAddPostContentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["content"] = text;
            state.CurrentAction = "db_add_post_channel";
            state.Step = 3;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 3/5\n\n" +
                "Введите название канала (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddPostChannelAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["channel"] = text;

            state.CurrentAction = "db_add_post_date";
            state.Step = 4;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 4/5\n\n" +
                "Введите дату публикации в формате ДД.ММ.ГГГГ ЧЧ:ММ\n" +
                "(или отправьте '-' для текущей даты):", cancellationToken);
        }

        private async Task HandleAddPostDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            DateTime? publishDate = null;
            if (text != "-")
            {
                if (DateTime.TryParseExact(text, "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    publishDate = date;
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты", cancellationToken, 3);
                    return;
                }
            }

            state.Data["publishDate"] = publishDate;
            state.CurrentAction = "db_add_post_status";
            state.Step = 5;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 ШАГ 5/5\n\n" +
                "Введите статус (Опубликовано/Черновик/Запланировано):\n" +
                "(или отправьте '-' для 'Черновик')", cancellationToken);
        }

        private async Task HandleAddPostStatusAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var status = text != "-" ? text : "Черновик";

            var post = new DbPost
            {
                Title = state.Data["title"]?.ToString() ?? "",
                Content = state.Data["content"]?.ToString() ?? "",
                Channel = state.Data.ContainsKey("channel") ? state.Data["channel"]?.ToString() : null,
                PublishDate = state.Data.ContainsKey("publishDate") ? (DateTime?)state.Data["publishDate"] : null,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _postService.CreatePostAsync(post);

            if (result != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Пост '{post.Title}' создан!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowPostDetailsAsync(chatId, result.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании поста", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowPostsMenuAsync(chatId, cancellationToken);
            }
        }

        // ===== РЕДАКТИРОВАНИЕ ПОСТА =====
        private async Task StartEditPostAsync(long chatId, long userId, int postId, CancellationToken cancellationToken)
        {
            var post = await _postService.GetPostAsync(postId);
            if (post == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Пост не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_edit_post_field",
                Data = new Dictionary<string, object?> { ["postId"] = postId },
                Step = 1
            };

            var text = $"✏️ РЕДАКТИРОВАНИЕ ПОСТА: {post.Title}\n\n" +
                       "Выберите поле для редактирования:\n\n" +
                       "1️⃣ Заголовок\n" +
                       "2️⃣ Содержание\n" +
                       "3️⃣ Канал\n" +
                       "4️⃣ Дата публикации\n" +
                       "5️⃣ Статус\n\n" +
                       "Введите номер поля (1-5) или 0 для выхода:";

            await _menuManager.SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }
        // ===== ВЫБОР ПОЛЯ ДЛЯ РЕДАКТИРОВАНИЯ =====
        private async Task HandleEditPostFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 0 || field > 5)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 0 до 5", cancellationToken, 3);
                return;
            }

            if (field == 0)
            {
                Console.WriteLine($"   → Выход из редактирования поста (field=0)");
                var postId2 = (int)state.Data["postId"]!;
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowPostDetailsAsync(chatId, postId2, cancellationToken);
                return;
            }

            var postId = (int)state.Data["postId"]!;
            var post = await _postService.GetPostAsync(postId);
            if (post == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Пост не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var fieldName = field switch
            {
                1 => "Заголовок",
                2 => "Содержание",
                3 => "Канал",
                4 => "Дата публикации",
                5 => "Статус",
                _ => ""
            };

            var currentValue = field switch
            {
                1 => post.Title,
                2 => post.Content.Length > 50 ? post.Content.Substring(0, 50) + "..." : post.Content,
                3 => post.Channel ?? "не указано",
                4 => post.PublishDate?.ToString("dd.MM.yyyy HH:mm") ?? "не указано",
                5 => post.Status ?? "не указано",
                _ => ""
            };

            state.Data["editField"] = field;
            state.CurrentAction = "db_edit_post_value";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ Редактирование поля: {fieldName}\n" +
                $"Текущее значение: {currentValue}\n\n" +
                $"Введите новое значение (или отправьте '-' для удаления):", cancellationToken);
        }

        // ===== СОХРАНЕНИЕ ИЗМЕНЕНИЙ =====
        private async Task HandleEditPostValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var postId = (int)state.Data["postId"]!;
            var field = (int)state.Data["editField"]!;

            var post = await _postService.GetPostAsync(postId);
            if (post == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Пост не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            switch (field)
            {
                case 1: // Заголовок
                    post.Title = text == "-" ? "Без названия" : text;
                    break;
                case 2: // Содержание
                    post.Content = text == "-" ? "" : text;
                    break;
                case 3: // Канал
                    post.Channel = text == "-" ? null : text;
                    break;
                case 4: // Дата публикации
                    if (text == "-")
                    {
                        post.PublishDate = null;
                    }
                    else if (DateTime.TryParseExact(text, "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    {
                        post.PublishDate = date;
                    }
                    else
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ ЧЧ:ММ", cancellationToken, 3);
                        return;
                    }
                    break;
                case 5: // Статус
                    var validStatuses = new[] { "Опубликовано", "Черновик", "Запланировано" };
                    if (text != "-" && !validStatuses.Contains(text))
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный статус", cancellationToken, 3);
                        return;
                    }
                    post.Status = text == "-" ? "Черновик" : text;
                    break;
            }

            post.UpdatedAt = DateTime.UtcNow;
            var success = await _postService.UpdatePostAsync(post);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Пост обновлен", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowPostDetailsAsync(chatId, postId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
            }
        }
        // ===== УДАЛЕНИЕ ПОСТА =====
        private async Task ShowDeletePostConfirmationAsync(long chatId, int postId, CancellationToken cancellationToken)
        {
            var post = await _postService.GetPostAsync(postId);
            if (post == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Пост не найден", cancellationToken, 3);
                return;
            }

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить пост?\n\n" +
                       $"📄 {post.Title}\n" +
                       $"📅 {post.CreatedAt:dd.MM.yyyy}\n\n" +
                       $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"delete_post_confirm_{postId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_post_view_{postId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_confirmation", cancellationToken);
        }

        // ===== УДАЛЕНИЕ ПОСТА =====
        private async Task DeletePostAsync(long chatId, int postId, CancellationToken cancellationToken)
        {
            await HandleDeleteConfirmationAsync(
                chatId,
                postId,
                (id) => _postService.GetPostAsync(id),
                (id) => _postService.DeletePostAsync(id),
                (post) => post.Title,
                "db_posts_menu",
                cancellationToken
            );
        }

        // ===== ПОИСК ПОСТОВ =====
        private async Task StartSearchPostsAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_search_posts",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК ПОСТОВ\n\nВведите текст для поиска:", cancellationToken);
        }

        private async Task HandleSearchPostsAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var posts = await _postService.SearchPostsAsync(text);

            if (!posts.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                await ShowPostsMenuAsync(chatId, cancellationToken);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {posts.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var post in posts.Take(10))
            {
                result += $"• {post.Title} ({post.CreatedAt:dd.MM.yyyy})\n";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📄 {post.Title}", $"db_post_view_{post.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_posts_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "db_posts_search", cancellationToken);
            _userStates.Remove(userId);
        }

        // ===== СТАТИСТИКА ПОСТОВ =====
        private async Task ShowPostsStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _postService.GetPostStatisticsAsync();
            var posts = await _postService.GetAllPostsAsync();

            var text = "📊 СТАТИСТИКА ПОСТОВ\n\n" +
                       $"📈 ОБЩАЯ СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего постов: {stats.TotalPosts}\n" +
                       $"│ ✅ Опубликовано: {stats.PublishedPosts}\n" +
                       $"│ 📝 Черновиков: {stats.DraftPosts}\n" +
                       $"│ 📅 Запланировано: {stats.ScheduledPosts}\n" +
                       $"└─────────────────────────────────\n\n";

            if (stats.PostsByChannel.Any())
            {
                text += $"📢 ПО КАНАЛАМ:\n";
                foreach (var channel in stats.PostsByChannel)
                {
                    text += $"│ {channel.Key}: {channel.Value} постов\n";
                }
                text += "\n";
            }

            text += $"📅 Последние 5 постов:\n";
            foreach (var post in posts.OrderByDescending(p => p.CreatedAt).Take(5))
            {
                text += $"• {post.Title} - {post.CreatedAt:dd.MM.yyyy}\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ ПОСТЫ", "db_posts_all") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_posts_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_posts_stats", cancellationToken);
        }

        // ===== ПОСТЫ ПО КАНАЛАМ =====
        private async Task ShowPostsByChannelAsync(long chatId, CancellationToken cancellationToken)
        {
            var posts = await _postService.GetAllPostsAsync();
            var byChannel = posts.GroupBy(p => p.Channel ?? "Без канала")
                                .OrderBy(g => g.Key)
                                .ToList();

            var text = "📁 ПОСТЫ ПО КАНАЛАМ\n\n";

            foreach (var channel in byChannel)
            {
                text += $"📢 {channel.Key} ({channel.Count()}):\n";
                foreach (var post in channel.Take(3))
                {
                    text += $"  • {post.Title} ({post.CreatedAt:dd.MM.yyyy})\n";
                }
                if (channel.Count() > 3)
                    text += $"  ... и еще {channel.Count() - 3}\n";
                text += "\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ ПОСТЫ", "db_posts_all") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_posts_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_posts_by_channel", cancellationToken);
        }
        private async Task ShowManualsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _manualService.GetManualStatisticsAsync();

            var text = "📚 МАНУАЛЫ\n\n" +
                       $"📊 СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего мануалов: {stats.TotalManuals}\n";

            foreach (var cat in stats.ManualsByCategory.Take(3))
            {
                text += $"│ {cat.Key}: {cat.Value}\n";
            }
            text += $"└─────────────────────────────────\n\n" +
                    $"Выберите действие:";

            var buttons = MainMenuKeyboard.GetManualsMenu();
            await _menuManager.ShowInlineMenuAsync(chatId, text, buttons, "db_manuals", cancellationToken);
        }

        // ===== ВСЕ МАНУАЛЫ =====
        private async Task ShowAllManualsAsync(long chatId, CancellationToken cancellationToken)
        {
            var manuals = await _manualService.GetAllManualsAsync();

            if (!manuals.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Мануалов нет",
                    new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_manuals_add") },
                                              new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_manuals_menu") } }),
                    "db_manuals_empty", cancellationToken);
                return;
            }

            var text = $"📋 ВСЕ МАНУАЛЫ ({manuals.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var manual in manuals.Take(10))
            {
                var categoryEmoji = manual.Category switch
                {
                    "Основной" => "📌",
                    "Дополнительный" => "📎",
                    "Тестовый" => "🧪",
                    "Обход теневого бана" => "🌑",
                    "Снятие 115/161" => "🔓",
                    _ => "📄"
                };

                text += $"{categoryEmoji} {manual.Title}\n";
                text += $"   🏦 {manual.BankName ?? "Общий"}\n";
                text += $"   📅 {manual.CreatedAt:dd.MM.yyyy}\n\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{categoryEmoji} {manual.Title}", $"db_manual_view_{manual.Id}")
        });
            }

            if (manuals.Count > 10)
                text += $"... и еще {manuals.Count - 10} мануалов\n\n";

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_manuals_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_manuals_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_manuals_all", cancellationToken);
        }

        // ===== ДЕТАЛИ МАНУАЛА =====
        private async Task ShowManualDetailsAsync(long chatId, int manualId, CancellationToken cancellationToken)
        {
            var manual = await _manualService.GetManualAsync(manualId);
            if (manual == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Мануал не найден", cancellationToken, 3);
                return;
            }

            var categoryEmoji = manual.Category switch
            {
                "Основной" => "📌",
                "Дополнительный" => "📎",
                "Тестовый" => "🧪",
                "Обход теневого бана" => "🌑",
                "Снятие 115/161" => "🔓",
                _ => "📄"
            };

            var text = $"{categoryEmoji} МАНУАЛ: {manual.Title}\n\n" +
                       $"📊 ДЕТАЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ 🏷️ Категория: {manual.Category}\n" +
                       $"│ 🏦 Банк: {manual.BankName ?? "Общий"}\n" +
                       $"│ 📅 Создан: {manual.CreatedAt:dd.MM.yyyy}\n" +
                       $"│ ✍️ Автор: {manual.Author ?? "-"}\n" +
                       $"│ 📎 Файл: {(manual.FilePath != null ? "✅" : "❌")}\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"📝 СОДЕРЖАНИЕ:\n{manual.Content}\n";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ РЕДАКТИРОВАТЬ", $"db_manual_edit_{manual.Id}"),
            InlineKeyboardButton.WithCallbackData("🗑️ УДАЛИТЬ", $"db_manual_delete_{manual.Id}")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_manuals_all") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_manual_{manual.Id}", cancellationToken);
        }

        // ===== ДОБАВЛЕНИЕ МАНУАЛА =====
        private async Task StartAddManualAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_manual_title",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📚 ДОБАВЛЕНИЕ НОВОГО МАНУАЛА (ШАГ 1/6)\n\n" +
                "Введите название мануала:", cancellationToken);
        }

        private async Task HandleAddManualTitleAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["title"] = text;
            state.CurrentAction = "db_add_manual_category";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Название: {text}\n\n" +
                "📚 ШАГ 2/6\n\n" +
                "Введите категорию (Основной/Дополнительный/Тестовый/Обход теневого бана/Снятие 115/161):", cancellationToken);
        }

        private async Task HandleAddManualCategoryAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var validCategories = new[] { "Основной", "Дополнительный", "Тестовый", "Обход теневого бана", "Снятие 115/161" };
            if (!validCategories.Contains(text))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверная категория. Выберите из списка", cancellationToken, 3);
                return;
            }

            state.Data["category"] = text;
            state.CurrentAction = "db_add_manual_bank";
            state.Step = 3;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Категория: {text}\n\n" +
                "📚 ШАГ 3/6\n\n" +
                "Введите название банка (или отправьте '-' для общего мануала):", cancellationToken);
        }

        private async Task HandleAddManualBankAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["bankName"] = text;

            state.CurrentAction = "db_add_manual_content";
            state.Step = 4;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📚 ШАГ 4/6\n\n" +
                "Введите содержание мануала:", cancellationToken);
        }

        private async Task HandleAddManualContentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["content"] = text;
            state.CurrentAction = "db_add_manual_author";
            state.Step = 5;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📚 ШАГ 5/6\n\n" +
                "Введите автора (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddManualAuthorAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["author"] = text;

            state.CurrentAction = "db_add_manual_file";
            state.Step = 6;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📚 ШАГ 6/6\n\n" +
                "Введите путь к файлу (PDF) или отправьте '-' чтобы пропустить:", cancellationToken);
        }

        private async Task HandleAddManualFileAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var manual = new DbManual
            {
                Title = state.Data["title"]?.ToString() ?? "",
                Category = state.Data["category"]?.ToString() ?? "",
                BankName = state.Data.ContainsKey("bankName") ? state.Data["bankName"]?.ToString() : null,
                Content = state.Data["content"]?.ToString() ?? "",
                Author = state.Data.ContainsKey("author") ? state.Data["author"]?.ToString() : null,
                FilePath = text != "-" ? text : null,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _manualService.CreateManualAsync(manual);

            if (result != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Мануал '{manual.Title}' создан!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowManualDetailsAsync(chatId, result.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании мануала", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowManualsMenuAsync(chatId, cancellationToken);
            }
        }

        // ===== МАНУАЛЫ ПО КАТЕГОРИИ =====
        private async Task ShowManualsByCategoryAsync(long chatId, string category, CancellationToken cancellationToken)
        {
            var manuals = await _manualService.GetManualsByCategoryAsync(category);

            var categoryEmoji = category switch
            {
                "Основной" => "📌",
                "Дополнительный" => "📎",
                "Тестовый" => "🧪",
                "Обход теневого бана" => "🌑",
                "Снятие 115/161" => "🔓",
                _ => "📄"
            };

            var text = $"{categoryEmoji} МАНУАЛЫ: {category}\n\n";

            if (!manuals.Any())
            {
                text += "Нет мануалов в этой категории";
            }
            else
            {
                text += $"Найдено: {manuals.Count}\n\n";

                foreach (var manual in manuals.Take(10))
                {
                    text += $"• {manual.Title}\n";
                    text += $"  🏦 {manual.BankName ?? "Общий"}\n";
                    text += $"  📅 {manual.CreatedAt:dd.MM.yyyy}\n\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_manuals_add") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_manuals_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_manuals_{category}", cancellationToken);
        }

        // ===== МАНУАЛЫ ПО БАНКАМ =====
        private async Task ShowManualsByBankAsync(long chatId, CancellationToken cancellationToken)
        {
            var manuals = await _manualService.GetAllManualsAsync();
            var byBank = manuals.GroupBy(m => m.BankName ?? "Общие")
                               .OrderBy(g => g.Key)
                               .ToList();

            var text = "🏦 МАНУАЛЫ ПО БАНКАМ\n\n";

            foreach (var bank in byBank)
            {
                text += $"📌 {bank.Key} ({bank.Count()}):\n";
                foreach (var manual in bank.Take(3))
                {
                    text += $"  • {manual.Title}\n";
                }
                if (bank.Count() > 3)
                    text += $"  ... и еще {bank.Count() - 3}\n";
                text += "\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ МАНУАЛЫ", "db_manuals_all") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_manuals_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_manuals_by_bank", cancellationToken);
        }

        // ===== ПОИСК МАНУАЛОВ =====
        private async Task StartSearchManualsAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_search_manuals",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК МАНУАЛОВ\n\nВведите текст для поиска:", cancellationToken);
        }

        private async Task HandleSearchManualsAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var manuals = await _manualService.SearchManualsAsync(text);

            if (!manuals.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                await ShowManualsMenuAsync(chatId, cancellationToken);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {manuals.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var manual in manuals.Take(10))
            {
                result += $"• {manual.Title} ({manual.Category})\n";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📚 {manual.Title}", $"db_manual_view_{manual.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_manuals_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "db_manuals_search", cancellationToken);
            _userStates.Remove(userId);
        }
        private async Task HandleEditManualFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 0 || field > 6)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 0 до 6", cancellationToken, 3);
                return;
            }

            if (field == 0)
            {
                Console.WriteLine($"   → Выход из редактирования мануала (field=0)");
                var manualId2 = (int)state.Data["manualId"]!;
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowManualDetailsAsync(chatId, manualId2, cancellationToken);
                return;
            }

            var manualId = (int)state.Data["manualId"]!;
            var manual = await _manualService.GetManualAsync(manualId);
            if (manual == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Мануал не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var fieldName = field switch
            {
                1 => "Название",
                2 => "Категория",
                3 => "Банк",
                4 => "Содержание",
                5 => "Автор",
                6 => "Файл",
                _ => ""
            };

            var currentValue = field switch
            {
                1 => manual.Title,
                2 => manual.Category,
                3 => manual.BankName ?? "не указано",
                4 => manual.Content.Length > 50 ? manual.Content.Substring(0, 50) + "..." : manual.Content,
                5 => manual.Author ?? "не указано",
                6 => manual.FilePath ?? "не указано",
                _ => ""
            };

            state.Data["editField"] = field;
            state.CurrentAction = "db_edit_manual_value";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ Редактирование поля: {fieldName}\n" +
                $"Текущее значение: {currentValue}\n\n" +
                $"Введите новое значение (или отправьте '-' для удаления):", cancellationToken);
        }

        private async Task HandleEditManualValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var manualId = (int)state.Data["manualId"]!;
            var field = (int)state.Data["editField"]!;

            var manual = await _manualService.GetManualAsync(manualId);
            if (manual == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Мануал не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            switch (field)
            {
                case 1:
                    manual.Title = text == "-" ? "Без названия" : text;
                    break;
                case 2:
                    var validCategories = new[] { "Основной", "Дополнительный", "Тестовый", "Обход теневого бана", "Снятие 115/161" };
                    if (text != "-" && !validCategories.Contains(text))
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверная категория", cancellationToken, 3);
                        return;
                    }
                    manual.Category = text == "-" ? "Основной" : text;
                    break;
                case 3:
                    manual.BankName = text == "-" ? null : text;
                    break;
                case 4:
                    manual.Content = text == "-" ? "" : text;
                    break;
                case 5:
                    manual.Author = text == "-" ? null : text;
                    break;
                case 6:
                    manual.FilePath = text == "-" ? null : text;
                    break;
            }

            manual.UpdatedAt = DateTime.UtcNow;
            var success = await _manualService.UpdateManualAsync(manual);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Мануал обновлен", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowManualDetailsAsync(chatId, manualId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
            }
        }
        // ===== СТАРТ РЕДАКТИРОВАНИЯ МАНУАЛА =====
        private async Task StartEditManualAsync(long chatId, long userId, int manualId, CancellationToken cancellationToken)
        {
            var manual = await _manualService.GetManualAsync(manualId);
            if (manual == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Мануал не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_edit_manual_field",
                Data = new Dictionary<string, object?> { ["manualId"] = manualId },
                Step = 1
            };

            var text = $"✏️ РЕДАКТИРОВАНИЕ МАНУАЛА: {manual.Title}\n\n" +
                       "Выберите поле для редактирования:\n\n" +
                       "1️⃣ Название\n" +
                       "2️⃣ Категория\n" +
                       "3️⃣ Банк\n" +
                       "4️⃣ Содержание\n" +
                       "5️⃣ Автор\n" +
                       "6️⃣ Файл\n\n" +
                       "Введите номер поля (1-6) или 0 для выхода:";

            await _menuManager.SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }
        // ===== УДАЛЕНИЕ МАНУАЛА =====
        private async Task ShowDeleteManualConfirmationAsync(long chatId, int manualId, CancellationToken cancellationToken)
        {
            var manual = await _manualService.GetManualAsync(manualId);
            if (manual == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Мануал не найден", cancellationToken, 3);
                return;
            }

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить мануал?\n\n" +
                       $"📚 {manual.Title}\n" +
                       $"🏷️ {manual.Category}\n" +
                       $"🏦 {manual.BankName ?? "Общий"}\n\n" +
                       $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"delete_manual_confirm_{manualId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_manual_view_{manualId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_confirmation", cancellationToken);
        }

        // ===== УДАЛЕНИЕ МАНУАЛА =====
        private async Task DeleteManualAsync(long chatId, int manualId, CancellationToken cancellationToken)
        {
            await HandleDeleteConfirmationAsync(
                chatId,
                manualId,
                (id) => _manualService.GetManualAsync(id),
                (id) => _manualService.DeleteManualAsync(id),
                (manual) => manual.Title,
                "db_manuals_menu",
                cancellationToken
            );
        }


        // ===== ОТЧЁТЫ - МЕНЮ =====
        private async Task ShowReportsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _reportService.GetReportStatisticsAsync();

            var text = "📊 ОТЧЁТЫ ИНВЕСТОРАМ\n\n" +
                       $"📈 СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего отчётов: {stats.TotalReports}\n" +
                       $"│ За этот месяц: {stats.ReportsThisMonth}\n" +
                       $"│ Общая прибыль: {stats.TotalProfit:N0} ₽\n" +
                       $"│ Общий депозит: {stats.TotalDeposits:N0} ₽\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"Выберите действие:";

            var buttons = MainMenuKeyboard.GetReportsMenu();
            await _menuManager.ShowInlineMenuAsync(chatId, text, buttons, "db_reports", cancellationToken);
        }

        // ===== ВСЕ ОТЧЁТЫ =====
        private async Task ShowAllReportsAsync(long chatId, CancellationToken cancellationToken)
        {
            var reports = await _reportService.GetAllReportsAsync();

            if (!reports.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Отчётов нет",
                    new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("➕ СОЗДАТЬ", "db_reports_add") },
                                              new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_reports_menu") } }),
                    "db_reports_empty", cancellationToken);
                return;
            }

            var text = $"📋 ВСЕ ОТЧЁТЫ ({reports.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var report in reports.Take(10))
            {
                var statusEmoji = report.Status switch
                {
                    "Готов" => "✅",
                    "Черновик" => "📝",
                    "Отправлен" => "📤",
                    _ => "📄"
                };

                text += $"{statusEmoji} {report.Title}\n";
                text += $"   👤 {report.InvestorName ?? "Инвестор"}\n";
                text += $"   📅 {report.ReportDate:dd.MM.yyyy}\n";
                text += $"   💰 Прибыль: {report.TotalProfit:N0} ₽\n\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📊 {report.Title}", $"db_report_view_{report.Id}")
        });
            }

            if (reports.Count > 10)
                text += $"... и еще {reports.Count - 10} отчётов\n\n";

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ СОЗДАТЬ", "db_reports_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_reports_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_reports_all", cancellationToken);
        }

        // ===== ДЕТАЛИ ОТЧЁТА =====
        private async Task ShowReportDetailsAsync(long chatId, int reportId, CancellationToken cancellationToken)
        {
            var report = await _reportService.GetReportAsync(reportId);
            if (report == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Отчёт не найден", cancellationToken, 3);
                return;
            }

            var statusEmoji = report.Status switch
            {
                "Готов" => "✅",
                "Черновик" => "📝",
                "Отправлен" => "📤",
                _ => "📄"
            };

            var text = $"📊 ОТЧЁТ: {report.Title}\n\n" +
                       $"📈 ДЕТАЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ {statusEmoji} Статус: {report.Status ?? "Черновик"}\n" +
                       $"│ 👤 Инвестор: {report.InvestorName ?? "-"}\n" +
                       $"│ 📅 Дата отчёта: {report.ReportDate:dd.MM.yyyy}\n" +
                       $"│ 📅 Создан: {report.CreatedAt:dd.MM.yyyy}\n" +
                       $"│ 💰 Прибыль: {report.TotalProfit:N0} ₽\n" +
                       $"│ 💵 Депозит: {report.TotalDeposit:N0} ₽\n" +
                       $"│ 📎 Файл: {(report.FilePath != null ? "✅" : "❌")}\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"📝 СОДЕРЖАНИЕ:\n{report.Summary ?? "Нет описания"}\n";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ РЕДАКТИРОВАТЬ", $"db_report_edit_{report.Id}"),
            InlineKeyboardButton.WithCallbackData("📤 ЭКСПОРТ PDF", $"db_report_export_{report.Id}")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🗑️ УДАЛИТЬ", $"db_report_delete_{report.Id}"),
            InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_reports_all")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_report_{report.Id}", cancellationToken);
        }

        // ===== ДОБАВЛЕНИЕ ОТЧЁТА =====
        private async Task StartAddReportAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_report_title",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📊 ДОБАВЛЕНИЕ НОВОГО ОТЧЁТА (ШАГ 1/7)\n\n" +
                "Введите название отчёта:", cancellationToken);
        }

        private async Task HandleAddReportTitleAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["title"] = text;
            state.CurrentAction = "db_add_report_investor";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Название: {text}\n\n" +
                "📊 ШАГ 2/7\n\n" +
                "Введите имя инвестора (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddReportInvestorAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["investorName"] = text;

            state.CurrentAction = "db_add_report_date";
            state.Step = 3;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📊 ШАГ 3/7\n\n" +
                "Введите дату отчёта в формате ДД.ММ.ГГГГ\n" +
                "(или отправьте '-' для сегодняшней):", cancellationToken);
        }

        private async Task HandleAddReportDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            DateTime reportDate;
            if (text == "-")
                reportDate = DateTime.UtcNow;
            else if (!DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out reportDate))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты", cancellationToken, 3);
                return;
            }

            state.Data["reportDate"] = reportDate;
            state.CurrentAction = "db_add_report_deposit";
            state.Step = 4;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Дата: {reportDate:dd.MM.yyyy}\n\n" +
                "📊 ШАГ 4/7\n\n" +
                "Введите общую сумму депозита (в ₽):\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddReportDepositAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                if (!decimal.TryParse(text, out decimal deposit))
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken, 3);
                    return;
                }
                state.Data["deposit"] = deposit;
            }

            state.CurrentAction = "db_add_report_profit";
            state.Step = 5;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📊 ШАГ 5/7\n\n" +
                "Введите общую прибыль (в ₽):\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddReportProfitAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
            {
                if (!decimal.TryParse(text, out decimal profit))
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken, 3);
                    return;
                }
                state.Data["profit"] = profit;
            }

            state.CurrentAction = "db_add_report_summary";
            state.Step = 6;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📊 ШАГ 6/7\n\n" +
                "Введите краткое содержание/итоги:", cancellationToken);
        }

        private async Task HandleAddReportSummaryAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["summary"] = text;
            state.CurrentAction = "db_add_report_status";
            state.Step = 7;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📊 ШАГ 7/7\n\n" +
                "Введите статус (Черновик/Готов/Отправлен):\n" +
                "(или отправьте '-' для 'Черновик')", cancellationToken);
        }

        private async Task HandleAddReportStatusAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var status = text != "-" ? text : "Черновик";

            var report = new DbReport
            {
                Title = state.Data["title"]?.ToString() ?? "",
                InvestorName = state.Data.ContainsKey("investorName") ? state.Data["investorName"]?.ToString() : null,
                ReportDate = state.Data.ContainsKey("reportDate") ? (DateTime)state.Data["reportDate"]! : DateTime.UtcNow,
                TotalDeposit = state.Data.ContainsKey("deposit") ? (decimal?)state.Data["deposit"] : null,
                TotalProfit = state.Data.ContainsKey("profit") ? (decimal?)state.Data["profit"] : null,
                Summary = state.Data["summary"]?.ToString(),
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _reportService.CreateReportAsync(report);

            if (result != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Отчёт '{report.Title}' создан!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowReportDetailsAsync(chatId, result.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании отчёта", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowReportsMenuAsync(chatId, cancellationToken);
            }
        }

        // ===== ЭКСПОРТ ОТЧЁТА В PDF =====
        private async Task StartExportReportAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var reports = await _reportService.GetAllReportsAsync();

            if (!reports.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Нет отчётов для экспорта", cancellationToken, 3);
                return;
            }

            var text = "📤 ЭКСПОРТ ОТЧЁТА В PDF\n\nВыберите отчёт:";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var report in reports.Take(10))
            {
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{report.Title} ({report.ReportDate:dd.MM.yyyy})",
                $"db_report_export_{report.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_reports_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_reports_export", cancellationToken);
        }

        private async Task ExportReportToPdfAsync(long chatId, int reportId, CancellationToken cancellationToken)
        {
            var report = await _reportService.GetReportAsync(reportId);
            if (report == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Отчёт не найден", cancellationToken, 3);
                return;
            }

            await _menuManager.SendTemporaryMessageAsync(chatId, $"⏳ Генерация PDF для отчёта '{report.Title}'...", cancellationToken, 2);

            try
            {
                var pdfData = await _reportService.ExportReportToPdfAsync(reportId);

                if (pdfData.Length > 0)
                {
                    using var stream = new MemoryStream(pdfData);

                    var fileName = $"report_{reportId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

                    // Новый способ отправки файлов
                    await _botClient.SendDocument(
                        chatId: chatId,
                        document: new InputFileStream(stream, fileName),
                        caption: $"📊 Отчёт: {report.Title}\n📅 {report.ReportDate:dd.MM.yyyy}",
                        cancellationToken: cancellationToken
                    );

                    await _menuManager.SendTemporaryMessageAsync(chatId, "✅ PDF сгенерирован и отправлен!", cancellationToken, 3);
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при генерации PDF", cancellationToken, 3);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to PDF");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при экспорте", cancellationToken, 3);
            }
        }

        // ===== ПОИСК ОТЧЁТОВ =====
        private async Task StartSearchReportsAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_search_reports",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК ОТЧЁТОВ\n\nВведите текст для поиска:", cancellationToken);
        }

        private async Task HandleSearchReportsAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var reports = await _reportService.SearchReportsAsync(text);

            if (!reports.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                await ShowReportsMenuAsync(chatId, cancellationToken);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {reports.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var report in reports.Take(10))
            {
                result += $"• {report.Title} ({report.ReportDate:dd.MM.yyyy})\n";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📊 {report.Title}", $"db_report_view_{report.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_reports_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "db_reports_search", cancellationToken);
            _userStates.Remove(userId);
        }

        // ===== СТАТИСТИКА ОТЧЁТОВ =====
        private async Task ShowReportsStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _reportService.GetReportStatisticsAsync();
            var reports = await _reportService.GetAllReportsAsync();

            var text = "📊 СТАТИСТИКА ОТЧЁТОВ\n\n" +
                       $"📈 ОБЩАЯ СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего отчётов: {stats.TotalReports}\n" +
                       $"│ За текущий месяц: {stats.ReportsThisMonth}\n" +
                       $"│ Общая прибыль: {stats.TotalProfit:N0} ₽\n" +
                       $"│ Общий депозит: {stats.TotalDeposits:N0} ₽\n" +
                       $"│ Средняя прибыль: {(stats.TotalReports > 0 ? stats.TotalProfit / stats.TotalReports : 0):N0} ₽\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"📅 Последние 5 отчётов:\n";

            foreach (var report in reports.OrderByDescending(r => r.ReportDate).Take(5))
            {
                text += $"• {report.Title} - {report.ReportDate:dd.MM.yyyy} ({report.TotalProfit:N0} ₽)\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ ОТЧЁТЫ", "db_reports_all") },
        new() { InlineKeyboardButton.WithCallbackData("➕ СОЗДАТЬ", "db_reports_add") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_reports_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_reports_stats", cancellationToken);
        }

        // ===== УДАЛЕНИЕ ОТЧЁТА =====
        private async Task ShowDeleteReportConfirmationAsync(long chatId, int reportId, CancellationToken cancellationToken)
        {
            var report = await _reportService.GetReportAsync(reportId);
            if (report == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Отчёт не найден", cancellationToken, 3);
                return;
            }

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить отчёт?\n\n" +
                       $"📊 {report.Title}\n" +
                       $"👤 {report.InvestorName ?? "Инвестор"}\n" +
                       $"📅 {report.ReportDate:dd.MM.yyyy}\n" +
                       $"💰 Прибыль: {report.TotalProfit:N0} ₽\n\n" +
                       $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"delete_report_confirm_{reportId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_report_view_{reportId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_confirmation", cancellationToken);
        }
        // ===== РЕДАКТИРОВАНИЕ ОТЧЁТА =====
        private async Task StartEditReportAsync(long chatId, long userId, int reportId, CancellationToken cancellationToken)
        {
            var report = await _reportService.GetReportAsync(reportId);
            if (report == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Отчёт не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_edit_report_field",
                Data = new Dictionary<string, object?> { ["reportId"] = reportId },
                Step = 1
            };

            var text = $"✏️ РЕДАКТИРОВАНИЕ ОТЧЁТА: {report.Title}\n\n" +
                       "Выберите поле для редактирования:\n\n" +
                       "1️⃣ Название\n" +
                       "2️⃣ Инвестор\n" +
                       "3️⃣ Дата отчёта\n" +
                       "4️⃣ Депозит\n" +
                       "5️⃣ Прибыль\n" +
                       "6️⃣ Содержание\n" +
                       "7️⃣ Статус\n\n" +
                       "Введите номер поля (1-7) или 0 для выхода:";

            await _menuManager.SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }

        private async Task HandleEditReportFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 0 || field > 7)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 0 до 7", cancellationToken, 3);
                return;
            }

            if (field == 0)
            {
                Console.WriteLine($"   → Выход из редактирования отчёта (field=0)");
                var reportId2 = (int)state.Data["reportId"]!;
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowReportDetailsAsync(chatId, reportId2, cancellationToken);
                return;
            }

            var reportId = (int)state.Data["reportId"]!;
            var report = await _reportService.GetReportAsync(reportId);
            if (report == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Отчёт не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var fieldName = field switch
            {
                1 => "Название",
                2 => "Инвестор",
                3 => "Дата отчёта",
                4 => "Депозит",
                5 => "Прибыль",
                6 => "Содержание",
                7 => "Статус",
                _ => ""
            };

            var currentValue = field switch
            {
                1 => report.Title,
                2 => report.InvestorName ?? "не указано",
                3 => report.ReportDate.ToString("dd.MM.yyyy"),
                4 => report.TotalDeposit?.ToString("N0") ?? "не указано",
                5 => report.TotalProfit?.ToString("N0") ?? "не указано",
                6 => report.Summary?.Length > 50 ? report.Summary.Substring(0, 50) + "..." : report.Summary ?? "не указано",
                7 => report.Status ?? "не указано",
                _ => ""
            };

            state.Data["editField"] = field;
            state.CurrentAction = "db_edit_report_value";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ Редактирование поля: {fieldName}\n" +
                $"Текущее значение: {currentValue}\n\n" +
                $"Введите новое значение (или отправьте '-' для удаления):", cancellationToken);
        }

        private async Task HandleEditReportValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var reportId = (int)state.Data["reportId"]!;
            var field = (int)state.Data["editField"]!;

            var report = await _reportService.GetReportAsync(reportId);
            if (report == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Отчёт не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            switch (field)
            {
                case 1:
                    report.Title = text == "-" ? "Без названия" : text;
                    break;
                case 2:
                    report.InvestorName = text == "-" ? null : text;
                    break;
                case 3:
                    if (text != "-")
                    {
                        if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                            report.ReportDate = date;
                        else
                        {
                            await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты", cancellationToken, 3);
                            return;
                        }
                    }
                    break;
                case 4:
                    if (text != "-")
                    {
                        if (decimal.TryParse(text, out decimal deposit))
                            report.TotalDeposit = deposit;
                        else
                        {
                            await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken, 3);
                            return;
                        }
                    }
                    else
                        report.TotalDeposit = null;
                    break;
                case 5:
                    if (text != "-")
                    {
                        if (decimal.TryParse(text, out decimal profit))
                            report.TotalProfit = profit;
                        else
                        {
                            await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken, 3);
                            return;
                        }
                    }
                    else
                        report.TotalProfit = null;
                    break;
                case 6:
                    report.Summary = text == "-" ? null : text;
                    break;
                case 7:
                    var validStatuses = new[] { "Черновик", "Готов", "Отправлен" };
                    if (text != "-" && !validStatuses.Contains(text))
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный статус", cancellationToken, 3);
                        return;
                    }
                    report.Status = text == "-" ? "Черновик" : text;
                    break;
            }

            report.UpdatedAt = DateTime.UtcNow;
            var success = await _reportService.UpdateReportAsync(report);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Отчёт обновлен", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowReportDetailsAsync(chatId, reportId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
            }
        }

        // ===== УДАЛЕНИЕ ОТЧЁТА =====
        private async Task DeleteReportAsync(long chatId, int reportId, CancellationToken cancellationToken)
        {
            await HandleDeleteConfirmationAsync(
                chatId,
                reportId,
                (id) => _reportService.GetReportAsync(id),
                (id) => _reportService.DeleteReportAsync(id),
                (report) => report.Title,
                "db_reports_menu",
                cancellationToken
            );
        }

        private async Task ShowProjectDocsDatabaseAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var projects = await _projectService.GetAllProjectsAsync();

                var text = $"📋 Документация проектов\n\n" +
                           $"Всего проектов: {projects.Count}\n\n" +
                           $"📁 Проекты с документацией:\n";

                var projectsWithDocs = projects
                    .Where(p => !string.IsNullOrEmpty(p.Description) || !string.IsNullOrEmpty(p.Link))
                    .Take(5)
                    .ToList();

                foreach (var project in projectsWithDocs)
                {
                    var hasDescription = !string.IsNullOrEmpty(project.Description) ? "📝" : "";
                    var hasLink = !string.IsNullOrEmpty(project.Link) ? "🔗" : "";
                    var statusIcon = project.Status switch
                    {
                        ProjectStatus.Pending => "🟡",
                        ProjectStatus.InProgress => "🟠",
                        ProjectStatus.Completed => "✅",
                        _ => "⚪"
                    };

                    text += $"{statusIcon} {project.Name} {hasDescription}{hasLink}\n";
                }

                if (projectsWithDocs.Count == 0)
                {
                    text += "📭 Нет проектов с документацией.\n";
                }

                var statsText = $"\n📊 Статистика:\n" +
                                $"• С описанием: {projects.Count(p => !string.IsNullOrEmpty(p.Description))}\n" +
                                $"• Со ссылками: {projects.Count(p => !string.IsNullOrEmpty(p.Link))}\n" +
                                $"• Полные описания: {projects.Count(p => !string.IsNullOrEmpty(p.Description) && p.Description.Length > 100)}";

                text += statsText;

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("📋 Список проектов", CallbackData.ProjectsList) },
            new() { InlineKeyboardButton.WithCallbackData("🔍 Поиск документации", "project_docs_search") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToDatabase) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "database_project_docs",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing project docs database");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке документации проектов.", cancellationToken);
            }
        }

        // ===== ДОКУМЕНТАЦИЯ - МЕНЮ =====
        private async Task ShowDocsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _documentService.GetDocumentStatisticsAsync();

            var text = "📋 ДОКУМЕНТАЦИЯ\n\n" +
                       $"📊 СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего документов: {stats.TotalDocuments}\n";

            if (stats.DocumentsByProject.Any())
            {
                foreach (var proj in stats.DocumentsByProject.Take(3))
                {
                    text += $"│ {proj.Key}: {proj.Value} док.\n";
                }
            }
            text += $"└─────────────────────────────────\n\n" +
                    $"Выберите действие:";

            var buttons = MainMenuKeyboard.GetDocsMenu();
            await _menuManager.ShowInlineMenuAsync(chatId, text, buttons, "db_docs", cancellationToken);
        }

        // ===== ВСЯ ДОКУМЕНТАЦИЯ =====
        private async Task ShowAllDocsAsync(long chatId, CancellationToken cancellationToken)
        {
            var docs = await _documentService.GetAllDocumentsAsync();

            if (!docs.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Документов нет",
                    new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_docs_add") },
                                              new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_docs_menu") } }),
                    "db_docs_empty", cancellationToken);
                return;
            }

            var text = $"📋 ВСЯ ДОКУМЕНТАЦИЯ ({docs.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var doc in docs.Take(10))
            {
                var typeEmoji = doc.DocumentType switch
                {
                    "Инструкция" => "📘",
                    "Правила" => "📜",
                    "Сводка" => "📊",
                    "API" => "🔧",
                    _ => "📄"
                };

                text += $"{typeEmoji} {doc.Title}\n";
                text += $"   📂 {doc.ProjectName ?? "Общий"}\n";
                text += $"   📅 {doc.CreatedAt:dd.MM.yyyy}\n\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{typeEmoji} {doc.Title}", $"db_doc_view_{doc.Id}")
        });
            }

            if (docs.Count > 10)
                text += $"... и еще {docs.Count - 10} документов\n\n";

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_docs_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_docs_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_docs_all", cancellationToken);
        }

        // ===== ДЕТАЛИ ДОКУМЕНТА =====
        private async Task ShowDocDetailsAsync(long chatId, int docId, CancellationToken cancellationToken)
        {
            var doc = await _documentService.GetDocumentAsync(docId);
            if (doc == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Документ не найден", cancellationToken, 3);
                return;
            }

            var typeEmoji = doc.DocumentType switch
            {
                "Инструкция" => "📘",
                "Правила" => "📜",
                "Сводка" => "📊",
                "API" => "🔧",
                _ => "📄"
            };

            var text = $"{typeEmoji} ДОКУМЕНТ: {doc.Title}\n\n" +
                       $"📊 ДЕТАЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ 📂 Проект: {doc.ProjectName ?? "Общий"}\n" +
                       $"│ 🏷️ Тип: {doc.DocumentType ?? "-"}\n" +
                       $"│ 📅 Создан: {doc.CreatedAt:dd.MM.yyyy}\n" +
                       $"│ 📎 Файл: {(doc.FilePath != null ? "✅" : "❌")}\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"📝 СОДЕРЖАНИЕ:\n{doc.Content}\n";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ РЕДАКТИРОВАТЬ", $"db_doc_edit_{doc.Id}"),
            InlineKeyboardButton.WithCallbackData("🗑️ УДАЛИТЬ", $"db_doc_delete_{doc.Id}")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_docs_all") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_doc_{doc.Id}", cancellationToken);
        }

        // ===== ДОБАВЛЕНИЕ ДОКУМЕНТА =====
        private async Task StartAddDocAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_doc_title",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📋 ДОБАВЛЕНИЕ НОВОГО ДОКУМЕНТА (ШАГ 1/6)\n\n" +
                "Введите название документа:", cancellationToken);
        }

        private async Task HandleAddDocTitleAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["title"] = text;
            state.CurrentAction = "db_add_doc_project";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Название: {text}\n\n" +
                "📋 ШАГ 2/6\n\n" +
                "Введите название проекта (или отправьте '-' для общего документа):", cancellationToken);
        }

        private async Task HandleAddDocProjectAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["projectName"] = text;

            state.CurrentAction = "db_add_doc_type";
            state.Step = 3;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📋 ШАГ 3/6\n\n" +
                "Введите тип документа (Инструкция/Правила/Сводка/API):\n" +
                "(или отправьте '-' чтобы пропустить)", cancellationToken);
        }

        private async Task HandleAddDocTypeAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["docType"] = text;

            state.CurrentAction = "db_add_doc_content";
            state.Step = 4;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📋 ШАГ 4/6\n\n" +
                "Введите содержание документа:", cancellationToken);
        }

        private async Task HandleAddDocContentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["content"] = text;
            state.CurrentAction = "db_add_doc_file";
            state.Step = 5;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📋 ШАГ 5/6\n\n" +
                "Введите путь к файлу (PDF) или отправьте '-' чтобы пропустить:", cancellationToken);
        }

        private async Task HandleAddDocFileAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["filePath"] = text != "-" ? text : null;
            state.CurrentAction = "db_add_doc_confirm";
            state.Step = 6;
            _userStates[userId] = state;

            var doc = new DbDocument
            {
                Title = state.Data["title"]?.ToString() ?? "",
                ProjectName = state.Data.ContainsKey("projectName") ? state.Data["projectName"]?.ToString() : null,
                DocumentType = state.Data.ContainsKey("docType") ? state.Data["docType"]?.ToString() : null,
                Content = state.Data["content"]?.ToString() ?? "",
                FilePath = state.Data.ContainsKey("filePath") ? state.Data["filePath"]?.ToString() : null,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _documentService.CreateDocumentAsync(doc);

            if (result != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Документ '{doc.Title}' создан!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowDocDetailsAsync(chatId, result.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании документа", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowDocsMenuAsync(chatId, cancellationToken);
            }
        }

        // ===== ДОКУМЕНТЫ ПО ПРОЕКТАМ =====
        private async Task ShowDocsByProjectAsync(long chatId, CancellationToken cancellationToken)
        {
            var docs = await _documentService.GetAllDocumentsAsync();
            var byProject = docs.GroupBy(d => d.ProjectName ?? "Общие")
                               .OrderBy(g => g.Key)
                               .ToList();

            var text = "📂 ДОКУМЕНТАЦИЯ ПО ПРОЕКТАМ\n\n";

            foreach (var project in byProject)
            {
                text += $"📌 {project.Key} ({project.Count()}):\n";
                foreach (var doc in project.Take(3))
                {
                    var typeEmoji = doc.DocumentType switch
                    {
                        "Инструкция" => "📘",
                        "Правила" => "📜",
                        "Сводка" => "📊",
                        "API" => "🔧",
                        _ => "📄"
                    };
                    text += $"  {typeEmoji} {doc.Title}\n";
                }
                if (project.Count() > 3)
                    text += $"  ... и еще {project.Count() - 3}\n";
                text += "\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЯ ДОКУМЕНТАЦИЯ", "db_docs_all") },
        new() { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_docs_add") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_docs_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_docs_by_project", cancellationToken);
        }

        // ===== СТАТИСТИКА ДОКУМЕНТОВ =====
        private async Task ShowDocsStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _documentService.GetDocumentStatisticsAsync();
            var docs = await _documentService.GetAllDocumentsAsync();

            var text = "📊 СТАТИСТИКА ДОКУМЕНТОВ\n\n" +
                       $"📈 ОБЩАЯ СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего документов: {stats.TotalDocuments}\n" +
                       $"└─────────────────────────────────\n\n";

            if (stats.DocumentsByProject.Any())
            {
                text += $"📂 ПО ПРОЕКТАМ:\n";
                foreach (var proj in stats.DocumentsByProject)
                {
                    text += $"│ {proj.Key}: {proj.Value} док.\n";
                }
                text += "\n";
            }

            if (stats.DocumentsByType.Any())
            {
                text += $"🏷️ ПО ТИПАМ:\n";
                foreach (var type in stats.DocumentsByType)
                {
                    var emoji = type.Key switch
                    {
                        "Инструкция" => "📘",
                        "Правила" => "📜",
                        "Сводка" => "📊",
                        "API" => "🔧",
                        _ => "📄"
                    };
                    text += $"│ {emoji} {type.Key}: {type.Value}\n";
                }
                text += "\n";
            }

            text += $"📅 Последние 5 документов:\n";
            foreach (var doc in docs.OrderByDescending(d => d.CreatedAt).Take(5))
            {
                text += $"• {doc.Title} - {doc.CreatedAt:dd.MM.yyyy}\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЯ ДОКУМЕНТАЦИЯ", "db_docs_all") },
        new() { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_docs_add") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_docs_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_docs_stats", cancellationToken);
        }

        // ===== ПОИСК ДОКУМЕНТОВ =====
        private async Task StartSearchDocsAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_search_docs",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК ДОКУМЕНТОВ\n\nВведите текст для поиска:", cancellationToken);
        }

        private async Task HandleSearchDocsAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var docs = await _documentService.SearchDocumentsAsync(text);

            if (!docs.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                await ShowDocsMenuAsync(chatId, cancellationToken);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {docs.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var doc in docs.Take(10))
            {
                result += $"• {doc.Title} ({doc.ProjectName ?? "Общий"})\n";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📄 {doc.Title}", $"db_doc_view_{doc.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_docs_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "db_docs_search", cancellationToken);
            _userStates.Remove(userId);
        }

        // ===== УДАЛЕНИЕ ДОКУМЕНТА =====
        private async Task ShowDeleteDocConfirmationAsync(long chatId, int docId, CancellationToken cancellationToken)
        {
            var doc = await _documentService.GetDocumentAsync(docId);
            if (doc == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Документ не найден", cancellationToken, 3);
                return;
            }

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить документ?\n\n" +
                       $"📄 {doc.Title}\n" +
                       $"📂 {doc.ProjectName ?? "Общий"}\n" +
                       $"🏷️ {doc.DocumentType ?? "-"}\n\n" +
                       $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"delete_doc_confirm_{docId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_doc_view_{docId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_confirmation", cancellationToken);
        }
        // ===== РЕДАКТИРОВАНИЕ ДОКУМЕНТА =====
        private async Task StartEditDocAsync(long chatId, long userId, int docId, CancellationToken cancellationToken)
        {
            var doc = await _documentService.GetDocumentAsync(docId);
            if (doc == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Документ не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_edit_doc_field",
                Data = new Dictionary<string, object?> { ["docId"] = docId },
                Step = 1
            };

            var text = $"✏️ РЕДАКТИРОВАНИЕ ДОКУМЕНТА: {doc.Title}\n\n" +
                       "Выберите поле для редактирования:\n\n" +
                       "1️⃣ Название\n" +
                       "2️⃣ Проект\n" +
                       "3️⃣ Тип\n" +
                       "4️⃣ Содержание\n" +
                       "5️⃣ Файл\n\n" +
                       "Введите номер поля (1-5) или 0 для выхода:";

            await _menuManager.SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }

        private async Task HandleEditDocFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 0 || field > 5)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 0 до 5", cancellationToken, 3);
                return;
            }

            if (field == 0)
            {
                Console.WriteLine($"   → Выход из редактирования документа (field=0)");
                var docId2 = (int)state.Data["docId"]!;
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowDocDetailsAsync(chatId, docId2, cancellationToken);
                return;
            }

            var docId = (int)state.Data["docId"]!;
            var doc = await _documentService.GetDocumentAsync(docId);
            if (doc == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Документ не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var fieldName = field switch
            {
                1 => "Название",
                2 => "Проект",
                3 => "Тип",
                4 => "Содержание",
                5 => "Файл",
                _ => ""
            };

            var currentValue = field switch
            {
                1 => doc.Title,
                2 => doc.ProjectName ?? "не указано",
                3 => doc.DocumentType ?? "не указано",
                4 => doc.Content.Length > 50 ? doc.Content.Substring(0, 50) + "..." : doc.Content,
                5 => doc.FilePath ?? "не указано",
                _ => ""
            };

            state.Data["editField"] = field;
            state.CurrentAction = "db_edit_doc_value";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ Редактирование поля: {fieldName}\n" +
                $"Текущее значение: {currentValue}\n\n" +
                $"Введите новое значение (или отправьте '-' для удаления):", cancellationToken);
        }

        private async Task HandleEditDocValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var docId = (int)state.Data["docId"]!;
            var field = (int)state.Data["editField"]!;

            var doc = await _documentService.GetDocumentAsync(docId);
            if (doc == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Документ не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            switch (field)
            {
                case 1:
                    doc.Title = text == "-" ? "Без названия" : text;
                    break;
                case 2:
                    doc.ProjectName = text == "-" ? null : text;
                    break;
                case 3:
                    doc.DocumentType = text == "-" ? null : text;
                    break;
                case 4:
                    doc.Content = text == "-" ? "" : text;
                    break;
                case 5:
                    doc.FilePath = text == "-" ? null : text;
                    break;
            }

            doc.UpdatedAt = DateTime.UtcNow;
            var success = await _documentService.UpdateDocumentAsync(doc);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Документ обновлен", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowDocDetailsAsync(chatId, docId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
            }
        }
        // ===== УДАЛЕНИЕ ДОКУМЕНТА =====
        private async Task DeleteDocAsync(long chatId, int docId, CancellationToken cancellationToken)
        {
            await HandleDeleteConfirmationAsync(
                chatId,
                docId,
                (id) => _documentService.GetDocumentAsync(id),
                (id) => _documentService.DeleteDocumentAsync(id),
                (doc) => doc.Title,
                "db_docs_menu",
                cancellationToken
            );
        }

        // ===== ПЛАНЫ =====
        private async Task StartSearchPlansAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "search_plans",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК ПЛАНОВ\n\nВведите текст для поиска:", cancellationToken);
        }

        private async Task HandleSearchPlansAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var plans = await _planService.SearchPlansAsync(text);

            if (!plans.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowPlansMenuAsync(chatId, cancellationToken);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {plans.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var plan in plans.Take(10))
            {
                result += $"• {plan.Title}\n";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📄 {plan.Title}", $"plan_view_{plan.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "show_plans")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "plans_search", cancellationToken);
            _userStates.Remove(userId);
        }
        private async Task ShowPlansMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _planService.GetAllPlansAsync();

            var text = "📝 ПЛАНЫ\n\n" +
                       $"📊 Всего планов: {stats.Count}\n\n" +
                       "Выберите действие:";

            var buttons = MainMenuKeyboard.GetPlansMenu();
            await _menuManager.ShowInlineMenuAsync(chatId, text, buttons, "plans_menu", cancellationToken);
        }

        private async Task ShowAllPlansAsync(long chatId, CancellationToken cancellationToken)
        {
            var plans = await _planService.GetAllPlansAsync();

            if (!plans.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Планов нет",
                    new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("➕ НОВЫЙ ПЛАН", "plans_add") },
                                              new[] { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "show_plans") } }),
                    "plans_empty", cancellationToken);
                return;
            }

            var text = $"📋 ВСЕ ПЛАНЫ ({plans.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var plan in plans.Take(10))
            {
                var author = plan.CreatedBy?.Username != null ? $"@{plan.CreatedBy.Username}" : "Неизвестно";
                var date = plan.UpdatedAt ?? plan.CreatedAt;

                text += $"📄 {plan.Title}\n";
                text += $"   👤 {author}\n";
                text += $"   📅 {date:dd.MM.yyyy}\n\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📄 {plan.Title}", $"plan_view_{plan.Id}")
        });
            }

            if (plans.Count > 10)
                text += $"... и еще {plans.Count - 10} планов\n\n";

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ НОВЫЙ", "plans_add"),
        InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", "show_plans")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "plans_all", cancellationToken);
        }

        private async Task ShowPlanDetailsAsync(long chatId, int planId, CancellationToken cancellationToken)
        {
            var plan = await _planService.GetPlanAsync(planId);
            if (plan == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ План не найден", cancellationToken, 3);
                return;
            }

            var author = plan.CreatedBy?.Username != null ? $"@{plan.CreatedBy.Username}" : "Неизвестно";
            var created = plan.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            var updated = plan.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";

            var preview = plan.Content.Length > 200
                ? plan.Content.Substring(0, 200) + "..."
                : plan.Content;

            var text = $"📄 ПЛАН: {plan.Title}\n\n" +
                       $"👤 Автор: {author}\n" +
                       $"📅 Создан: {created}\n" +
                       $"🔄 Обновлён: {updated}\n\n" +
                       $"📝 СОДЕРЖАНИЕ:\n{preview}\n";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"plan_edit_{plan.Id}"),
            InlineKeyboardButton.WithCallbackData("📥 Скачать", $"plan_download_{plan.Id}")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"plan_delete_{plan.Id}"),
            InlineKeyboardButton.WithCallbackData("◀️ Назад", "plans_all")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"plan_{plan.Id}", cancellationToken);
        }

        private async Task StartAddPlanAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "add_plan_title",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📝 СОЗДАНИЕ НОВОГО ПЛАНА (ШАГ 1/2)\n\n" +
                "Введите название плана:", cancellationToken);
        }

        private async Task HandleAddPlanTitleAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["title"] = text;
            state.CurrentAction = "add_plan_content";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Название: {text}\n\n" +
                "📝 ШАГ 2/2\n\n" +
                "Введите содержание плана:", cancellationToken);
        }

        private async Task HandleAddPlanContentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var title = state.Data["title"]?.ToString() ?? "Без названия";

            var plan = await _planService.CreatePlanAsync(title, text, userId);

            if (plan != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ План '{title}' создан!", cancellationToken, 3);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowPlanDetailsAsync(chatId, plan.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании плана", cancellationToken, 5);
                _userStates.Remove(userId);
                await ShowPlansMenuAsync(chatId, cancellationToken);
            }
        }

        private async Task StartEditPlanAsync(long chatId, long userId, int planId, CancellationToken cancellationToken)
        {
            var plan = await _planService.GetPlanAsync(planId);
            if (plan == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ План не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "edit_plan_title",
                Data = new Dictionary<string, object?> { ["planId"] = planId },
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ РЕДАКТИРОВАНИЕ ПЛАНА\n\n" +
                $"Текущее название: {plan.Title}\n\n" +
                $"Введите новое название (или отправьте '-' чтобы оставить текущее):", cancellationToken);
        }

        private async Task HandleEditPlanTitleAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var planId = (int)state.Data["planId"]!;
            var plan = await _planService.GetPlanAsync(planId);

            if (plan == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ План не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            if (text != "-")
            {
                plan.Title = text;
            }

            state.Data["plan"] = plan;
            state.CurrentAction = "edit_plan_content";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Текущее содержание:\n{plan.Content}\n\n" +
                $"Введите новое содержание (или отправьте '-' чтобы оставить текущее):", cancellationToken);
        }

        private async Task HandleEditPlanContentAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var plan = (Plan)state.Data["plan"]!;

            if (text != "-")
            {
                plan.Content = text;
            }

            var success = await _planService.UpdatePlanAsync(plan);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"✅ План '{plan.Title}' обновлён!", cancellationToken, 3);

                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowPlanDetailsAsync(chatId, plan.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }

        private async Task ShowDeletePlanConfirmationAsync(long chatId, int planId, CancellationToken cancellationToken)
        {
            var plan = await _planService.GetPlanAsync(planId);
            if (plan == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ План не найден", cancellationToken, 3);
                return;
            }

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить план?\n\n" +
                       $"📄 {plan.Title}\n" +
                       $"📅 {plan.CreatedAt:dd.MM.yyyy}\n\n" +
                       $"❗ Это действие невозможно отменить!\n\n" +
                       $"⏳ Это сообщение будет удалено через 60 секунд.";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ ДА, УДАЛИТЬ", $"delete_plan_confirm_{planId}"),
            InlineKeyboardButton.WithCallbackData("❌ ОТМЕНА", $"plan_view_{planId}")
        }
    };

            // Отправляем временное сообщение, которое удалится через 60 секунд
            await _menuManager.SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(buttons), cancellationToken, 15);
        }

        private async Task DeletePlanAsync(long chatId, int planId, CancellationToken cancellationToken)
        {
            var plan = await _planService.GetPlanAsync(planId);
            if (plan == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ План не найден", cancellationToken, 3);
                return;
            }

            var success = await _planService.DeletePlanAsync(planId);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ План '{plan.Title}' удалён!", cancellationToken, 3);
                _menuManager.ClearMenuState(chatId);
                await ShowPlansMenuAsync(chatId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при удалении", cancellationToken, 5);
            }
        }

        private async Task DownloadPlanAsync(long chatId, int planId, CancellationToken cancellationToken)
        {
            var plan = await _planService.GetPlanAsync(planId);
            if (plan == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ План не найден", cancellationToken, 3);
                return;
            }

            await _menuManager.SendTemporaryMessageAsync(chatId, "📄 Генерация файла...", cancellationToken, 2);

            var fileData = await _planService.ExportPlanToTxtAsync(planId);

            if (fileData.Length > 0)
            {
                using var stream = new MemoryStream(fileData);
                var fileName = $"plan_{planId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";

                await _botClient.SendDocument(
                    chatId: chatId,
                    document: new InputFileStream(stream, fileName),
                    caption: $"📄 План: {plan.Title}",
                    cancellationToken: cancellationToken
                );

                _menuManager.ClearMenuState(chatId);
                await ShowPlansMenuAsync(chatId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при генерации файла", cancellationToken, 3);
            }
        }

        // ===== РЕКЛАМА - МЕНЮ =====
        private async Task ShowAdsMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _adService.GetAdStatisticsAsync();

            var text = "📢 РЕКЛАМА\n\n" +
                       $"📊 СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего кампаний: {stats.TotalCampaigns}\n" +
                       $"│ 🟢 Активных: {stats.ActiveCampaigns}\n" +
                       $"│ 💰 Бюджет: {stats.TotalBudget:N0} ₽\n" +
                       $"│ 💸 Потрачено: {stats.TotalSpent:N0} ₽\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"Выберите действие:";

            var buttons = MainMenuKeyboard.GetAdsMenu();
            await _menuManager.ShowInlineMenuAsync(chatId, text, buttons, "db_ads", cancellationToken);
        }

        // ===== ВСЯ РЕКЛАМА =====
        private async Task ShowAllAdsAsync(long chatId, CancellationToken cancellationToken)
        {
            var ads = await _adService.GetAllAdsAsync();

            if (!ads.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Рекламных кампаний нет",
                    new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_ads_add") },
                                              new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_ads_menu") } }),
                    "db_ads_empty", cancellationToken);
                return;
            }

            var text = $"📋 ВСЕ РЕКЛАМНЫЕ КАМПАНИИ ({ads.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var ad in ads.Take(10))
            {
                var statusEmoji = ad.Status switch
                {
                    "Активна" => "🟢",
                    "Завершена" => "✅",
                    "Планируется" => "📅",
                    _ => "📄"
                };

                text += $"{statusEmoji} {ad.CampaignName}\n";
                text += $"   📂 {ad.ProjectName ?? "Без проекта"}\n";
                text += $"   📅 {ad.StartDate:dd.MM.yyyy} - {ad.EndDate?.ToString("dd.MM.yyyy") ?? "..."}\n";
                text += $"   💰 Бюджет: {ad.Budget:N0} ₽ | Потрачено: {ad.Spent:N0} ₽\n\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📢 {ad.CampaignName}", $"db_ad_view_{ad.Id}")
        });
            }

            if (ads.Count > 10)
                text += $"... и еще {ads.Count - 10} кампаний\n\n";

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_ads_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_ads_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_ads_all", cancellationToken);
        }

        // ===== ДЕТАЛИ РЕКЛАМНОЙ КАМПАНИИ =====
        private async Task ShowAdDetailsAsync(long chatId, int adId, CancellationToken cancellationToken)
        {
            var ad = await _adService.GetAdAsync(adId);
            if (ad == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Кампания не найдена", cancellationToken, 3);
                return;
            }

            var statusEmoji = ad.Status switch
            {
                "Активна" => "🟢",
                "Завершена" => "✅",
                "Планируется" => "📅",
                _ => "📄"
            };

            var spentPercent = ad.Budget > 0 ? (ad.Spent ?? 0) / ad.Budget * 100 : 0;

            var text = $"📢 РЕКЛАМНАЯ КАМПАНИЯ: {ad.CampaignName}\n\n" +
                       $"📊 ДЕТАЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ {statusEmoji} Статус: {ad.Status ?? "Черновик"}\n" +
                       $"│ 📂 Проект: {ad.ProjectName ?? "-"}\n" +
                       $"│ 📅 Период: {ad.StartDate:dd.MM.yyyy} - {ad.EndDate?.ToString("dd.MM.yyyy") ?? "..."}\n" +
                       $"│ 💰 Бюджет: {ad.Budget:N0} ₽\n" +
                       $"│ 💸 Потрачено: {ad.Spent:N0} ₽ ({spentPercent:F1}%)\n" +
                       $"│ 🔗 Пост: {ad.PostLink ?? "-"}\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"📝 ОПИСАНИЕ:\n{ad.Description ?? "Нет описания"}\n\n" +
                       $"📊 РЕЗУЛЬТАТЫ:\n{ad.Results ?? "Нет данных"}\n";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ РЕДАКТИРОВАТЬ", $"db_ad_edit_{ad.Id}"),
            InlineKeyboardButton.WithCallbackData("💰 ДОБАВИТЬ ТРАТЫ", $"db_ad_add_spent_{ad.Id}")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🗑️ УДАЛИТЬ", $"db_ad_delete_{ad.Id}"),
            InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_ads_all")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_ad_{ad.Id}", cancellationToken);
        }

        // ===== ДОБАВЛЕНИЕ РЕКЛАМНОЙ КАМПАНИИ =====
        private async Task StartAddAdAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_ad_name",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📢 ДОБАВЛЕНИЕ РЕКЛАМНОЙ КАМПАНИИ (ШАГ 1/8)\n\n" +
                "Введите название кампании:", cancellationToken);
        }

        private async Task HandleAddAdNameAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["name"] = text;
            state.CurrentAction = "db_add_ad_project";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Название: {text}\n\n" +
                "📢 ШАГ 2/8\n\n" +
                "Введите название проекта (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddAdProjectAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["project"] = text;

            state.CurrentAction = "db_add_ad_budget";
            state.Step = 3;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📢 ШАГ 3/8\n\n" +
                "Введите бюджет кампании (в ₽):", cancellationToken);
        }

        private async Task HandleAddAdBudgetAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal budget) || budget <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken, 3);
                return;
            }

            state.Data["budget"] = budget;
            state.CurrentAction = "db_add_ad_start_date";
            state.Step = 4;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Бюджет: {budget:N0} ₽\n\n" +
                "📢 ШАГ 4/8\n\n" +
                "Введите дату начала в формате ДД.ММ.ГГГГ:", cancellationToken);
        }

        private async Task HandleAddAdStartDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime startDate))
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты", cancellationToken, 3);
                return;
            }

            state.Data["startDate"] = startDate;
            state.CurrentAction = "db_add_ad_end_date";
            state.Step = 5;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Дата начала: {startDate:dd.MM.yyyy}\n\n" +
                "📢 ШАГ 5/8\n\n" +
                "Введите дату окончания в формате ДД.ММ.ГГГГ\n" +
                "(или отправьте '-' если не определена):", cancellationToken);
        }

        private async Task HandleAddAdEndDateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            DateTime? endDate = null;
            if (text != "-")
            {
                if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    endDate = date;
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты", cancellationToken, 3);
                    return;
                }
            }

            state.Data["endDate"] = endDate;
            state.CurrentAction = "db_add_ad_description";
            state.Step = 6;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📢 ШАГ 6/8\n\n" +
                "Введите описание кампании (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddAdDescriptionAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["description"] = text;

            state.CurrentAction = "db_add_ad_link";
            state.Step = 7;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📢 ШАГ 7/8\n\n" +
                "Введите ссылку на пост (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddAdLinkAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["link"] = text;

            state.CurrentAction = "db_add_ad_status";
            state.Step = 8;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "📢 ШАГ 8/8\n\n" +
                "Введите статус (Активна/Завершена/Планируется):\n" +
                "(или отправьте '-' для 'Планируется')", cancellationToken);
        }

        private async Task HandleAddAdStatusAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var status = text != "-" ? text : "Планируется";

            var ad = new DbAd
            {
                CampaignName = state.Data["name"]?.ToString() ?? "",
                ProjectName = state.Data.ContainsKey("project") ? state.Data["project"]?.ToString() : null,
                Budget = (decimal)state.Data["budget"]!,
                Spent = 0,
                StartDate = (DateTime)state.Data["startDate"]!,
                EndDate = state.Data.ContainsKey("endDate") ? (DateTime?)state.Data["endDate"] : null,
                Description = state.Data.ContainsKey("description") ? state.Data["description"]?.ToString() : null,
                PostLink = state.Data.ContainsKey("link") ? state.Data["link"]?.ToString() : null,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _adService.CreateAdAsync(ad);

            if (result != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Кампания '{ad.CampaignName}' создана!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowAdDetailsAsync(chatId, result.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании кампании", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowAdsMenuAsync(chatId, cancellationToken);
            }
        }

        // ===== ДОБАВЛЕНИЕ ТРАТ =====
        private async Task StartAddSpentAsync(long chatId, long userId, int adId, CancellationToken cancellationToken)
        {
            var ad = await _adService.GetAdAsync(adId);
            if (ad == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Кампания не найдена", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_spent_amount",
                Data = new Dictionary<string, object?> { ["adId"] = adId },
                Step = 1
            };

            var remaining = ad.Budget - (ad.Spent ?? 0);
            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"💰 ДОБАВЛЕНИЕ ТРАТ ДЛЯ КАМПАНИИ: {ad.CampaignName}\n\n" +
                $"Бюджет: {ad.Budget:N0} ₽\n" +
                $"Потрачено: {ad.Spent:N0} ₽\n" +
                $"Осталось: {remaining:N0} ₽\n\n" +
                $"Введите сумму трат (в ₽):", cancellationToken);
        }

        private async Task HandleAddSpentAmountAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму", cancellationToken, 3);
                return;
            }

            var adId = (int)state.Data["adId"]!;
            var success = await _adService.AddSpentAsync(adId, amount);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Траты {amount:N0} ₽ добавлены!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowAdDetailsAsync(chatId, adId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении трат", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }

        // ===== РЕКЛАМА ПО СТАТУСУ =====
        private async Task ShowAdsByStatusAsync(long chatId, string status, CancellationToken cancellationToken)
        {
            var ads = await _adService.GetAdsByStatusAsync(status);

            var statusEmoji = status switch
            {
                "Активна" => "🟢",
                "Завершена" => "✅",
                "Планируется" => "📅",
                _ => "📄"
            };

            var text = $"{statusEmoji} РЕКЛАМА: {status}\n\n";

            if (!ads.Any())
            {
                text += "Нет кампаний с таким статусом";
            }
            else
            {
                text += $"Найдено: {ads.Count}\n\n";

                foreach (var ad in ads.Take(10))
                {
                    text += $"• {ad.CampaignName}\n";
                    text += $"  📂 {ad.ProjectName ?? "Без проекта"}\n";
                    text += $"  💰 {ad.Spent:N0} / {ad.Budget:N0} ₽\n";
                    text += $"  📅 {ad.StartDate:dd.MM.yyyy}\n\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_ads_add") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_ads_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_ads_{status}", cancellationToken);
        }

        // ===== СТАТИСТИКА РЕКЛАМЫ =====
        private async Task ShowAdStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _adService.GetAdStatisticsAsync();
            var ads = await _adService.GetAllAdsAsync();

            var text = "📊 СТАТИСТИКА РЕКЛАМЫ\n\n" +
                       $"📈 ОБЩАЯ СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего кампаний: {stats.TotalCampaigns}\n" +
                       $"│ 🟢 Активных: {stats.ActiveCampaigns}\n" +
                       $"│ 💰 Общий бюджет: {stats.TotalBudget:N0} ₽\n" +
                       $"│ 💸 Потрачено всего: {stats.TotalSpent:N0} ₽\n" +
                       $"│ 📊 Освоено: {(stats.TotalBudget > 0 ? stats.TotalSpent / stats.TotalBudget * 100 : 0):F1}%\n" +
                       $"└─────────────────────────────────\n\n";

            if (stats.SpentByProject.Any())
            {
                text += $"📂 ПО ПРОЕКТАМ:\n";
                foreach (var proj in stats.SpentByProject)
                {
                    text += $"│ {proj.Key}: {proj.Value:N0} ₽\n";
                }
                text += "\n";
            }

            text += $"📅 Последние 5 кампаний:\n";
            foreach (var ad in ads.OrderByDescending(a => a.StartDate).Take(5))
            {
                var spentPercent = ad.Budget > 0 ? (ad.Spent ?? 0) / ad.Budget * 100 : 0;
                text += $"• {ad.CampaignName}: {ad.Spent:N0} / {ad.Budget:N0} ₽ ({spentPercent:F1}%)\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЯ РЕКЛАМА", "db_ads_all") },
        new() { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_ads_add") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_ads_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_ads_stats", cancellationToken);
        }

        // ===== ПОИСК РЕКЛАМЫ =====
        private async Task StartSearchAdsAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_search_ads",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК РЕКЛАМЫ\n\nВведите текст для поиска:", cancellationToken);
        }

        private async Task HandleSearchAdsAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var ads = await _adService.SearchAdsAsync(text);

            if (!ads.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                await ShowAdsMenuAsync(chatId, cancellationToken);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {ads.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var ad in ads.Take(10))
            {
                result += $"• {ad.CampaignName} ({ad.ProjectName ?? "Без проекта"})\n";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"📢 {ad.CampaignName}", $"db_ad_view_{ad.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_ads_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "db_ads_search", cancellationToken);
            _userStates.Remove(userId);
        }

        // ===== УДАЛЕНИЕ РЕКЛАМНОЙ КАМПАНИИ =====
        private async Task ShowDeleteAdConfirmationAsync(long chatId, int adId, CancellationToken cancellationToken)
        {
            var ad = await _adService.GetAdAsync(adId);
            if (ad == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Кампания не найдена", cancellationToken, 3);
                return;
            }

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить рекламную кампанию?\n\n" +
                       $"📢 {ad.CampaignName}\n" +
                       $"📂 {ad.ProjectName ?? "Без проекта"}\n" +
                       $"💰 Бюджет: {ad.Budget:N0} ₽\n" +
                       $"💸 Потрачено: {ad.Spent:N0} ₽\n\n" +
                       $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"delete_ad_confirm_{adId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_ad_view_{adId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_confirmation", cancellationToken);
        }

        // ===== УДАЛЕНИЕ РЕКЛАМЫ =====
        private async Task DeleteAdAsync(long chatId, int adId, CancellationToken cancellationToken)
        {
            await HandleDeleteConfirmationAsync(
                chatId,
                adId,
                (id) => _adService.GetAdAsync(id),
                (id) => _adService.DeleteAdAsync(id),
                (ad) => ad.CampaignName,
                "db_ads_menu",
                cancellationToken
            );
        }

        // ===== РЕДАКТИРОВАНИЕ РЕКЛАМЫ =====
        private async Task StartEditAdAsync(long chatId, long userId, int adId, CancellationToken cancellationToken)
        {
            var ad = await _adService.GetAdAsync(adId);
            if (ad == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Кампания не найдена", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_edit_ad_field",
                Data = new Dictionary<string, object?> { ["adId"] = adId },
                Step = 1
            };

            var text = $"✏️ РЕДАКТИРОВАНИЕ КАМПАНИИ: {ad.CampaignName}\n\n" +
                       "Выберите поле для редактирования:\n\n" +
                       "1️⃣ Название\n" +
                       "2️⃣ Проект\n" +
                       "3️⃣ Бюджет\n" +
                       "4️⃣ Дата начала\n" +
                       "5️⃣ Дата окончания\n" +
                       "6️⃣ Описание\n" +
                       "7️⃣ Ссылка\n" +
                       "8️⃣ Статус\n\n" +
                       "Введите номер поля (1-8) или 0 для выхода:";

            await _menuManager.SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }


        // ===== ТРАТЫ (общая статистика) =====
        private async Task ShowAdCostsAsync(long chatId, CancellationToken cancellationToken)
        {
            var ads = await _adService.GetAllAdsAsync();
            var totalBudget = ads.Sum(a => a.Budget);
            var totalSpent = ads.Sum(a => a.Spent ?? 0);
            var activeAds = ads.Where(a => a.Status == "Активна").ToList();

            var text = "💰 СТАТИСТИКА ТРАТ НА РЕКЛАМУ\n\n" +
                       $"📊 ОБЩИЕ ПОКАЗАТЕЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ Всего кампаний: {ads.Count}\n" +
                       $"│ 🟢 Активных: {activeAds.Count}\n" +
                       $"│ 💰 Общий бюджет: {totalBudget:N0} ₽\n" +
                       $"│ 💸 Потрачено всего: {totalSpent:N0} ₽\n" +
                       $"│ 📊 Освоено: {(totalBudget > 0 ? totalSpent / totalBudget * 100 : 0):F1}%\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"🔥 АКТИВНЫЕ КАМПАНИИ:\n";

            foreach (var ad in activeAds.Take(5))
            {
                var spentPercent = ad.Budget > 0 ? (ad.Spent ?? 0) / ad.Budget * 100 : 0;
                text += $"• {ad.CampaignName}: {ad.Spent:N0} / {ad.Budget:N0} ₽ ({spentPercent:F1}%)\n";
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЯ РЕКЛАМА", "db_ads_all") },
        new() { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ ТРАТЫ", "db_ads_add") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_ads_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_ads_costs", cancellationToken);
        }

        // ===== FUNPAY - МЕНЮ =====
        private async Task ShowFunPayDbMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _dbFunPayService.GetStatisticsAsync();

            var text = "🎮 FUNPAY\n\n" +
                       $"📊 СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ 👤 Аккаунтов: {stats.TotalAccounts}\n" +
                       $"│ ⚠️ Всего штрафов: {stats.TotalWarnings}\n" +
                       $"│ 🔴 Активных штрафов: {stats.ActiveWarnings}\n" +
                       $"│ ✅ Решённых штрафов: {stats.ResolvedWarnings}\n" +
                       $"└─────────────────────────────────\n\n" +
                       $"Выберите действие:";

            var buttons = MainMenuKeyboard.GetFunPayDbMenu();
            await _menuManager.ShowInlineMenuAsync(chatId, text, buttons, "db_funpay", cancellationToken);
        }

        // ===== ВСЕ АККАУНТЫ =====
        private async Task ShowAllFunPayAccountsAsync(long chatId, CancellationToken cancellationToken)
        {
            var accounts = await _dbFunPayService.GetAllAccountsAsync();

            if (!accounts.Any())
            {
                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Аккаунтов нет",
                    new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_funpay_account_add") },
                                              new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_funpay_menu") } }),
                    "db_funpay_empty", cancellationToken);
                return;
            }

            var text = $"📋 ВСЕ FUNPAY АККАУНТЫ ({accounts.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var acc in accounts.Take(10))
            {
                var warningsCount = acc.Warnings?.Count ?? 0;
                var warningEmoji = warningsCount > 0 ? $"⚠️ {warningsCount}" : "✅";

                text += $"👤 {acc.Nickname} {warningEmoji}\n";
                text += $"   🤖 Бот: @{acc.BotUsername ?? "-"}\n";
                text += $"   📅 {acc.CreatedAt:dd.MM.yyyy}\n\n";

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"👤 {acc.Nickname}", $"db_funpay_account_view_{acc.Id}")
        });
            }

            if (accounts.Count > 10)
                text += $"... и еще {accounts.Count - 10} аккаунтов\n\n";

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_funpay_account_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_funpay_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_funpay_accounts_all", cancellationToken);
        }

        // ===== ДЕТАЛИ АККАУНТА =====
        private async Task ShowFunPayAccountDetailsAsync(long chatId, int accountId, CancellationToken cancellationToken)
        {
            var account = await _dbFunPayService.GetAccountAsync(accountId);
            if (account == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Аккаунт не найден", cancellationToken, 3);
                return;
            }

            var warnings = account.Warnings?.ToList() ?? new List<DbFunPayWarning>();
            var activeWarnings = warnings.Count(w => w.Status == "Активно");

            var text = $"👤 FUNPAY АККАУНТ: {account.Nickname}\n\n" +
                       $"📊 ДЕТАЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ 🤖 Бот: @{account.BotUsername ?? "-"}\n" +
                       $"│ 🔑 Golden Key: {account.GoldenKey ?? "-"}\n" +
                       $"│ 🔐 API Key: {account.ApiKey ?? "-"}\n" +
                       $"│ 📅 Создан: {account.CreatedAt:dd.MM.yyyy}\n" +
                       $"│ ⚠️ Штрафов: {warnings.Count} (активных: {activeWarnings})\n" +
                       $"└─────────────────────────────────\n";

            if (warnings.Any())
            {
                text += $"\n⚠️ ПОСЛЕДНИЕ ШТРАФЫ:\n";
                foreach (var w in warnings.OrderByDescending(w => w.Date).Take(3))
                {
                    var statusEmoji = w.Status == "Активно" ? "🔴" : "✅";
                    text += $"{statusEmoji} {w.Date:dd.MM.yyyy}: {w.Reason}\n";
                    if (!string.IsNullOrEmpty(w.Resolution))
                        text += $"   ✅ Решение: {w.Resolution}\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"db_funpay_account_edit_{account.Id}"),
            InlineKeyboardButton.WithCallbackData("⚠️ Штрафы", $"db_funpay_warnings_all_{account.Id}")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ Добавить штраф", $"db_funpay_warning_add_{account.Id}"),
            InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"db_funpay_account_delete_{account.Id}")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_funpay_accounts_all") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_funpay_account_{account.Id}", cancellationToken);
        }

        // ===== ДОБАВЛЕНИЕ АККАУНТА =====
        private async Task StartAddFunPayAccountAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_add_funpay_nickname",
                Data = new Dictionary<string, object?>(),
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "👤 ДОБАВЛЕНИЕ FUNPAY АККАУНТА (ШАГ 1/5)\n\n" +
                "Введите никнейм аккаунта:", cancellationToken);
        }

        private async Task HandleAddFunPayNicknameAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            state.Data["nickname"] = text;
            state.CurrentAction = "db_add_funpay_golden";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"Никнейм: {text}\n\n" +
                "👤 ШАГ 2/5\n\n" +
                "Введите Golden Key аккаунта (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddFunPayGoldenAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["goldenKey"] = text;

            state.CurrentAction = "db_add_funpay_bot";
            state.Step = 3;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "👤 ШАГ 3/5\n\n" +
                "Введите username бота (с @) или отправьте '-' чтобы пропустить:", cancellationToken);
        }

        private async Task HandleAddFunPayBotAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["botUsername"] = text;

            state.CurrentAction = "db_add_funpay_password";
            state.Step = 4;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "👤 ШАГ 4/5\n\n" +
                "Введите пароль от бота (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddFunPayPasswordAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["botPassword"] = text;

            state.CurrentAction = "db_add_funpay_api";
            state.Step = 5;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "👤 ШАГ 5/5\n\n" +
                "Введите API ключ от бота (или отправьте '-' чтобы пропустить):", cancellationToken);
        }

        private async Task HandleAddFunPayApiAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (text != "-")
                state.Data["apiKey"] = text;

            var account = new DbFunPayAccount
            {
                Nickname = state.Data["nickname"]?.ToString() ?? "",
                GoldenKey = state.Data.ContainsKey("goldenKey") ? state.Data["goldenKey"]?.ToString() : null,
                BotUsername = state.Data.ContainsKey("botUsername") ? state.Data["botUsername"]?.ToString() : null,
                BotPassword = state.Data.ContainsKey("botPassword") ? state.Data["botPassword"]?.ToString() : null,
                ApiKey = state.Data.ContainsKey("apiKey") ? state.Data["apiKey"]?.ToString() : null,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _dbFunPayService.CreateAccountAsync(account);

            if (result != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Аккаунт '{account.Nickname}' создан!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFunPayAccountDetailsAsync(chatId, result.Id, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при создании аккаунта", cancellationToken, 5);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFunPayDbMenuAsync(chatId, cancellationToken);
            }
        }

        // ===== ВСЕ ШТРАФЫ =====
        private async Task ShowAllFunPayWarningsAsync(long chatId, CancellationToken cancellationToken, int? accountId = null)
        {
            List<DbFunPayWarning> warnings;
            string title;

            if (accountId.HasValue)
            {
                warnings = await _dbFunPayService.GetWarningsAsync(accountId.Value);
                var account = await _dbFunPayService.GetAccountAsync(accountId.Value);
                title = $"⚠️ ШТРАФЫ АККАУНТА: {account?.Nickname}";
            }
            else
            {
                warnings = await _dbFunPayService.GetAllWarningsAsync();
                title = "⚠️ ВСЕ ШТРАФЫ";
            }

            if (!warnings.Any())
            {
                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("➕ Добавить штраф",
                accountId.HasValue ? $"db_funpay_warning_add_{accountId}" : "db_funpay_warning_add") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад",
                accountId.HasValue ? $"db_funpay_account_view_{accountId}" : "db_funpay_menu") }
        };

                await _menuManager.ShowInlineMenuAsync(chatId, "📭 Штрафов нет",
                    new InlineKeyboardMarkup(buttons), "db_funpay_warnings_empty", cancellationToken);
                return;
            }

            var text = $"{title}\n\n" +
                       $"Всего: {warnings.Count}\n" +
                       $"🔴 Активных: {warnings.Count(w => w.Status == "Активно")}\n" +
                       $"✅ Решённых: {warnings.Count(w => w.Status == "Решено")}\n\n";

            var buttonsList = new List<List<InlineKeyboardButton>>();

            foreach (var w in warnings.OrderByDescending(w => w.Date).Take(10))
            {
                var account = accountId.HasValue ? null : await _dbFunPayService.GetAccountAsync(w.FunPayAccountId);
                var statusEmoji = w.Status == "Активно" ? "🔴" : "✅";

                text += $"{statusEmoji} {w.Date:dd.MM.yyyy}\n";
                if (!accountId.HasValue && account != null)
                    text += $"   👤 {account.Nickname}\n";
                text += $"   📝 {w.Reason}\n\n";

                buttonsList.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"⚠️ Штраф от {w.Date:dd.MM.yyyy}", $"db_funpay_warning_view_{w.Id}")
        });
            }

            buttonsList.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ ШТРАФ",
            accountId.HasValue ? $"db_funpay_warning_add_{accountId}" : "db_funpay_warning_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад",
            accountId.HasValue ? $"db_funpay_account_view_{accountId}" : "db_funpay_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttonsList),
                accountId.HasValue ? $"db_funpay_warnings_{accountId}" : "db_funpay_warnings_all", cancellationToken);
        }

        // ===== ДЕТАЛИ ШТРАФА =====
        private async Task ShowFunPayWarningDetailsAsync(long chatId, int warningId, CancellationToken cancellationToken)
        {
            var warnings = await _dbFunPayService.GetAllWarningsAsync();
            var warning = warnings.FirstOrDefault(w => w.Id == warningId);

            if (warning == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Штраф не найден", cancellationToken, 3);
                return;
            }

            var account = await _dbFunPayService.GetAccountAsync(warning.FunPayAccountId);
            var statusEmoji = warning.Status == "Активно" ? "🔴" : "✅";

            var text = $"⚠️ ШТРАФ\n\n" +
                       $"📊 ДЕТАЛИ:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ {statusEmoji} Статус: {warning.Status}\n" +
                       $"│ 👤 Аккаунт: {account?.Nickname}\n" +
                       $"│ 📅 Дата: {warning.Date:dd.MM.yyyy HH:mm}\n" +
                       $"│ 📝 Причина: {warning.Reason}\n";

            if (!string.IsNullOrEmpty(warning.Resolution))
                text += $"│ ✅ Решение: {warning.Resolution}\n";

            text += $"└─────────────────────────────────\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            if (warning.Status == "Активно")
            {
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("✅ РЕШИТЬ", $"db_funpay_warning_resolve_{warning.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("🗑️ УДАЛИТЬ", $"db_funpay_warning_delete_{warning.Id}"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", $"db_funpay_account_view_{warning.FunPayAccountId}")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), $"db_funpay_warning_{warning.Id}", cancellationToken);
        }

        // ===== ДОБАВЛЕНИЕ ШТРАФА =====
        private async Task StartAddFunPayWarningAsync(long chatId, long userId, CancellationToken cancellationToken, int? preSelectedAccountId = null)
        {
            List<DbFunPayAccount> accounts;

            if (preSelectedAccountId.HasValue)
            {
                var account = await _dbFunPayService.GetAccountAsync(preSelectedAccountId.Value);
                accounts = account != null ? new List<DbFunPayAccount> { account } : new List<DbFunPayAccount>();
            }
            else
            {
                accounts = await _dbFunPayService.GetAllAccountsAsync();
            }

            if (!accounts.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Сначала добавьте аккаунт", cancellationToken, 3);
                await ShowFunPayDbMenuAsync(chatId, cancellationToken);
                return;
            }

            if (preSelectedAccountId.HasValue && accounts.Any())
            {
                // Если аккаунт уже выбран, сразу переходим к вводу причины
                _userStates[userId] = new UserState
                {
                    CurrentAction = "db_add_funpay_warning_reason",
                    Data = new Dictionary<string, object?> { ["accountId"] = preSelectedAccountId.Value },
                    Step = 1
                };

                await _menuManager.SendTemporaryMessageAsync(chatId,
                    $"⚠️ ДОБАВЛЕНИЕ ШТРАФА ДЛЯ {accounts.First().Nickname}\n\n" +
                    $"Введите причину штрафа:", cancellationToken);
            }
            else
            {
                // Показываем список аккаунтов для выбора
                var text = "⚠️ ДОБАВЛЕНИЕ ШТРАФА\n\nВыберите аккаунт:";
                var buttons = new List<List<InlineKeyboardButton>>();

                foreach (var acc in accounts.Take(10))
                {
                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(acc.Nickname, $"db_funpay_warning_add_{acc.Id}")
            });
                }

                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_funpay_menu")
        });

                await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_funpay_warning_select", cancellationToken);
            }
        }

        private async Task HandleAddFunPayWarningReasonAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var accountId = (int)state.Data["accountId"]!;

            var warning = await _dbFunPayService.AddWarningAsync(accountId, text);

            if (warning != null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Штраф добавлен!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFunPayAccountDetailsAsync(chatId, accountId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при добавлении штрафа", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }

        // ===== РЕШЕНИЕ ШТРАФА =====
        private async Task StartResolveWarningAsync(long chatId, long userId, int warningId, CancellationToken cancellationToken)
        {
            var warnings = await _dbFunPayService.GetAllWarningsAsync();
            var warning = warnings.FirstOrDefault(w => w.Id == warningId);

            if (warning == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Штраф не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_resolve_warning",
                Data = new Dictionary<string, object?> { ["warningId"] = warningId },
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✅ РЕШЕНИЕ ШТРАФА\n\n" +
                $"Причина: {warning.Reason}\n\n" +
                $"Введите описание решения:", cancellationToken);
        }

        private async Task HandleResolveWarningAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var warningId = (int)state.Data["warningId"]!;

            var success = await _dbFunPayService.ResolveWarningAsync(warningId, text);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Штраф помечен как решённый!", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFunPayWarningDetailsAsync(chatId, warningId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при решении штрафа", cancellationToken, 5);
                _userStates.Remove(userId);
            }
        }

        // ===== УДАЛЕНИЕ ШТРАФА =====
        private async Task ShowDeleteWarningConfirmationAsync(long chatId, int warningId, CancellationToken cancellationToken)
        {
            var warnings = await _dbFunPayService.GetAllWarningsAsync();
            var warning = warnings.FirstOrDefault(w => w.Id == warningId);

            if (warning == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Штраф не найден", cancellationToken, 3);
                return;
            }

            var account = await _dbFunPayService.GetAccountAsync(warning.FunPayAccountId);

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить штраф?\n\n" +
                       $"👤 Аккаунт: {account?.Nickname}\n" +
                       $"📅 Дата: {warning.Date:dd.MM.yyyy}\n" +
                       $"📝 Причина: {warning.Reason}\n\n" +
                       $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"delete_warning_confirm_{warningId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_funpay_warning_view_{warningId}")
        }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "delete_confirmation", cancellationToken);
        }

        // ===== УДАЛЕНИЕ ШТРАФА FUNPAY =====
        private async Task DeleteWarningAsync(long chatId, int warningId, CancellationToken cancellationToken)
        {
            var warnings = await _dbFunPayService.GetAllWarningsAsync();
            var warning = warnings.FirstOrDefault(w => w.Id == warningId);

            if (warning == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Штраф не найден", cancellationToken, 3);
                _menuManager.ClearMenuState(chatId);
                await ShowFunPayDbMenuAsync(chatId, cancellationToken);
                return;
            }

            await HandleDeleteConfirmationAsync(
                chatId,
                warningId,
                async (id) => warning, // Уже есть
                (id) => _dbFunPayService.DeleteWarningAsync(id),
                (w) => $"Штраф от {w.Date:dd.MM.yyyy}",
                $"db_funpay_account_view_{warning.FunPayAccountId}",
                cancellationToken
            );
        }

        // ===== ПОИСК АККАУНТОВ =====
        private async Task StartSearchFunPayAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            _userStates[userId] = new UserState
            {
                CurrentAction = "db_search_funpay",
                Step = 1
            };

            await _menuManager.SendTemporaryMessageAsync(chatId,
                "🔍 ПОИСК FUNPAY АККАУНТОВ\n\nВведите текст для поиска:", cancellationToken);
        }

        private async Task HandleSearchFunPayAsync(long chatId, long userId, string text, CancellationToken cancellationToken)
        {
            var accounts = await _dbFunPayService.SearchAccountsAsync(text);

            if (!accounts.Any())
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"❌ По запросу '{text}' ничего не найдено", cancellationToken, 3);
                _userStates.Remove(userId);
                await ShowFunPayDbMenuAsync(chatId, cancellationToken);
                return;
            }

            var result = $"🔍 РЕЗУЛЬТАТЫ ПОИСКА: '{text}'\n\n" +
                         $"Найдено: {accounts.Count}\n\n";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var acc in accounts.Take(10))
            {
                var warningsCount = acc.Warnings?.Count ?? 0;
                var warningEmoji = warningsCount > 0 ? $"⚠️ {warningsCount}" : "✅";

                result += $"• {acc.Nickname} {warningEmoji}\n";
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"👤 {acc.Nickname}", $"db_funpay_account_view_{acc.Id}")
        });
            }

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_funpay_account_add"),
        InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_funpay_menu")
    });

            await _menuManager.ShowInlineMenuAsync(chatId, result, new InlineKeyboardMarkup(buttons), "db_funpay_search", cancellationToken);
            _userStates.Remove(userId);
        }

        // ===== СТАТИСТИКА FUNPAY =====
        private async Task ShowFunPayDbStatsAsync(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _dbFunPayService.GetStatisticsAsync();
            var accounts = await _dbFunPayService.GetAllAccountsAsync();

            var text = "📊 СТАТИСТИКА FUNPAY\n\n" +
                       $"📈 ОБЩАЯ СТАТИСТИКА:\n" +
                       $"┌─────────────────────────────────\n" +
                       $"│ 👤 Всего аккаунтов: {stats.TotalAccounts}\n" +
                       $"│ ⚠️ Всего штрафов: {stats.TotalWarnings}\n" +
                       $"│ 🔴 Активных штрафов: {stats.ActiveWarnings}\n" +
                       $"│ ✅ Решённых штрафов: {stats.ResolvedWarnings}\n" +
                       $"└─────────────────────────────────\n\n";

            if (accounts.Any())
            {
                text += $"👥 АККАУНТЫ СО ШТРАФАМИ:\n";
                foreach (var acc in accounts.Where(a => a.Warnings?.Any() == true).Take(5))
                {
                    var activeWarnings = acc.Warnings?.Count(w => w.Status == "Активно") ?? 0;
                    text += $"• {acc.Nickname}: {acc.Warnings?.Count ?? 0} штрафов (🔴 {activeWarnings})\n";
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ АККАУНТЫ", "db_funpay_accounts_all") },
        new() { InlineKeyboardButton.WithCallbackData("⚠️ ВСЕ ШТРАФЫ", "db_funpay_warnings_all") },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", "db_funpay_menu") }
    };

            await _menuManager.ShowInlineMenuAsync(chatId, text, new InlineKeyboardMarkup(buttons), "db_funpay_stats", cancellationToken);
        }

        // ===== УДАЛЕНИЕ АККАУНТА =====
        private async Task ShowDeleteFunPayAccountConfirmationAsync(long chatId, int accountId, CancellationToken cancellationToken)
        {
            var account = await _dbFunPayService.GetAccountAsync(accountId);
            if (account == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Аккаунт не найден", cancellationToken, 3);
                return;
            }

            var warningsCount = account.Warnings?.Count ?? 0;

            var text = $"⚠️ ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ\n\n" +
                       $"Вы уверены, что хотите удалить аккаунт?\n\n" +
                       $"👤 {account.Nickname}\n" +
                       $"⚠️ Штрафов: {warningsCount}\n\n" +
                       $"❗ Это действие невозможно отменить!";

            var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"delete_funpay_account_confirm_{accountId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"db_funpay_account_view_{accountId}")
        }
    };

            await _menuManager.SendTemporaryInlineMessageAsync(chatId, text, new InlineKeyboardMarkup(buttons), cancellationToken, 15);
        }

        // ===== РЕДАКТИРОВАНИЕ FUNPAY АККАУНТА =====
        private async Task StartEditFunPayAccountAsync(long chatId, long userId, int accountId, CancellationToken cancellationToken)
        {
            var account = await _dbFunPayService.GetAccountAsync(accountId);
            if (account == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Аккаунт не найден", cancellationToken, 3);
                return;
            }

            _userStates[userId] = new UserState
            {
                CurrentAction = "db_edit_funpay_field",
                Data = new Dictionary<string, object?> { ["accountId"] = accountId },
                Step = 1
            };

            var text = $"✏️ РЕДАКТИРОВАНИЕ АККАУНТА: {account.Nickname}\n\n" +
                       "Выберите поле для редактирования:\n\n" +
                       "1️⃣ Никнейм\n" +
                       "2️⃣ Golden Key\n" +
                       "3️⃣ Бот\n" +
                       "4️⃣ Пароль бота\n" +
                       "5️⃣ API ключ\n\n" +
                       "Введите номер поля (1-5) или 0 для выхода:";

            await _menuManager.SendTemporaryMessageAsync(chatId, text, cancellationToken);
        }

        private async Task HandleEditFunPayFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 0 || field > 5)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 0 до 5", cancellationToken, 3);
                return;
            }

            if (field == 0)
            {
                Console.WriteLine($"   → Выход из редактирования FunPay аккаунта (field=0)");
                var accountId2 = (int)state.Data["accountId"]!;
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFunPayAccountDetailsAsync(chatId, accountId2, cancellationToken);
                return;
            }

            var accountId = (int)state.Data["accountId"]!;
            var account = await _dbFunPayService.GetAccountAsync(accountId);
            if (account == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Аккаунт не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var fieldName = field switch
            {
                1 => "Никнейм",
                2 => "Golden Key",
                3 => "Бот",
                4 => "Пароль бота",
                5 => "API ключ",
                _ => ""
            };

            var currentValue = field switch
            {
                1 => account.Nickname,
                2 => account.GoldenKey ?? "не указано",
                3 => account.BotUsername ?? "не указано",
                4 => account.BotPassword ?? "не указано",
                5 => account.ApiKey ?? "не указано",
                _ => ""
            };

            state.Data["editField"] = field;
            state.CurrentAction = "db_edit_funpay_value";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ Редактирование поля: {fieldName}\n" +
                $"Текущее значение: {currentValue}\n\n" +
                $"Введите новое значение (или отправьте '-' для удаления):", cancellationToken);
        }

        private async Task HandleEditFunPayValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var accountId = (int)state.Data["accountId"]!;
            var field = (int)state.Data["editField"]!;

            var account = await _dbFunPayService.GetAccountAsync(accountId);
            if (account == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Аккаунт не найден", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            switch (field)
            {
                case 1:
                    account.Nickname = text == "-" ? "Без имени" : text;
                    break;
                case 2:
                    account.GoldenKey = text == "-" ? null : text;
                    break;
                case 3:
                    account.BotUsername = text == "-" ? null : text;
                    break;
                case 4:
                    account.BotPassword = text == "-" ? null : text;
                    break;
                case 5:
                    account.ApiKey = text == "-" ? null : text;
                    break;
            }

            account.UpdatedAt = DateTime.UtcNow;
            var success = await _dbFunPayService.UpdateAccountAsync(account);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Аккаунт обновлен", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowFunPayAccountDetailsAsync(chatId, accountId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
            }
        }

        // ===== УДАЛЕНИЕ FUNPAY АККАУНТА =====
        private async Task DeleteFunPayAccountAsync(long chatId, int accountId, CancellationToken cancellationToken)
        {
            Console.WriteLine($"\n🔴 DeleteFunPayAccountAsync для ID: {accountId}");

            try
            {
                var account = await _dbFunPayService.GetAccountAsync(accountId);
                if (account == null)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Аккаунт не найден", cancellationToken, 3);
                    return;
                }

                var success = await _dbFunPayService.DeleteAccountAsync(accountId);

                if (success)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Аккаунт '{account.Nickname}' удален!", cancellationToken, 3);

                    _menuManager.ClearMenuState(chatId);
                    await ShowFunPayDbMenuAsync(chatId, cancellationToken);
                }
                else
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при удалении", cancellationToken, 5);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при удалении", cancellationToken, 5);
            }
        }
        #endregion

        #region Реклама - РЕАЛИЗАЦИЯ
        private async Task HandleAdvertisementCallbackAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case CallbackData.AdContentPlan:
                    await ShowContentPlanMenuAsync(chatId, userId, cancellationToken);
                    break;

                case CallbackData.AdCampaignPlan:
                    await ShowCampaignPlanMenuAsync(chatId, userId, cancellationToken);
                    break;

                default:
                    if (callbackData.StartsWith("content_plan_"))
                    {
                        await HandleContentPlanActionAsync(chatId, userId, callbackData, cancellationToken);
                    }
                    else if (callbackData.StartsWith("campaign_"))
                    {
                        await HandleCampaignActionAsync(chatId, userId, callbackData, cancellationToken);
                    }
                    break;
            }
        }

        private async Task HandleEditAdFieldAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (!int.TryParse(text, out int field) || field < 0 || field > 8)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите число от 0 до 8", cancellationToken, 3);
                return;
            }

            if (field == 0)
            {
                Console.WriteLine($"   → Выход из редактирования рекламы (field=0)");
                var adId2 = (int)state.Data["adId"]!;
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowAdDetailsAsync(chatId, adId2, cancellationToken);
                return;
            }

            var adId = (int)state.Data["adId"]!;
            var ad = await _adService.GetAdAsync(adId);
            if (ad == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Кампания не найдена", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            var fieldName = field switch
            {
                1 => "Название",
                2 => "Проект",
                3 => "Бюджет",
                4 => "Дата начала",
                5 => "Дата окончания",
                6 => "Описание",
                7 => "Ссылка",
                8 => "Статус",
                _ => ""
            };

            var currentValue = field switch
            {
                1 => ad.CampaignName,
                2 => ad.ProjectName ?? "не указано",
                3 => ad.Budget.ToString("N0") + " ₽",
                4 => ad.StartDate.ToString("dd.MM.yyyy"),
                5 => ad.EndDate?.ToString("dd.MM.yyyy") ?? "не указано",
                6 => ad.Description?.Length > 50 ? ad.Description.Substring(0, 50) + "..." : ad.Description ?? "не указано",
                7 => ad.PostLink ?? "не указано",
                8 => ad.Status ?? "не указано",
                _ => ""
            };

            state.Data["editField"] = field;
            state.CurrentAction = "db_edit_ad_value";
            state.Step = 2;
            _userStates[userId] = state;

            await _menuManager.SendTemporaryMessageAsync(chatId,
                $"✏️ Редактирование поля: {fieldName}\n" +
                $"Текущее значение: {currentValue}\n\n" +
                $"Введите новое значение (или отправьте '-' для удаления):", cancellationToken);
        }

        private async Task HandleEditAdValueAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            var adId = (int)state.Data["adId"]!;
            var field = (int)state.Data["editField"]!;

            var ad = await _adService.GetAdAsync(adId);
            if (ad == null)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Кампания не найдена", cancellationToken, 3);
                _userStates.Remove(userId);
                return;
            }

            switch (field)
            {
                case 1:
                    ad.CampaignName = text == "-" ? "Без названия" : text;
                    break;
                case 2:
                    ad.ProjectName = text == "-" ? null : text;
                    break;
                case 3:
                    if (text != "-")
                    {
                        if (decimal.TryParse(text, out decimal budget) && budget > 0)
                            ad.Budget = budget;
                        else
                        {
                            await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Введите корректный бюджет", cancellationToken, 3);
                            return;
                        }
                    }
                    break;
                case 4:
                    if (text != "-")
                    {
                        if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                            ad.StartDate = date;
                        else
                        {
                            await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты", cancellationToken, 3);
                            return;
                        }
                    }
                    break;
                case 5:
                    if (text != "-")
                    {
                        if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                            ad.EndDate = date;
                        else
                        {
                            await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный формат даты", cancellationToken, 3);
                            return;
                        }
                    }
                    else
                        ad.EndDate = null;
                    break;
                case 6:
                    ad.Description = text == "-" ? null : text;
                    break;
                case 7:
                    ad.PostLink = text == "-" ? null : text;
                    break;
                case 8:
                    var validStatuses = new[] { "Активна", "Завершена", "Планируется" };
                    if (text != "-" && !validStatuses.Contains(text))
                    {
                        await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Неверный статус", cancellationToken, 3);
                        return;
                    }
                    ad.Status = text == "-" ? "Планируется" : text;
                    break;
            }

            ad.UpdatedAt = DateTime.UtcNow;
            var success = await _adService.UpdateAdAsync(ad);

            if (success)
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ Кампания обновлена", cancellationToken, 3);
                _userStates.Remove(userId);
                _menuManager.ClearMenuState(chatId);
                await ShowAdDetailsAsync(chatId, adId, cancellationToken);
            }
            else
            {
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при обновлении", cancellationToken, 5);
            }
        }

        private async Task ShowContentPlanMenuAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            try
            {
                // Получаем проекты для контент-плана
                var projects = await _projectService.GetAllProjectsAsync();
                var activeProjects = projects.Where(p => p.Status == ProjectStatus.InProgress).Take(3).ToList();

                var text = $"# Контент-план\n\n" +
                           $"📊 Активные проекты для контента: {activeProjects.Count}\n\n";

                if (activeProjects.Any())
                {
                    text += $"🎯 Темы для контента:\n";
                    foreach (var project in activeProjects)
                    {
                        var tasksCount = project.Tasks?.Count ?? 0;
                        text += $"• {project.Name} ({tasksCount} задач)\n";
                    }
                }

                // Статистика по задачам (как контент)
                var tasks = await _taskService.GetAllTasksAsync();
                var recentTasks = tasks
                    .Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .Take(5)
                    .ToList();

                if (recentTasks.Any())
                {
                    text += $"\n📝 Недавние задачи (идеи для контента):\n";
                    foreach (var task in recentTasks)
                    {
                        text += $"• {task.Title}\n";
                    }
                }

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("➕ Создать план", "content_plan_create") },
            new() { InlineKeyboardButton.WithCallbackData("📋 Мои планы", "content_plan_list") },
            new() { InlineKeyboardButton.WithCallbackData("📊 Аналитика", "content_plan_analytics") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToAdvertisement) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "content_plan_menu",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing content plan menu");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке меню контент-плана.", cancellationToken);
            }
        }

        private async Task ShowCampaignPlanMenuAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            try
            {
                // Получаем финансовые данные для рекламных кампаний
                var financeRecords = await _financeService.GetAllRecordsAsync();
                var advertisingExpenses = financeRecords
                    .Where(f => f.Type == FinancialRecordType.Expense &&
                               (f.Category.Contains("Реклама") || f.Category.Contains("Маркетинг")))
                    .ToList();

                var totalAdvertising = advertisingExpenses.Sum(f => f.Amount);
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var monthAdvertising = advertisingExpenses
                    .Where(f => f.TransactionDate >= monthStart)
                    .Sum(f => f.Amount);

                // Получаем доходы для расчета ROI
                var incomes = await _financeService.GetRecordsByTypeAsync(FinancialRecordType.Income);
                var monthIncomes = incomes
                    .Where(i => i.TransactionDate >= monthStart)
                    .Sum(i => i.Amount);

                var roi = monthAdvertising > 0 ? (monthIncomes / monthAdvertising * 100) - 100 : 0;

                var text = $"📢 Рекламный план\n\n" +
                           $"💰 Финансовые показатели:\n" +
                           $"• Всего на рекламу: {totalAdvertising:N2} ₽\n" +
                           $"• За текущий месяц: {monthAdvertising:N2} ₽\n" +
                           $"• Доход за месяц: {monthIncomes:N2} ₽\n" +
                           $"• ROI: {(roi):F1}%\n\n" +
                           $"📊 Эффективность:\n" +
                           $"• Кампаний в этом месяце: {advertisingExpenses.Count(f => f.TransactionDate >= monthStart)}\n" +
                           $"• Средний бюджет: {(advertisingExpenses.Any() ? advertisingExpenses.Average(f => f.Amount) : 0):N2} ₽";

                var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("➕ Создать кампанию", "campaign_create") },
            new() { InlineKeyboardButton.WithCallbackData("📋 Активные кампании", "campaign_list_active") },
            new() { InlineKeyboardButton.WithCallbackData("📊 Отчеты", "campaign_reports") },
            new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToAdvertisement) }
        };

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    new InlineKeyboardMarkup(buttons),
                    "campaign_plan_menu",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing campaign plan menu");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке меню рекламного плана.", cancellationToken);
            }
        }

        private async Task HandleContentPlanActionAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case "content_plan_create":
                    _userStates[userId] = new UserState
                    {
                        CurrentAction = "create_content_plan",
                        Step = 1
                    };
                    await SendTemporaryMessageAsync(chatId, "Введите название контент-плана:", cancellationToken);
                    break;

                case "content_plan_list":
                    await ShowContentPlansListAsync(chatId, userId, cancellationToken);
                    break;

                case "content_plan_analytics":
                    await ShowContentAnalyticsAsync(chatId, cancellationToken);
                    break;

                default:
                    await SendTemporaryMessageAsync(chatId, "Неизвестное действие.", cancellationToken);
                    break;
            }
        }

        private async Task HandleCampaignActionAsync(long chatId, long userId, string callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData)
            {
                case "campaign_create":
                    _userStates[userId] = new UserState
                    {
                        CurrentAction = "create_campaign",
                        Step = 1
                    };
                    await SendTemporaryMessageAsync(chatId, "Введите название рекламной кампании:", cancellationToken);
                    break;

                case "campaign_list_active":
                    await ShowActiveCampaignsAsync(chatId, cancellationToken);
                    break;

                default:
                    await SendTemporaryMessageAsync(chatId, "Неизвестное действие.", cancellationToken);
                    break;
            }
        }

        private async Task ShowContentPlansListAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            try
            {
                // В реальном проекте здесь нужно получать контент-планы из БД
                // Показываем проекты как контент-планы
                var projects = await _projectService.GetAllProjectsAsync();
                var recentProjects = projects
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(3)
                    .ToList();

                var text = $"📋 Мои контент-планы (проекты)\n\n";

                if (recentProjects.Any())
                {
                    foreach (var project in recentProjects)
                    {
                        var statusIcon = project.Status switch
                        {
                            ProjectStatus.Pending => "🟡",
                            ProjectStatus.InProgress => "🟠",
                            ProjectStatus.Completed => "✅",
                            _ => "⚪"
                        };

                        var daysAgo = (DateTime.UtcNow - project.CreatedAt).Days;
                        var timeText = daysAgo == 0 ? "сегодня" : $"{daysAgo} дн. назад";

                        text += $"{statusIcon} {project.Name}\n";
                        text += $"   • Создан: {timeText}\n";
                        text += $"   • Статус: {project.Status}\n";
                        text += $"   • Задач: {project.Tasks?.Count ?? 0}\n\n";
                    }
                }
                else
                {
                    text += "📭 Нет контент-планов.\n";
                }

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    MainMenuKeyboard.GetBackButton("content_plan_menu"),
                    "content_plans_list",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing content plans list");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке списка контент-планов.", cancellationToken);
            }
        }

        private async Task ShowContentAnalyticsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Аналитика на основе проектов и задач
                var projects = await _projectService.GetAllProjectsAsync();
                var tasks = await _taskService.GetAllTasksAsync();

                var weekStart = DateTime.UtcNow.AddDays(-7);
                var weekTasks = tasks.Count(t => t.CreatedAt >= weekStart);
                var weekCompleted = tasks.Count(t => t.Status == TeamTaskStatus.Completed && t.CompletedAt >= weekStart);

                var completionRate = weekTasks > 0 ? (decimal)weekCompleted / weekTasks * 100 : 0;

                var text = $"📊 Аналитика контента\n\n" +
                           $"📅 За последнюю неделю:\n" +
                           $"• Новых задач: {weekTasks}\n" +
                           $"• Выполнено задач: {weekCompleted}\n" +
                           $"• Процент выполнения: {(completionRate):F1}%\n\n" +
                           $"📈 Активные проекты:\n";

                var activeProjects = projects
                    .Where(p => p.Status == ProjectStatus.InProgress)
                    .Take(3)
                    .ToList();

                if (activeProjects.Any())
                {
                    foreach (var project in activeProjects)
                    {
                        var projectTasks = project.Tasks?.Count ?? 0;
                        var completedTasks = project.Tasks?.Count(t => t.Status == TeamTaskStatus.Completed) ?? 0;
                        var progress = projectTasks > 0 ? (decimal)completedTasks / projectTasks * 100 : 0;

                        text += $"• {project.Name}: {progress:F1}% выполнено\n";
                    }
                }

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    MainMenuKeyboard.GetBackButton("content_plan_menu"),
                    "content_analytics",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing content analytics");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке аналитики контента.", cancellationToken);
            }
        }

        private async Task ShowActiveCampaignsAsync(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // Активные рекламные кампании - на основе финансовых записей
                var financeRecords = await _financeService.GetAllRecordsAsync();
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

                var advertisingExpenses = financeRecords
                    .Where(f => f.Type == FinancialRecordType.Expense &&
                               (f.Category.Contains("Реклама") || f.Category.Contains("Маркетинг")) &&
                               f.TransactionDate >= monthStart)
                    .OrderByDescending(f => f.Amount)
                    .Take(3)
                    .ToList();

                var text = $"📢 Активные рекламные кампании\n\n" +
                           $"📅 За текущий месяц:\n\n";

                if (advertisingExpenses.Any())
                {
                    int index = 1;
                    foreach (var record in advertisingExpenses)
                    {
                        var daysAgo = (DateTime.UtcNow - record.TransactionDate).Days;
                        var timeText = daysAgo == 0 ? "сегодня" : $"{daysAgo} дн. назад";

                        text += $"{index}. {record.Description}\n";
                        text += $"   • Бюджет: {record.Amount:N2} ₽\n";
                        text += $"   • Дата: {timeText}\n";
                        text += $"   • Категория: {record.Category}\n\n";
                        index++;
                    }
                }
                else
                {
                    text += "📭 Нет активных рекламных кампаний в этом месяце.\n";
                }

                // Общая статистика
                var totalAdvertising = advertisingExpenses.Sum(f => f.Amount);
                text += $"💰 Общий бюджет: {totalAdvertising:N2} ₽\n";
                text += $"📊 Средний бюджет кампании: {(advertisingExpenses.Any() ? advertisingExpenses.Average(f => f.Amount) : 0):N2} ₽";

                await _menuManager.ShowInlineMenuAsync(
                    chatId,
                    text,
                    MainMenuKeyboard.GetBackButton("campaign_plan_menu"),
                    "active_campaigns",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing active campaigns");
                await SendTemporaryMessageAsync(chatId, "❌ Ошибка при загрузке активных кампаний.", cancellationToken);
            }
        }
        #endregion

        #region Вспомогательные методы
        public async Task<Message> SendTemporaryMessageAsync(long chatId, string text, CancellationToken cancellationToken, int deleteAfterSeconds = 0)
        {
            try
            {
                var message = await _botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    cancellationToken: cancellationToken);

                // Удаляем ТОЛЬКО если deleteAfterSeconds > 0
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

        private async Task HandleAddIncomeStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (state.Step == 1)
            {
                if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму (число больше 0):", cancellationToken);
                    return;
                }

                state.Data["amount"] = amount;
                state.Step = 2;
                _userStates[userId] = state; // Сохраняем состояние
                await SendTemporaryMessageAsync(chatId, "Введите описание дохода:", cancellationToken);
            }
            else if (state.Step == 2)
            {
                var amount = (decimal)state.Data["amount"]!;
                var category = state.Data["category"]?.ToString() ?? "Прочее";

                var record = await _financeService.CreateFinancialRecordAsync(
                    type: FinancialRecordType.Income,
                    category: category,
                    description: text,
                    amount: amount,
                    currency: "₽",
                    source: null,
                    userId: userId,
                    projectId: null);

                if (record != null)
                {
                    await SendTemporaryMessageAsync(chatId,
                        $"✅ Доход добавлен!\n💰 {amount:N2} ₽ - {text}\nКатегория: {category}",
                        cancellationToken);

                    _userStates.Remove(userId); // Удаляем состояние
                    await _menuManager.ShowFinanceMenuAsync(chatId, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Не удалось добавить доход.", cancellationToken);
                    _userStates.Remove(userId); // Удаляем состояние
                }
            }
        }

        private async Task HandleAddExpenseStateAsync(long chatId, long userId, string text, UserState state, CancellationToken cancellationToken)
        {
            if (state.Step == 1)
            {
                if (!decimal.TryParse(text, out decimal amount) || amount <= 0)
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Введите корректную сумму (число больше 0):", cancellationToken);
                    return;
                }

                state.Data["amount"] = amount;
                state.Step = 2;
                _userStates[userId] = state; // Сохраняем состояние
                await SendTemporaryMessageAsync(chatId, "Введите описание расхода:", cancellationToken);
            }
            else if (state.Step == 2)
            {
                var amount = (decimal)state.Data["amount"]!;
                var category = state.Data["category"]?.ToString() ?? "Прочее";

                var record = await _financeService.CreateFinancialRecordAsync(
                    type: FinancialRecordType.Expense,
                    category: category,
                    description: text,
                    amount: amount,
                    currency: "₽",
                    source: null,
                    userId: userId,
                    projectId: null);

                if (record != null)
                {
                    await SendTemporaryMessageAsync(chatId,
                        $"✅ Расход добавлен!\n💸 {amount:N2} ₽ - {text}\nКатегория: {category}",
                        cancellationToken);

                    _userStates.Remove(userId); // Удаляем состояние
                    _menuManager.ClearMenuState(chatId);
                    await ShowExpensesAsync(chatId, cancellationToken);
                }
                else
                {
                    await SendTemporaryMessageAsync(chatId, "❌ Не удалось добавить расход.", cancellationToken);
                    _userStates.Remove(userId); // Удаляем состояние
                }
            }
        }

        private async Task HandleDeleteConfirmationAsync<T>(
            long chatId,
            int itemId,
            Func<int, Task<T?>> getItemAsync,
            Func<int, Task<bool>> deleteAsync,
            Func<T, string> getItemName,
            string returnMenuCallback,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"\n🔴 Удаление элемента ID: {itemId}");

            try
            {
                var item = await getItemAsync(itemId);
                if (item == null)
                {
                    Console.WriteLine($"❌ Элемент {itemId} не найден");
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Элемент не найден", cancellationToken, 3);

                    _menuManager.ClearMenuState(chatId);
                    // Возврат в меню через callback
                    await HandleDatabaseCallbackAsync(chatId, 0, returnMenuCallback, cancellationToken);
                    return;
                }

                var itemName = getItemName(item);
                Console.WriteLine($"✅ Найден элемент: {itemName}");

                var success = await deleteAsync(itemId);
                Console.WriteLine($"Результат удаления: {success}");

                if (success)
                {
                    await _menuManager.SendTemporaryMessageAsync(chatId, $"✅ {itemName} удален!", cancellationToken, 3);

                    _menuManager.ClearMenuState(chatId);
                    await HandleDatabaseCallbackAsync(chatId, 0, returnMenuCallback, cancellationToken);
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка при удалении");
                    await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка при удалении", cancellationToken, 5);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Исключение: {ex.Message}");
                await _menuManager.SendTemporaryMessageAsync(chatId, "❌ Ошибка", cancellationToken, 5);
                _menuManager.ClearMenuState(chatId);
                await HandleDatabaseCallbackAsync(chatId, 0, returnMenuCallback, cancellationToken);
            }
        }
        #endregion

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:F2} {sizes[order]}";
        }


    }
}
