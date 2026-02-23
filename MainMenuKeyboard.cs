using Telegram.Bot.Types.ReplyMarkups;

namespace TeamManagerBot.Keyboards
{
    public static class MainMenuKeyboard
    {
        public static InlineKeyboardMarkup GetMainMenu(bool isAdmin)
        {
            var buttons = new List<List<InlineKeyboardButton>>();

            // Первая строка
            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("📂 Проекты", "show_projects"),
        InlineKeyboardButton.WithCallbackData("✅ Задачи", "show_tasks"),
        InlineKeyboardButton.WithCallbackData("📊 Статусы", "show_statuses"),
    });

            // Вторая строка
            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("💰 Бухгалтерия", "show_finance"),
        InlineKeyboardButton.WithCallbackData("📈 KPI", "show_kpi")
    });

            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("🗃️ База данных", "show_database"),
        InlineKeyboardButton.WithCallbackData("📢 Планы", "show_plans")
    });
            // Шестая строка (только для админа)
            if (isAdmin)
            {
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("⚙️ Настройки", "show_settings")
        });
            }

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetProjectsMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Создать проект", CallbackData.CreateProject) },
                new() { InlineKeyboardButton.WithCallbackData("📋 Список проектов", CallbackData.ProjectsList) },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetTasksMenu(bool isAdmin)
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Создать задачу", CallbackData.TasksCreate) },
                new() { InlineKeyboardButton.WithCallbackData("📋 Список задач", CallbackData.TasksList) },
                new() { InlineKeyboardButton.WithCallbackData("👤 Мои задачи", CallbackData.TasksMy) },
                new() { InlineKeyboardButton.WithCallbackData("📁 Архив задач", CallbackData.TasksArchive) }
            };

            if (isAdmin)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("⚙️ Настройки участников", CallbackData.TasksSettings)
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain)
            });

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetStatusesMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("🗺️ Статусная доска", CallbackData.StatusBoard) },
                new() { InlineKeyboardButton.WithCallbackData("📈 Прогресс", CallbackData.StatusProgress) },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetAdvertisementMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("# Контент план", CallbackData.AdContentPlan) },
                new() { InlineKeyboardButton.WithCallbackData("📢 Рекламный план", CallbackData.AdCampaignPlan) },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        // ОБНОВЛЕНО: Добавлены кнопки для банковских карт и крипто-кошельков

        public static InlineKeyboardMarkup GetContactsDatabaseMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        // Основные действия
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_contact_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "db_contacts_search")
        },
        
        // Фильтры по статусу
        new()
        {
            InlineKeyboardButton.WithCallbackData("🟢 РАБОЧИЕ", "db_contacts_status_working"),
            InlineKeyboardButton.WithCallbackData("🔒 ЛОК", "db_contacts_status_locked"),
            InlineKeyboardButton.WithCallbackData("⚠️ 115/161", "db_contacts_status_blocked")
        },
        
        // По наличию данных
        new()
        {
            InlineKeyboardButton.WithCallbackData("💳 С КАРТАМИ", "db_contacts_with_cards"),
            InlineKeyboardButton.WithCallbackData("🆔 С ПАСПОРТАМИ", "db_contacts_with_passports")
        },
        
        // Статистика и все контакты
        new()
        {
            InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", "db_contacts_stats"),
            InlineKeyboardButton.WithCallbackData("📋 ВСЕ", "db_contacts_all")
        },

        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToDatabase) }
    };

            return new InlineKeyboardMarkup(buttons);
        }
        public static InlineKeyboardMarkup GetContactsMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Добавить контакт", CallbackData.ContactsAdd) },
                new() { InlineKeyboardButton.WithCallbackData("🔍 Поиск", CallbackData.ContactsSearch) },
                new() { InlineKeyboardButton.WithCallbackData("📋 Список", CallbackData.ContactsList) },
                new() {
                    InlineKeyboardButton.WithCallbackData("💳 Банковские карты", "contacts_banks_all"),
                    InlineKeyboardButton.WithCallbackData("₿ Крипто-кошельки", "contacts_crypto_all")
                },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetDatabaseMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        // 1. Посты
        new() { InlineKeyboardButton.WithCallbackData("📝 Посты", "db_posts_menu") },
        
        // 2. Контакты (Дропы)
        new() { InlineKeyboardButton.WithCallbackData("👤 Контакты (ДРОПЫ)", "db_contacts_menu") },
        
        // 3. Мануалы по картам
        new() { InlineKeyboardButton.WithCallbackData("📚 Мануалы", "db_manuals_menu") },
        
        // 4. Отчёты инвесторам
        new() { InlineKeyboardButton.WithCallbackData("📊 Отчет инвесторам", "db_reports_menu") },
        
        // 5. Документация
        new() { InlineKeyboardButton.WithCallbackData("📋 Документация", "db_docs_menu") },
        
        // 6. Реклама
        new() { InlineKeyboardButton.WithCallbackData("📢 Реклама", "db_ads_menu") },
        
        // 7. FunPay
        new() { InlineKeyboardButton.WithCallbackData("🎮 FUNPAY", "db_funpay_menu") },
        
        // Назад
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain) }
    };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetFinanceMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                // 1. Учёты - подменю
                new() { InlineKeyboardButton.WithCallbackData("📊 Учеты", "finance_accounts") },
        
                // 2. Вклады участников
                new() { InlineKeyboardButton.WithCallbackData("👥 Вклады WWTEAM", "finance_investments") },
        
                // 3. Траты
                new() { InlineKeyboardButton.WithCallbackData("📉 ТРАТЫ", "finance_expenses") },
        
                // 4. Комиссии
                new() { InlineKeyboardButton.WithCallbackData("📊 Комиссии", "finance_commissions_menu") },
        
                // Навигация
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetKPIMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("📊 Задачи за неделю", CallbackData.KpiTasksWeek) },
                new() { InlineKeyboardButton.WithCallbackData("💰 Финансы за месяц", CallbackData.KpiFinanceMonth) },
                new() { InlineKeyboardButton.WithCallbackData("📈 Прогресс проектов", CallbackData.KpiProjects) },
                new() { InlineKeyboardButton.WithCallbackData("👥 Активность", CallbackData.KpiActivity) },
                new() { InlineKeyboardButton.WithCallbackData("📊 Общий KPI", CallbackData.KpiOverall) },
                new() { InlineKeyboardButton.WithCallbackData("👥 KPI команды", CallbackData.KpiTeam) },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain) }
            };

            return new InlineKeyboardMarkup(buttons);
        }
        public static InlineKeyboardMarkup GetSettingsMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("👥 Пользователи", CallbackData.SettingsUsers) },
        new() { InlineKeyboardButton.WithCallbackData("🔐 Безопасность", CallbackData.SettingsSecurity) },
        new() { InlineKeyboardButton.WithCallbackData("📊 Отчеты", CallbackData.SettingsReports) },
        new() { InlineKeyboardButton.WithCallbackData("🔔 Уведомления", "settings_notifications") },
        new() { InlineKeyboardButton.WithCallbackData("💾 База данных", CallbackData.SettingsDatabase) },
        new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToMain) }
    };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetProjectActions(int projectId)
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"{CallbackData.EditProjectPrefix}{projectId}") },
                new() { InlineKeyboardButton.WithCallbackData("📊 Сменить статус", $"{CallbackData.ChangeStatusPrefix}{projectId}") },
                new() { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"{CallbackData.DeleteProjectPrefix}{projectId}") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToProjects) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetProjectStatusChange(int projectId)
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("🟡 Предстоит", $"{CallbackData.StatusPendingPrefix}{projectId}") },
                new() { InlineKeyboardButton.WithCallbackData("🟠 В работе", $"{CallbackData.StatusInProgressPrefix}{projectId}") },
                new() { InlineKeyboardButton.WithCallbackData("✅ Готово", $"{CallbackData.StatusCompletedPrefix}{projectId}") },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", $"{CallbackData.ProjectPrefix}{projectId}") }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetBackButton(string callbackData = CallbackData.BackToMain)
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", callbackData) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        // ОБНОВЛЕНО: Реалистичные категории доходов
        public static InlineKeyboardMarkup GetIncomeCategories()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData("💼 Продажи", "income_category_Продажи"),
                    InlineKeyboardButton.WithCallbackData("📊 Услуги", "income_category_Услуги")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("🤝 Партнерские", "income_category_Партнерские"),
                    InlineKeyboardButton.WithCallbackData("💎 Инвестиции", "income_category_Инвестиции")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("🎨 Творчество", "income_category_Творчество"),
                    InlineKeyboardButton.WithCallbackData("🏦 Проценты", "income_category_Проценты")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("📈 Трейдинг", "income_category_Трейдинг"),
                    InlineKeyboardButton.WithCallbackData("💸 Прочее", "income_category_Прочее")
                },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceIncomes) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        // ОБНОВЛЕНО: Реалистичные категории расходов
        public static InlineKeyboardMarkup GetExpenseCategories()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData("🏢 Аренда", "expense_category_Аренда"),
                    InlineKeyboardButton.WithCallbackData("💰 Зарплата", "expense_category_Зарплата")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("📢 Реклама", "expense_category_Реклама"),
                    InlineKeyboardButton.WithCallbackData("💻 Оборудование", "expense_category_Оборудование")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("📱 Софт", "expense_category_Софт"),
                    InlineKeyboardButton.WithCallbackData("📊 Комиссии", "expense_category_Комиссии")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("🍕 Еда", "expense_category_Еда"),
                    InlineKeyboardButton.WithCallbackData("🚕 Транспорт", "expense_category_Транспорт")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("💊 Здоровье", "expense_category_Здоровье"),
                    InlineKeyboardButton.WithCallbackData("📚 Обучение", "expense_category_Обучение")
                },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.FinanceExpenses) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        // НОВЫЙ: Меню для банковских карт контакта
        public static InlineKeyboardMarkup GetContactBanksMenu(int contactId, bool hasCards)
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Добавить карту", $"contact_add_bank_{contactId}") }
            };

            if (hasCards)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("💳 Управление картами", $"contact_banks_{contactId}")
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", $"contact_{contactId}")
            });

            return new InlineKeyboardMarkup(buttons);
        }

        // НОВЫЙ: Меню для крипто-кошельков контакта
        public static InlineKeyboardMarkup GetContactCryptoMenu(int contactId, bool hasWallets)
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new() { InlineKeyboardButton.WithCallbackData("➕ Добавить кошелек", $"contact_add_crypto_{contactId}") }
            };

            if (hasWallets)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("₿ Управление кошельками", $"contact_crypto_{contactId}")
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад", $"contact_{contactId}")
            });

            return new InlineKeyboardMarkup(buttons);
        }

        // НОВЫЙ: Кнопки для депозита с разделением на рабочие/нерабочие средства
        public static InlineKeyboardMarkup GetDepositMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData("💰 В оборот", "finance_deposit_to_working"),
                    InlineKeyboardButton.WithCallbackData("🏦 В резерв", "finance_deposit_to_reserved")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("➕ Пополнить", "finance_deposit_add"),
                    InlineKeyboardButton.WithCallbackData("➖ Вывести", "finance_deposit_withdraw")
                },
                new()
                {
                    InlineKeyboardButton.WithCallbackData("↔️ Переместить", "finance_deposit_transfer"),
                    InlineKeyboardButton.WithCallbackData("📊 История", "finance_deposit_history")
                },
                new() { InlineKeyboardButton.WithCallbackData("◀️ Назад", CallbackData.BackToFinance) }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        // ===== МЕНЮ ПОСТОВ =====
        public static InlineKeyboardMarkup GetPostsMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ ПОСТЫ", "db_posts_all") },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_posts_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "db_posts_search")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", "db_posts_stats"),
            InlineKeyboardButton.WithCallbackData("📁 ПО КАНАЛАМ", "db_posts_by_channel")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToDatabase) }
    };
            return new InlineKeyboardMarkup(buttons);
        }

        // ===== МЕНЮ МАНУАЛОВ =====
        public static InlineKeyboardMarkup GetManualsMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ МАНУАЛЫ", "db_manuals_all") },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_manuals_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "db_manuals_search")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📌 ОСНОВНЫЕ", "db_manuals_main"),
            InlineKeyboardButton.WithCallbackData("📎 ДОПОЛНИТЕЛЬНЫЕ", "db_manuals_additional")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🧪 ТЕСТОВЫЕ", "db_manuals_test"),
            InlineKeyboardButton.WithCallbackData("🏦 ПО БАНКАМ", "db_manuals_by_bank")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🌑 ОБХОД ТЕНЕВОГО БАНА", "db_manuals_shadowban"),
            InlineKeyboardButton.WithCallbackData("🔓 СНЯТИЕ 115/161", "db_manuals_unblock")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToDatabase) }
    };
            return new InlineKeyboardMarkup(buttons);
        }

        // ===== МЕНЮ ОТЧЁТОВ =====
        public static InlineKeyboardMarkup GetReportsMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ ОТЧЁТЫ", "db_reports_all") },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ СОЗДАТЬ", "db_reports_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "db_reports_search")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", "db_reports_stats"),
            InlineKeyboardButton.WithCallbackData("📤 ЭКСПОРТ PDF", "db_reports_export")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToDatabase) }
    };
            return new InlineKeyboardMarkup(buttons);
        }

        // ===== МЕНЮ ДОКУМЕНТАЦИИ =====
        public static InlineKeyboardMarkup GetDocsMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЯ ДОКУМЕНТАЦИЯ", "db_docs_all") },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_docs_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "db_docs_search")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("📂 ПО ПРОЕКТАМ", "db_docs_by_project"),
            InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", "db_docs_stats")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToDatabase) }
    };
            return new InlineKeyboardMarkup(buttons);
        }

        // ===== МЕНЮ РЕКЛАМЫ И ПЛАНОВ =====
        public static InlineKeyboardMarkup GetPlansMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ ПЛАНЫ", "plans_all") },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ НОВЫЙ ПЛАН", "plans_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "plans_search")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToMain) } // ← В главное меню
    };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetAdsMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЯ РЕКЛАМА", "db_ads_all") },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ", "db_ads_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "db_ads_search")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("💰 ТРАТЫ", "db_ads_costs"),
            InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", "db_ads_stats")
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData("🟢 АКТИВНЫЕ", "db_ads_active"),
            InlineKeyboardButton.WithCallbackData("✅ ЗАВЕРШЕННЫЕ", "db_ads_completed")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToDatabase) }
    };
            return new InlineKeyboardMarkup(buttons);
        }

        // ===== МЕНЮ FUNPAY =====
        public static InlineKeyboardMarkup GetFunPayDbMenu()
        {
            var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("📋 ВСЕ АККАУНТЫ", "db_funpay_accounts_all") },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ АККАУНТ", "db_funpay_account_add"),
            InlineKeyboardButton.WithCallbackData("🔍 ПОИСК", "db_funpay_search")
        },
        new() { InlineKeyboardButton.WithCallbackData("⚠️ ВСЕ ШТРАФЫ", "db_funpay_warnings_all") },
        new()
        {
            InlineKeyboardButton.WithCallbackData("➕ ДОБАВИТЬ ШТРАФ", "db_funpay_warning_add"),
            InlineKeyboardButton.WithCallbackData("📊 СТАТИСТИКА", "db_funpay_stats")
        },
        new() { InlineKeyboardButton.WithCallbackData("◀️ НАЗАД", CallbackData.BackToDatabase) }
    };
            return new InlineKeyboardMarkup(buttons);
        }
    }
}