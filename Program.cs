using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamManagerBot.BackgroundServices;
using TeamManagerBot.Handlers;
using TeamManagerBot.Models;
using TeamManagerBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TeamManagerBot
{
    class Program
    {
        private static IHost? _host;
        private static ILogger<Program>? _logger;
        private static CancellationTokenSource _cts = new();

        static async Task Main(string[] args)
        {
            // Обработка Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n🛑 Получен сигнал остановки...");
                _cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                Console.WriteLine("🚀 Запуск Team Manager Bot...");
                Console.WriteLine("=".PadRight(50, '='));

                // Создаем и настраиваем хост
                _host = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(services, context.Configuration);
                    })
                    .Build();

                // Получаем логгер
                _logger = _host.Services.GetRequiredService<ILogger<Program>>();

                // Инициализация базы данных (ПЕРЕД запуском хоста!)
                await InitializeDatabaseAsync();

                // Настройка и запуск бота
                await RunBotAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                if (_logger != null)
                {
                    _logger.LogCritical(ex, "Critical error during bot startup");
                }
            }
            finally
            {
                Console.WriteLine("\n🛑 Остановка бота...");
                await (_host?.StopAsync() ?? Task.CompletedTask);
                _host?.Dispose();
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Логирование
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
                configure.SetMinimumLevel(LogLevel.Information); // Меняем с Debug на Information для продакшена

                // Форматирование для консоли
                configure.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "HH:mm:ss ";
                    options.UseUtcTimestamp = false;
                    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                });
            });

            // Конфигурация
            services.AddSingleton(configuration);

            // База данных - ВАЖНО: ConnectionString
            var connectionString = configuration["BotConfiguration:ConnectionString"]
                ?? "Data Source=TeamManager.db;Cache=Shared";

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(connectionString);
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors();
            });

            // Telegram Bot Client
            var botToken = configuration["BotConfiguration:BotToken"];
            if (string.IsNullOrEmpty(botToken))
            {
                throw new InvalidOperationException("Bot token is not configured. Please set BotConfiguration:BotToken in appsettings.json");
            }

            services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Creating Telegram Bot Client...");
                return new TelegramBotClient(botToken);
            });

            // Менеджер состояний меню (Singleton - должен быть один на все приложение)
            services.AddSingleton<MenuStateManager>();

            // ВАЖНО: MenuManager должен быть Scoped!
            services.AddScoped<MenuManager>();

            // Сервисы (Scoped)
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IFinanceService, FinanceService>();
            services.AddScoped<ITeamInvestmentService, TeamInvestmentService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<ICryptoService, CryptoService>();
            services.AddScoped<IFunPayService, FunPayService>();
            services.AddScoped<IFastInvestService, FastInvestService>();
            services.AddScoped<IDropService, DropService>();
            services.AddScoped<IManualService, ManualService>();
            services.AddScoped<IAdvertisementService, AdvertisementService>();
            services.AddScoped<ICommissionService, CommissionService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IManualService, ManualService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IAdService, AdService>();
            services.AddScoped<IDbFunPayService, DbFunPayService>();
            services.AddScoped<IPlanService, PlanService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddHostedService<NotificationWorker>();
            services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();

            // Обработчики (Scoped)
            services.AddScoped<MainMenuHandler>();
            services.AddScoped<CallbackQueryHandler>();

            // ВАЖНО: UpdateHandler регистрируем как IUpdateHandler (Scoped)
            services.AddScoped<IUpdateHandler, UpdateHandler>();

            // Receiver Service (HostedService - Singleton)
            services.AddHostedService<ReceiverService>();
        }

        private static async Task InitializeDatabaseAsync()
        {
            using var scope = _host!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Initializing database...");

                // 1. Проверяем, существует ли база данных
                var dbExists = await dbContext.Database.CanConnectAsync();

                if (!dbExists)
                {
                    // 2. СОЗДАЕМ БАЗУ ДАННЫХ И ВСЕ ТАБЛИЦЫ (только если её нет)
                    logger.LogInformation("Database not found. Creating new database and tables...");

                    // Используем EnsureCreated для создания БД и таблиц
                    var created = await dbContext.Database.EnsureCreatedAsync();

                    if (created)
                    {
                        logger.LogInformation("✅ Database created successfully!");
                        Console.WriteLine("✅ База данных создана успешно!");

                        // 3. Заполняем начальными данными (только для новой БД)
                        await SeedDatabaseAsync(scope.ServiceProvider);
                    }
                    else
                    {
                        logger.LogWarning("Database creation failed but no exception thrown");
                        Console.WriteLine("⚠️ Проблема при создании базы данных");
                    }
                }
                else
                {
                    logger.LogInformation("Database already exists");
                    Console.WriteLine("ℹ️ База данных уже существует");

                    // ОПЦИОНАЛЬНО: Проверяем и применяем миграции, если они есть
                    // await dbContext.Database.MigrateAsync();
                }

                // 4. Проверяем наличие администраторов (всегда)
                await EnsureAdminsExistAsync(scope.ServiceProvider);

                logger.LogInformation("✅ Database initialized successfully");
                Console.WriteLine("✅ База данных инициализирована успешно");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error initializing database");
                Console.WriteLine($"❌ Ошибка инициализации базы данных: {ex.Message}");
                throw; // Пробрасываем исключение дальше, чтобы остановить запуск
            }
        }

        private static async Task SeedDatabaseAsync(IServiceProvider services)
        {
            try
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Seeding database with initial data...");
                Console.WriteLine("🌱 Заполнение базы данных начальными данными...");

                var userService = services.GetRequiredService<IUserService>();
                var contactService = services.GetRequiredService<IContactService>();
                var projectService = services.GetRequiredService<IProjectService>();
                var taskService = services.GetRequiredService<ITaskService>();
                var financeService = services.GetRequiredService<IFinanceService>();

                // 1. Создаем администраторов из конфига
                var configuration = services.GetRequiredService<IConfiguration>();
                var adminIds = configuration.GetSection("BotConfiguration:AdminIds").Get<long[]>() ?? Array.Empty<long>();

                foreach (var adminId in adminIds)
                {
                    logger.LogInformation("Creating admin user: {AdminId}", adminId);

                    // Создаем пользователя-админа
                    var adminUser = await userService.GetOrCreateUserAsync(
                        adminId,
                        $"admin_{adminId}",
                        "Admin",
                        null);

                    if (adminUser != null && adminUser.Role != UserRole.Admin)
                    {
                        // Повышаем до админа
                        adminUser.Role = UserRole.Admin;
                        await userService.UpdateUserAsync(adminUser);
                    }

                    // Создаем контакт для админа
                    await contactService.CreateSimpleContactAsync(
                        $"admin_{adminId}",
                        "Admin User",
                        "Admin",
                        "admin,team");
                }

                // 2. Создаем тестовые данные для разработки (только если не продакшен)
#if DEBUG
                logger.LogInformation("Creating test data for development...");

                // Создаем тестовый проект
                var testProject = await projectService.CreateProjectAsync(
                    "Тестовый проект",
                    "Это тестовый проект для проверки функционала",
                    null,
                    adminIds.FirstOrDefault());

                if (testProject != null)
                {
                    // Создаем тестовые задачи
                    await taskService.CreateTaskAsync(
                        "Настроить бота",
                        "Настройка и запуск бота в Telegram",
                        testProject.Id,
                        adminIds.FirstOrDefault(),
                        adminIds.FirstOrDefault(),
                        DateTime.UtcNow.AddDays(7));

                    await taskService.CreateTaskAsync(
                        "Протестировать функционал",
                        "Проверить все разделы бота",
                        testProject.Id,
                        adminIds.FirstOrDefault(),
                        adminIds.FirstOrDefault(),
                        DateTime.UtcNow.AddDays(3));
                }

                // Создаем тестовую финансовую запись
                await financeService.CreateFinancialRecordAsync(
                    FinancialRecordType.Income,
                    "Продажи",
                    "Тестовый доход",
                    1000,
                    "USD",
                    "test",
                    adminIds.FirstOrDefault(),
                    testProject?.Id);
#endif

                logger.LogInformation("✅ Database seeded successfully");
                Console.WriteLine("✅ База данных заполнена начальными данными");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error seeding database");
                Console.WriteLine($"⚠️ Ошибка при заполнении базы данных: {ex.Message}");
                // Не пробрасываем исключение, чтобы бот мог запуститься даже без seed данных
            }
        }

        private static async Task EnsureAdminsExistAsync(IServiceProvider services)
        {
            try
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                var userService = services.GetRequiredService<IUserService>();
                var configuration = services.GetRequiredService<IConfiguration>();

                var adminIds = configuration.GetSection("BotConfiguration:AdminIds").Get<long[]>() ?? Array.Empty<long>();

                if (adminIds.Length == 0)
                {
                    logger.LogWarning("No admin IDs configured in appsettings.json");
                    Console.WriteLine("⚠️ ВНИМАНИЕ: Не настроены ID администраторов!");
                    return;
                }

                logger.LogInformation("Ensuring admin users exist: {AdminIds}", string.Join(", ", adminIds));

                foreach (var adminId in adminIds)
                {
                    var user = await userService.GetUserByTelegramIdAsync(adminId);

                    if (user == null)
                    {
                        // Создаем пользователя с правами администратора
                        user = await userService.GetOrCreateUserAsync(
                            adminId,
                            $"admin_{adminId}",
                            "Admin",
                            null);

                        if (user != null)
                        {
                            user.Role = UserRole.Admin;
                            await userService.UpdateUserAsync(user);
                            logger.LogInformation("Created admin user: {AdminId}", adminId);
                        }
                    }
                    else if (user.Role != UserRole.Admin)
                    {
                        // Повышаем существующего пользователя до админа
                        user.Role = UserRole.Admin;
                        await userService.UpdateUserAsync(user);
                        logger.LogInformation("Promoted user {AdminId} to admin", adminId);
                    }
                }

                logger.LogInformation("Admin users ensured successfully");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error ensuring admin users");
                // Не пробрасываем исключение, чтобы бот мог запуститься
            }
        }

        private static async Task RunBotAsync()
        {
            using var scope = _host!.Services.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Получаем информацию о боте
                var me = await botClient.GetMe(_cts.Token);

                Console.WriteLine($"🤖 Бот запущен: @{me.Username}");
                Console.WriteLine($"📛 Имя бота: {me.FirstName}");
                Console.WriteLine($"🆔 ID бота: {me.Id}");
                Console.WriteLine("=".PadRight(50, '='));
                Console.WriteLine("\n📱 Бот готов к работе! Отправьте /start в Telegram\n");
                Console.WriteLine("📊 Доступные команды:");
                Console.WriteLine("• /start - Запустить бота");
                Console.WriteLine("• /help - Помощь");
                Console.WriteLine("• Используйте кнопки меню для навигации\n");

                logger.LogInformation("Bot started: @{Username} (ID: {Id})", me.Username, me.Id);

                // Запускаем Receiver Service через хост
                await _host.StartAsync(_cts.Token);

                // Ждем отмены
                await Task.Delay(Timeout.Infinite, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Bot stopped by user request");
                Console.WriteLine("\n👋 Бот остановлен");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Bot stopped with error");
                Console.WriteLine($"\n❌ Бот остановлен с ошибкой: {ex.Message}");
                throw;
            }
        }
    }

    // Receiver Service для фоновой обработки обновлений
    public class ReceiverService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUpdateHandler _updateHandler;
        private readonly ILogger<ReceiverService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ReceiverService(
            ITelegramBotClient botClient,
            IUpdateHandler updateHandler,
            IServiceScopeFactory scopeFactory,
            ILogger<ReceiverService> logger)
        {
            _botClient = botClient;
            _updateHandler = updateHandler;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting polling...");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
                DropPendingUpdates = true // Игнорируем старые обновления при старте
            };

            try
            {
                // Запускаем polling
                await _botClient.ReceiveAsync(
                    updateHandler: _updateHandler,
                    receiverOptions: receiverOptions,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Polling stopped due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in polling");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping polling...");
            await base.StopAsync(cancellationToken);
        }
    }
}