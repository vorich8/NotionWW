namespace TeamManagerBot
{
    public static class CallbackData
    {
        public const string ContactsMenu = "contacts_menu";
        public const string DropsMenu = "drops_menu";
        public const string ManualsMenu = "manuals_menu";
        public const string ReportsMenu = "reports_menu";
        public const string ProjectDocsMenu = "project_docs_menu";
        public const string AdDocsMenu = "ad_docs_menu";
        public const string TasksArchive = "tasks_archive";
        public const string TasksAssigned = "tasks_assigned";
        public const string TasksSettings = "tasks_settings";
        public const string TaskDeletePrefix = "task_delete_";
        public const string TaskConfirmDeletePrefix = "task_confirm_delete_";

        // Навигация
        public const string BackToMain = "back_to_main";
        public const string BackToProjects = "back_to_projects";
        public const string BackToTasks = "back_to_tasks";
        public const string BackToFinance = "back_to_finance";
        public const string BackToKpi = "back_to_kpi";
        public const string BackToContacts = "back_to_contacts";
        public const string BackToDatabase = "back_to_database";
        public const string BackToSettings = "back_to_settings";
        public const string BackToStatuses = "back_to_statuses";
        public const string BackToAdvertisement = "back_to_advertisement";

        // Проекты
        public const string CreateProject = "create_project";
        public const string ProjectsList = "projects_list";
        public const string ProjectPrefix = "project_";
        public const string EditProjectPrefix = "edit_project_";
        public const string DeleteProjectPrefix = "delete_project_";
        public const string ChangeStatusPrefix = "change_status_";
        public const string StatusPendingPrefix = "status_pending_";
        public const string StatusInProgressPrefix = "status_inprogress_";
        public const string StatusCompletedPrefix = "status_completed_";
        public const string DeleteConfirmPrefix = "delete_confirm_";
        public const string DeleteCancelPrefix = "delete_cancel_";

        // Задачи
        public const string TasksCreate = "tasks_create";
        public const string TasksList = "tasks_list";
        public const string TasksMy = "tasks_my";
        public const string TasksMembers = "tasks_members";
        public const string TaskPrefix = "task_";
        public const string TaskCompletePrefix = "task_complete_";

        // Статусы
        public const string StatusWrite = "status_write";
        public const string StatusBoard = "status_board";
        public const string StatusProgress = "status_progress";
        public const string StatusProjectPrefix = "status_project_";

        // Реклама
        public const string AdContentPlan = "ad_content_plan";
        public const string AdCampaignPlan = "ad_campaign_plan";

        // Контакты
        public const string ContactsAdd = "contacts_add";
        public const string ContactsSearch = "contacts_search";
        public const string ContactsList = "contacts_list";

        // Финансы
        public const string FinanceDeposit = "finance_deposit";
        public const string FinanceCommission = "finance_commission";
        public const string FinanceIncomes = "finance_incomes";
        public const string FinanceInvestments = "finance_investments";
        public const string FinanceCrypto = "finance_crypto";
        public const string FinanceExpenses = "finance_expenses";
        public const string FinanceFi = "finance_fi";
        public const string FinanceCategories = "finance_categories";
        public const string FinanceAddIncome = "finance_add_income";
        public const string FinanceAddExpense = "finance_add_expense";
        public const string FinanceStats = "finance_stats";

        // KPI
        public const string KpiTasksWeek = "kpi_tasks_week";
        public const string KpiFinanceMonth = "kpi_finance_month";
        public const string KpiProjects = "kpi_projects";
        public const string KpiActivity = "kpi_activity";
        public const string KpiOverall = "kpi_overall";
        public const string KpiTeam = "kpi_team";

        // Настройки
        public const string SettingsUsers = "settings_users";
        public const string SettingsSecurity = "settings_security";
        public const string SettingsReports = "settings_reports";
        public const string SettingsDatabase = "settings_database";
    }

    public static class UserActions
    {
        public const string CreateProject = "create_project";
        public const string EditProject = "edit_project";
        public const string AddContact = "add_contact";
        public const string SearchContact = "search_contact";
        public const string WriteStatus = "write_status";
        public const string CreateTask = "create_task";
        public const string AddIncome = "add_income";
        public const string AddExpense = "add_expense";
        public const string AddTaskMember = "add_task_member";
        public const string RemoveTaskMember = "remove_task_member";
        public const string ChangeUserRole = "change_user_role";
    }
}