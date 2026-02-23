using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeamManagerBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UpdateHandler> _logger;

        public UpdateHandler(
            ITelegramBotClient botClient,
            IServiceProvider serviceProvider,
            ILogger<UpdateHandler> logger)
        {
            _botClient = botClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        await HandleMessageAsync(scope, update.Message!, cancellationToken);
                        break;

                    case UpdateType.CallbackQuery:
                        await HandleCallbackQueryAsync(scope, update.CallbackQuery!, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
            }
        }

        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(exception, "Polling error occurred: {ErrorMessage}", errorMessage);
            await Task.CompletedTask;
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            await HandlePollingErrorAsync(botClient, exception, cancellationToken);
        }

        private async Task HandleMessageAsync(IServiceScope scope, Message message, CancellationToken cancellationToken)
        {
            if (message.Text == null || message.From == null) return;

            var mainMenuHandler = scope.ServiceProvider.GetRequiredService<MainMenuHandler>();
            var callbackQueryHandler = scope.ServiceProvider.GetRequiredService<CallbackQueryHandler>();

            if (message.Text.StartsWith("/start"))
            {
                await mainMenuHandler.HandleStartCommandAsync(message, cancellationToken);
            }
            else if (message.Text.StartsWith("/help"))
            {
                await SendHelpMessageAsync(message.Chat.Id, cancellationToken);
            }
            else
            {
                // Сначала проверяем состояние пользователя
                var wasHandled = await callbackQueryHandler.HandleMessageAsync(message, cancellationToken);

                // Если сообщение не обработано как состояние, то это выбор меню
                if (!wasHandled)
                {
                    await mainMenuHandler.HandleMainMenuSelectionAsync(message, cancellationToken);
                }
            }
        }

        private async Task HandleCallbackQueryAsync(IServiceScope scope, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Data == null || callbackQuery.Message == null) return;

            var callbackQueryHandler = scope.ServiceProvider.GetRequiredService<CallbackQueryHandler>();
            await callbackQueryHandler.HandleCallbackQueryAsync(callbackQuery, cancellationToken);
        }

        private async Task SendHelpMessageAsync(long chatId, CancellationToken cancellationToken)
        {
            var helpText = "🤖 Team Manager Bot - Помощь\n\n" +
                          "Основные команды:\n" +
                          "/start - Запуск бота и главное меню\n" +
                          "/help - Эта справка\n\n" +
                          "Для навигации используйте кнопки меню.\n\n" +
                          "📂 Проекты - управление проектами\n" +
                          "✅ Задачи - задачи и назначения\n" +
                          "💰 Бухгалтерия - финансы и учет\n" +
                          "📈 KPI - показатели эффективности\n" +
                          "👤 Контакты - база контактов\n" +
                          "🗃️ База данных - документация и ресурсы";

            await _botClient.SendMessage(
                chatId: chatId,
                text: helpText,
                cancellationToken: cancellationToken);
        }
    }
}