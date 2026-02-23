using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TeamManagerBot.Services;
using TelegramUser = Telegram.Bot.Types.User;

namespace TeamManagerBot.Handlers
{
    public class MainMenuHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUserService _userService;
        private readonly ILogger<MainMenuHandler> _logger;
        private readonly MenuManager _menuManager;

        public MainMenuHandler(
            ITelegramBotClient botClient,
            IUserService userService,
            ILogger<MainMenuHandler> logger,
            MenuManager menuManager)
        {
            _botClient = botClient;
            _userService = userService;
            _logger = logger;
            _menuManager = menuManager;
        }

        public async Task HandleStartCommandAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.GetOrCreateUserAsync(
                    message.From!.Id,
                    message.From.Username ?? "unknown",
                    message.From.FirstName,
                    message.From.LastName);

                if (user == null)
                {
                    await SendTemporaryMessageAsync(message.Chat.Id,
                        "❌ Ошибка при создании пользователя. Обратитесь к администратору.",
                        cancellationToken);
                    return;
                }

                var isAdmin = await _userService.IsAdminAsync(user.TelegramId);
                await _menuManager.ShowMainMenuAsync(message.Chat.Id, user.TelegramId, isAdmin, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleStartCommandAsync");
                await SendTemporaryMessageAsync(message.Chat.Id,
                    "❌ Произошла ошибка при обработке команды.",
                    cancellationToken);
            }
        }

        public async Task HandleMainMenuSelectionAsync(Message message, CancellationToken cancellationToken)
        {
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine($"📱 ОБРАБОТКА ГЛАВНОГО МЕНЮ");
            Console.WriteLine($"├─ Chat ID: {message.Chat.Id}");
            Console.WriteLine($"├─ User ID: {message.From!.Id}");
            Console.WriteLine($"├─ Кнопка: {message.Text}");
            Console.WriteLine($"└─ Date: {DateTime.Now:HH:mm:ss}");

            var text = message.Text ?? string.Empty;
            var chatId = message.Chat.Id;
            var userId = message.From!.Id;

            try
            {
                var isAdmin = await _userService.IsAdminAsync(userId);
                await _userService.UpdateUserLastActiveAsync(userId);

                Console.WriteLine($"👤 Роль пользователя: {(isAdmin ? "Админ" : "Пользователь")}");

                switch (text)
                {
                    case "📂 Проекты":
                        Console.WriteLine($"📂 Выбрана кнопка: Проекты");
                        await _menuManager.ShowProjectsMenuAsync(chatId, cancellationToken);
                        break;

                    case "✅ Задачи":
                        Console.WriteLine($"📂 Выбрана кнопка: задачи");
                        await _menuManager.ShowTasksMenuAsync(chatId, isAdmin, cancellationToken);
                        break;

                    case "📊 Статусы":
                        Console.WriteLine($"📂 Выбрана кнопка: статусы");
                        await _menuManager.ShowStatusesMenuAsync(chatId, cancellationToken);
                        break;

                    case "📢 Реклама":
                        Console.WriteLine($"📂 Выбрана кнопка: реклама");
                        await _menuManager.ShowAdvertisementMenuAsync(chatId, cancellationToken);
                        break;

                    case "👤 Контакты":
                        Console.WriteLine($"📂 Выбрана кнопка: Контакты");
                        await _menuManager.ShowContactsMenuAsync(chatId, cancellationToken);
                        break;

                    case "🗃️ База данных":
                        Console.WriteLine($"📂 Выбрана кнопка: БД");
                        await _menuManager.ShowDatabaseMenuAsync(chatId, cancellationToken);
                        break;

                    case "💰 Бухгалтерия":
                        Console.WriteLine($"📂 Выбрана кнопка: Бухгалтерия");
                        await _menuManager.ShowFinanceMenuAsync(chatId, cancellationToken);
                        break;

                    case "📈 KPI":
                        Console.WriteLine($"📂 Выбрана кнопка: KPI");
                        await _menuManager.ShowKPIMenuAsync(chatId, cancellationToken);
                        break;

                    case "⚙️ Настройки":
                        if (isAdmin)
                        {
                            Console.WriteLine($"📂 Выбрана кнопка: Настройки");
                            await _menuManager.ShowSettingsMenuAsync(chatId, cancellationToken);
                        }
                        else
                        {
                            await SendTemporaryMessageAsync(chatId,
                                "⛔ У вас нет доступа к настройкам.",
                                cancellationToken);
                        }
                        break;

                    case "◀️ Назад в меню":
                        await _menuManager.ShowMainMenuAsync(chatId, userId, isAdmin, cancellationToken);
                        break;

                    default:
                        // Если текст не соответствует ни одной кнопке, игнорируем
                        Console.WriteLine($"❌ Неизвестная команда: {text}");
                        break;
                }
                Console.WriteLine($"✅ Меню обработано");
                Console.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА в HandleMainMenuSelectionAsync:");
                Console.WriteLine($"   └─ {ex.Message}");
                Console.WriteLine("════════════════════════════════════════");
                _logger.LogError(ex, "Error in HandleMainMenuSelectionAsync for chat {ChatId}", chatId);
                await SendTemporaryMessageAsync(chatId,
                    "❌ Произошла ошибка при обработке запроса.",
                    cancellationToken);
            }
        }

        private async Task SendTemporaryMessageAsync(
            long chatId,
            string text,
            CancellationToken cancellationToken)
        {
            try
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendTemporaryMessageAsync for chat {ChatId}", chatId);
            }
        }
    }
}