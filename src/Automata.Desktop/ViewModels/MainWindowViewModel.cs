using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Automata.Application.Auth.Models;
using Automata.Application.Companies.Services;
using Automata.Application.Dashboard.Services;
using Automata.Application.Inventory.Models;
using Automata.Application.Inventory.Services;
using Automata.Application.Machines.Services;
using Automata.Application.Monitoring.Services;
using Automata.Application.Users.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// Разделы основного окна desktop-клиента.
    /// В перечислении оставлены только реально используемые экраны.
    /// </summary>
    public enum DesktopSection
    {
        Dashboard,
        MonitorTa,
        Inventory,
        InventoryManagement,
        Machines,
        Users,
        Companies,
    }

    /// <summary>
    /// Корневая ViewModel главного окна приложения.
    /// Управляет навигацией, доступом к админским экранам и выходом из сессии.
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const string AdminRoleName = "Администратор";

        // Карты подписей для верхней панели и заголовков контента.
        private static readonly ReadOnlyDictionary<DesktopSection, string> SectionPageTitles = new(
            new Dictionary<DesktopSection, string>
            {
                [DesktopSection.Dashboard] = "Главная",
                [DesktopSection.MonitorTa] = "Монитор ТА",
                [DesktopSection.Inventory] = "Учет ТМЦ",
                [DesktopSection.InventoryManagement] = "Учет ТМЦ",
                [DesktopSection.Machines] = "Торговые автоматы",
                [DesktopSection.Users] = "Пользователи",
                [DesktopSection.Companies] = "Компании",
            });

        private static readonly ReadOnlyDictionary<DesktopSection, string> SectionTopPaths = new(
            new Dictionary<DesktopSection, string>
            {
                [DesktopSection.Dashboard] = "Главная",
                [DesktopSection.MonitorTa] = "Главная / Монитор ТА",
                [DesktopSection.Inventory] = "Главная / Учет ТМЦ",
                [DesktopSection.InventoryManagement] = "Администрирование / Учет ТМЦ",
                [DesktopSection.Machines] = "Администрирование / Торговые автоматы",
                [DesktopSection.Users] = "Администрирование / Пользователи",
                [DesktopSection.Companies] = "Администрирование / Компании",
            });

        private static readonly ReadOnlyDictionary<DesktopSection, string> SectionHeaders = new(
            new Dictionary<DesktopSection, string>
            {
                [DesktopSection.Dashboard] = "Личный кабинет. Главная",
                [DesktopSection.MonitorTa] = "Монитор ТА",
                [DesktopSection.Inventory] = "Учет ТМЦ (просмотр)",
                [DesktopSection.InventoryManagement] = "Администрирование. Учет ТМЦ",
                [DesktopSection.Machines] = "Администрирование. Торговые автоматы",
                [DesktopSection.Users] = "Администрирование. Пользователи",
                [DesktopSection.Companies] = "Администрирование. Компании",
            });

        private readonly Dictionary<DesktopSection, ViewModelBase> _sections;
        private readonly string _employeeRole;
        private readonly Action _signOut;

        [ObservableProperty]
        private DesktopSection currentSection;

        [ObservableProperty]
        private ViewModelBase currentPage = null!;

        [ObservableProperty]
        private bool isAdminExpanded = true;

        public MainWindowViewModel()
            : this(
                null,
                null,
                null,
                null,
                null,
                null,
                new AuthenticatedUser
                {
                    DisplayName = "Пользователь",
                    RoleName = AdminRoleName,
                    Email = "demo@automata.local",
                })
        {
        }

        public MainWindowViewModel(
            IHomeDashboardService? homeDashboardService,
            IVendingMachineService? vendingMachineService,
            IProductInventoryService? productInventoryService,
            IMonitoringService? monitoringService,
            IUserAdministrationService? userAdministrationService,
            Action? signOut,
            AuthenticatedUser currentUser,
            ICompanyService? companyService = null)
        {
            if (currentUser is null)
            {
                throw new ArgumentNullException(nameof(currentUser));
            }

            _signOut = signOut ?? (() => { });

            _employeeRole = string.IsNullOrWhiteSpace(currentUser.RoleName)
                ? AdminRoleName
                : currentUser.RoleName.Trim();

            EmployeeName = string.IsNullOrWhiteSpace(currentUser.DisplayName)
                ? "Пользователь"
                : currentUser.DisplayName.Trim();
            EmployeeEmail = currentUser.Email;

            var dashboardViewModel = homeDashboardService is null
                ? new DashboardViewModel()
                : new DashboardViewModel(homeDashboardService);

            var machinesViewModel = vendingMachineService is null
                ? new MachinesViewModel()
                : new MachinesViewModel(vendingMachineService);

            var readOnlyInventoryViewModel = productInventoryService is null
                ? new InventoryViewModel(new DesignProductInventoryService(), false)
                : new InventoryViewModel(productInventoryService, false);

            var managementInventoryViewModel = productInventoryService is null
                ? new InventoryViewModel(new DesignProductInventoryService(), true)
                : new InventoryViewModel(productInventoryService, true);

            var usersViewModel = userAdministrationService is null
                ? new UsersViewModel()
                : new UsersViewModel(userAdministrationService, currentUser.Id, IsAdmin);

            var companiesViewModel = companyService is null
                ? new CompaniesViewModel()
                : new CompaniesViewModel(companyService, IsAdmin);

            var monitorTaViewModel = monitoringService is null
                ? new MonitorTaViewModel()
                : new MonitorTaViewModel(monitoringService);

            // Регистрируем доступные экраны. Здесь не должно быть mock/legacy-разделов.
            _sections = new Dictionary<DesktopSection, ViewModelBase>
            {
                [DesktopSection.Dashboard] = dashboardViewModel,
                [DesktopSection.MonitorTa] = monitorTaViewModel,
                [DesktopSection.Inventory] = readOnlyInventoryViewModel,
                [DesktopSection.InventoryManagement] = managementInventoryViewModel,
                [DesktopSection.Machines] = machinesViewModel,
                [DesktopSection.Users] = usersViewModel,
                [DesktopSection.Companies] = companiesViewModel,
            };

            CurrentSection = DesktopSection.Dashboard;
            CurrentPage = _sections[CurrentSection];
        }

        public string EmployeeInitials
        {
            get
            {
                var parts = EmployeeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    return "П";
                }

                if (parts.Length == 1)
                {
                    return parts[0][0].ToString().ToUpperInvariant();
                }

                return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
            }
        }

        public string EmployeeName { get; }
        public string EmployeeEmail { get; }
        public string EmployeeRole => _employeeRole;
        public string CompanyTitle => "ООО Торговые Автоматы";

        public bool IsAdmin =>
            string.Equals(_employeeRole, AdminRoleName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(_employeeRole, "Admin", StringComparison.OrdinalIgnoreCase);

        public bool CanShowAdminToggle => IsAdmin;
        public bool ShowAdminSection => IsAdmin && IsAdminExpanded;
        public string AdminMenuTitle => IsAdminExpanded ? "⚙  Администрирование     ˄" : "⚙  Администрирование     ˅";

        public string PageTitle => GetSectionTitle(SectionPageTitles);
        public string TopBarPath => GetSectionTitle(SectionTopPaths);
        public string ContentHeader => GetSectionTitle(SectionHeaders);

        partial void OnCurrentSectionChanged(DesktopSection value)
        {
            if (!IsAdmin && IsAdministrationSection(value))
            {
                CurrentSection = DesktopSection.Dashboard;
                return;
            }

            if (_sections.TryGetValue(value, out var page))
            {
                CurrentPage = page;
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(TopBarPath));
                OnPropertyChanged(nameof(ContentHeader));
            }
        }

        [RelayCommand]
        private void ShowDashboard() => CurrentSection = DesktopSection.Dashboard;

        [RelayCommand]
        private void ShowMonitorTa() => CurrentSection = DesktopSection.MonitorTa;

        [RelayCommand]
        private void ShowInventory() => CurrentSection = DesktopSection.Inventory;

        [RelayCommand]
        private void ShowInventoryManagement() => NavigateToAdminSection(DesktopSection.InventoryManagement);

        [RelayCommand]
        private void ShowMachines() => NavigateToAdminSection(DesktopSection.Machines);

        [RelayCommand]
        private void ShowUsers() => NavigateToAdminSection(DesktopSection.Users);

        [RelayCommand]
        private void ShowCompanies() => NavigateToAdminSection(DesktopSection.Companies);

        [RelayCommand]
        private void ToggleAdmin()
        {
            if (!IsAdmin)
            {
                return;
            }

            IsAdminExpanded = !IsAdminExpanded;
            OnPropertyChanged(nameof(AdminMenuTitle));
            OnPropertyChanged(nameof(ShowAdminSection));
        }

        [RelayCommand]
        private void SignOut() => _signOut();

        private static bool IsAdministrationSection(DesktopSection section)
        {
            return section is DesktopSection.InventoryManagement
                or DesktopSection.Machines
                or DesktopSection.Users
                or DesktopSection.Companies;
        }

        /// <summary>
        /// Единый метод перехода в админские разделы.
        /// Предотвращает случайный обход проверки роли через команду.
        /// </summary>
        private void NavigateToAdminSection(DesktopSection section)
        {
            if (!IsAdmin)
            {
                return;
            }

            CurrentSection = section;
        }

        private string GetSectionTitle(IReadOnlyDictionary<DesktopSection, string> source)
        {
            return source.TryGetValue(CurrentSection, out var title)
                ? title
                : "Раздел";
        }

        /// <summary>
        /// Минимальный design-time/stub сервис только для конструктора без зависимостей.
        /// </summary>
        private sealed class DesignProductInventoryService : IProductInventoryService
        {
            public Task<IReadOnlyList<ProductListItem>> GetProductsAsync(
                string? search,
                Guid? machineId,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<ProductListItem> items = Array.Empty<ProductListItem>();
                return Task.FromResult(items);
            }

            public Task<IReadOnlyList<MachineLookupItem>> GetMachinesAsync(
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<MachineLookupItem> items = Array.Empty<MachineLookupItem>();
                return Task.FromResult(items);
            }

            public Task<Guid> CreateProductAsync(
                ProductEditModel model,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Guid.NewGuid());
            }

            public Task UpdateProductAsync(
                ProductEditModel model,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
