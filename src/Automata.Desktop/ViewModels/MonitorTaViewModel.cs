using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automata.Application.Common;
using Automata.Application.Monitoring.Models;
using Automata.Application.Monitoring.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// Вариант сортировки списка автоматов на экране мониторинга.
    /// </summary>
    public sealed class MonitoringSortOption
    {
        public required string Value { get; init; }
        public required string Name { get; init; }
    }

    /// <summary>
    /// Строка-карточка автомата в desktop-мониторинге.
    /// </summary>
    public sealed class MonitoringMachineRowViewModel
    {
        public Guid Id { get; init; }
        public int Number { get; init; }
        public string MachineName { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public string ModelDisplayName { get; init; } = string.Empty;
        public string ConnectionText { get; init; } = string.Empty;
        public string StatusText { get; init; } = string.Empty;
        public string StatusBadgeBackground { get; init; } = "#D1FAE5";
        public string StatusBadgeForeground { get; init; } = "#065F46";
        public string LoadOverallText { get; init; } = string.Empty;
        public string LoadMinText { get; init; } = string.Empty;
        public string MoneyLine1 { get; init; } = string.Empty;
        public string MoneyLine2 { get; init; } = string.Empty;
        public string MoneyLine3 { get; init; } = string.Empty;
        public string EventsLine1 { get; init; } = string.Empty;
        public string EventsLine2 { get; init; } = string.Empty;
        public string EquipmentText { get; init; } = string.Empty;
        public string InformationText { get; init; } = string.Empty;
        public string ExtraText { get; init; } = string.Empty;
        public bool IsAttentionRequired { get; init; }
        public bool IsRefillRecommended { get; init; }
        public string AttentionText => IsAttentionRequired ? "Требуется" : IsRefillRecommended ? "Пополнить" : "Норма";
        public string AttentionForeground => IsAttentionRequired ? "#B42318" : IsRefillRecommended ? "#B45309" : "#16A34A";
        public string RowBackground => IsAttentionRequired ? "#FFF8F8" : IsRefillRecommended ? "#FFFDF5" : "#FFFFFF";
    }

    public partial class MonitorTaViewModel : ViewModelBase
    {
        private readonly IMonitoringService _monitoringService;
        private readonly LookupItem _allStatusesItem = new() { Id = 0, Name = "Все статусы" };
        private CancellationTokenSource? _autoFilterDelayTokenSource;
        private bool _suppressAutoFiltering;
        private int _reloadRequestVersion;

        private static readonly TimeSpan AutoFilterDelay = TimeSpan.FromMilliseconds(250);

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private LookupItem? selectedStatus;

        [ObservableProperty]
        private MonitoringSortOption? selectedSort;

        [ObservableProperty]
        private MonitoringMachineRowViewModel? selectedMachine;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? actionMessage;

        [ObservableProperty]
        private string recordsCounterText = "0";

        [ObservableProperty]
        private string lastUpdatedText = "-";

        [ObservableProperty]
        private int totalMachines;

        [ObservableProperty]
        private int workingMachines;

        [ObservableProperty]
        private int notWorkingMachines;

        [ObservableProperty]
        private int attentionRequiredMachines;

        [ObservableProperty]
        private int totalProducts;

        [ObservableProperty]
        private int lowStockProducts;

        [ObservableProperty]
        private decimal totalIncome;

        public MonitorTaViewModel()
            : this(new DesignMonitoringService())
        {
        }

        public MonitorTaViewModel(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));

            // Основные коллекции фильтров и строк мониторинга.
            Rows = new ObservableCollection<MonitoringMachineRowViewModel>();
            Statuses = new ObservableCollection<LookupItem> { _allStatusesItem };
            SortOptions = new ObservableCollection<MonitoringSortOption>
            {
                new() { Value = "status", Name = "По состоянию ТА" },
                new() { Value = "name_asc", Name = "По названию ТА" },
                new() { Value = "last_sale_desc", Name = "По времени последней продажи" },
                new() { Value = "income_desc", Name = "По доходу (убывание)" },
                new() { Value = "low_stock_desc", Name = "По низким остаткам" },
                new() { Value = "attention", Name = "Сначала требующие внимания" },
            };

            _suppressAutoFiltering = true;
            SelectedStatus = _allStatusesItem;
            SelectedSort = SortOptions[0];
            _suppressAutoFiltering = false;

            _ = LoadAsync();
        }

        public ObservableCollection<MonitoringMachineRowViewModel> Rows { get; }
        public ObservableCollection<LookupItem> Statuses { get; }
        public ObservableCollection<MonitoringSortOption> SortOptions { get; }

        public bool HasRows => Rows.Count > 0;
        public bool HasNoRows => !IsLoading && string.IsNullOrWhiteSpace(ErrorMessage) && Rows.Count == 0;
        public string TotalIncomeText => $"{TotalIncome:N2} ₽";
        public string SelectedMachineHint => SelectedMachine is null
            ? "Автомат не выбран."
            : $"Выбран: {SelectedMachine.MachineName}";

        partial void OnSearchTextChanged(string? value) => TriggerAutoFiltering();
        partial void OnSelectedStatusChanged(LookupItem? value) => TriggerAutoFiltering();
        partial void OnSelectedSortChanged(MonitoringSortOption? value) => TriggerAutoFiltering();

        partial void OnTotalIncomeChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalIncomeText));
        }

        partial void OnSelectedMachineChanged(MonitoringMachineRowViewModel? value)
        {
            OnPropertyChanged(nameof(SelectedMachineHint));
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            // Полная загрузка фильтров + таблицы.
            IsLoading = true;
            ErrorMessage = null;
            ActionMessage = null;
            CancelPendingAutoFiltering();

            try
            {
                var statuses = await _monitoringService.GetStatusesAsync();

                _suppressAutoFiltering = true;
                try
                {
                    Statuses.Clear();
                    Statuses.Add(_allStatusesItem);
                    foreach (var status in statuses)
                    {
                        Statuses.Add(status);
                    }

                    if (SelectedStatus is null || !Statuses.Any(item => item.Id == SelectedStatus.Id))
                    {
                        SelectedStatus = _allStatusesItem;
                    }
                }
                finally
                {
                    _suppressAutoFiltering = false;
                }

                var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);
                await ReloadDashboardAsync(requestVersion);
            }
            catch (Exception ex)
            {
                // На ошибке очищаем состояние, чтобы не показывать частично старые данные.
                ClearState();
                ErrorMessage = $"Не удалось загрузить мониторинг: {ex.Message}";
                OnPropertyChanged(nameof(HasRows));
                OnPropertyChanged(nameof(HasNoRows));
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoRows));
            }
        }

        [RelayCommand]
        private async Task ApplyFilterAsync()
        {
            await ReloadDashboardSafeAsync();
        }

        [RelayCommand]
        private async Task ResetFilterAsync()
        {
            _suppressAutoFiltering = true;
            SearchText = null;
            SelectedStatus = _allStatusesItem;
            SelectedSort = SortOptions[0];
            _suppressAutoFiltering = false;

            ErrorMessage = null;
            ActionMessage = null;

            await ReloadDashboardSafeAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await ReloadDashboardSafeAsync(SelectedMachine?.Id);
        }

        [RelayCommand]
        private async Task ExportCsvAsync()
        {
            try
            {
                // Экспорт всегда повторяет текущий фильтр пользовательского списка.
                var csv = await _monitoringService.ExportCsvAsync(BuildFilter());
                var targetPath = ResolveExportPath();
                await File.WriteAllTextAsync(targetPath, csv, new UTF8Encoding(true));

                ErrorMessage = null;
                ActionMessage = $"CSV экспортирован: {targetPath}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось выполнить экспорт: {ex.Message}";
            }
        }

        private async Task ReloadDashboardSafeAsync(Guid? preferredSelectedId = null)
        {
            var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                await ReloadDashboardAsync(requestVersion, preferredSelectedId);
            }
            catch (Exception ex)
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    ClearState();
                    ErrorMessage = $"Не удалось загрузить мониторинг: {ex.Message}";
                    OnPropertyChanged(nameof(HasRows));
                    OnPropertyChanged(nameof(HasNoRows));
                }
            }
            finally
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    IsLoading = false;
                    OnPropertyChanged(nameof(HasNoRows));
                }
            }
        }

        private async Task ReloadDashboardAsync(
            int requestVersion,
            Guid? preferredSelectedId = null,
            CancellationToken cancellationToken = default)
        {
            // Общая загрузка данных карточек + сводки по мониторингу.
            var dashboard = await _monitoringService.GetDashboardAsync(BuildFilter(), cancellationToken);

            if (requestVersion != _reloadRequestVersion)
            {
                return;
            }

            var rows = dashboard.Machines
                .Select((machine, index) => BuildRow(machine, index + 1))
                .ToList();

            Rows.Clear();
            foreach (var row in rows)
            {
                Rows.Add(row);
            }

            RecordsCounterText = Rows.Count.ToString(CultureInfo.InvariantCulture);

            TotalMachines = dashboard.Summary.TotalMachines;
            WorkingMachines = dashboard.Summary.WorkingMachines;
            NotWorkingMachines = dashboard.Summary.NotWorkingMachines;
            AttentionRequiredMachines = dashboard.Summary.AttentionRequiredMachines;
            TotalProducts = dashboard.Summary.TotalProducts;
            LowStockProducts = dashboard.Summary.LowStockProducts;
            TotalIncome = dashboard.Summary.TotalIncome;
            LastUpdatedText = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

            var selectedId = preferredSelectedId ?? SelectedMachine?.Id;
            SelectedMachine = selectedId.HasValue
                ? Rows.FirstOrDefault(row => row.Id == selectedId.Value)
                : null;

            OnPropertyChanged(nameof(HasRows));
            OnPropertyChanged(nameof(HasNoRows));
        }

        private MonitoringFilterModel BuildFilter()
        {
            // Все команды (поиск, refresh, экспорт) используют единый объект фильтра.
            return new MonitoringFilterModel
            {
                Search = SearchText,
                StatusId = SelectedStatus is { Id: > 0 } ? SelectedStatus.Id : null,
                SortBy = SelectedSort?.Value,
            };
        }

        private static MonitoringMachineRowViewModel BuildRow(MonitoringMachineItem machine, int number)
        {
            // Адаптер из read-model сервиса в поля текущего desktop-дизайна.
            var (statusBg, statusFg) = ResolveStatusBadge(machine.StatusName);
            var (loadOverall, loadMin) = BuildLoadTexts(machine.ProductsCount, machine.LowStockProductsCount);

            return new MonitoringMachineRowViewModel
            {
                Id = machine.Id,
                Number = number,
                MachineName = machine.Name,
                Location = machine.Location,
                ModelDisplayName = machine.ModelDisplayName,
                ConnectionText = ResolveConnection(machine.StatusName),
                StatusText = machine.StatusName,
                StatusBadgeBackground = statusBg,
                StatusBadgeForeground = statusFg,
                LoadOverallText = loadOverall,
                LoadMinText = loadMin,
                MoneyLine1 = $"Доход: {machine.TotalIncome:N0} ₽",
                MoneyLine2 = $"Товаров: {machine.ProductsCount}",
                MoneyLine3 = $"Низкий остаток: {machine.LowStockProductsCount}",
                EventsLine1 = $"Продажа: {BuildTimeAgo(machine.LastSaleDateTime)}",
                EventsLine2 = $"Сервис: {machine.LastServiceDisplay}",
                EquipmentText = machine.IsAttentionRequired
                    ? "Купюропр., QR"
                    : "Купюропр., QR, NFC",
                InformationText = machine.IsAttentionRequired
                    ? "EXE / проверка"
                    : "MDB / онлайн",
                ExtraText = $"{machine.ProductsCount} / {machine.LowStockProductsCount}",
                IsAttentionRequired = machine.IsAttentionRequired,
                IsRefillRecommended = machine.IsRefillRecommended,
            };
        }

        private static (string Overall, string Min) BuildLoadTexts(int productsCount, int lowStockCount)
        {
            if (productsCount <= 0)
            {
                return ("Общая 0%", "Мин. 0%");
            }

            var safeProducts = Math.Max(1, productsCount);
            var overallPercent = (int)Math.Round((double)(safeProducts - lowStockCount) / safeProducts * 100);
            var minPercent = (int)Math.Round((double)lowStockCount / safeProducts * 100);

            overallPercent = Math.Clamp(overallPercent, 0, 100);
            minPercent = Math.Clamp(minPercent, 0, 100);

            return ($"Общая {overallPercent}%", $"Мин. {minPercent}%");
        }

        private static string ResolveConnection(string statusName)
        {
            var normalized = statusName.Trim().ToLowerInvariant();

            if (normalized.Contains("не") && normalized.Contains("рабоч"))
            {
                return "GSM / офлайн";
            }

            if (normalized.Contains("обслуж"))
            {
                return "Ethernet / сервис";
            }

            return "4G / онлайн";
        }

        private static (string Background, string Foreground) ResolveStatusBadge(string statusName)
        {
            var normalized = statusName.Trim().ToLowerInvariant();

            if (normalized.Contains("не") && normalized.Contains("рабоч"))
            {
                return ("#FEE2E2", "#991B1B");
            }

            if (normalized.Contains("обслуж"))
            {
                return ("#DBEAFE", "#1D4ED8");
            }

            return ("#DCFCE7", "#166534");
        }

        private static string BuildTimeAgo(DateTimeOffset? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return "нет данных";
            }

            var delta = DateTimeOffset.Now - dateTime.Value.ToLocalTime();

            if (delta.TotalMinutes < 1)
            {
                return "только что";
            }

            if (delta.TotalMinutes < 60)
            {
                return $"{(int)delta.TotalMinutes} мин. назад";
            }

            if (delta.TotalHours < 24)
            {
                return $"{(int)delta.TotalHours} ч назад";
            }

            if (delta.TotalDays < 2)
            {
                return "вчера";
            }

            return $"{(int)delta.TotalDays} дн назад";
        }

        private static string ResolveExportPath()
        {
            var fileName = $"monitoring-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            var desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            if (!string.IsNullOrWhiteSpace(desktopDirectory) && Directory.Exists(desktopDirectory))
            {
                return Path.Combine(desktopDirectory, fileName);
            }

            return Path.Combine(AppContext.BaseDirectory, fileName);
        }

        private void TriggerAutoFiltering()
        {
            if (_suppressAutoFiltering)
            {
                return;
            }

            // Debounce поиска/фильтров для снижения количества запросов.
            StartAutoFiltering();
        }

        private void StartAutoFiltering()
        {
            _autoFilterDelayTokenSource?.Cancel();
            _autoFilterDelayTokenSource?.Dispose();

            var tokenSource = new CancellationTokenSource();
            _autoFilterDelayTokenSource = tokenSource;
            _ = RunDelayedAutoFilteringAsync(tokenSource);
        }

        private async Task RunDelayedAutoFilteringAsync(CancellationTokenSource tokenSource)
        {
            try
            {
                await Task.Delay(AutoFilterDelay, tokenSource.Token);
                await ReloadDashboardSafeAsync();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(_autoFilterDelayTokenSource, tokenSource))
                {
                    _autoFilterDelayTokenSource = null;
                }

                tokenSource.Dispose();
            }
        }

        private void CancelPendingAutoFiltering()
        {
            _autoFilterDelayTokenSource?.Cancel();
            _autoFilterDelayTokenSource?.Dispose();
            _autoFilterDelayTokenSource = null;
        }

        private void ClearState()
        {
            // Единый reset состояния на случай ошибок и повторной инициализации.
            Rows.Clear();
            SelectedMachine = null;
            RecordsCounterText = "0";

            TotalMachines = 0;
            WorkingMachines = 0;
            NotWorkingMachines = 0;
            AttentionRequiredMachines = 0;
            TotalProducts = 0;
            LowStockProducts = 0;
            TotalIncome = 0m;
            LastUpdatedText = "-";
        }

        private sealed class DesignMonitoringService : IMonitoringService
        {
            public Task<IReadOnlyList<LookupItem>> GetStatusesAsync(CancellationToken cancellationToken = default)
            {
                IReadOnlyList<LookupItem> statuses =
                [
                    new LookupItem { Id = 1, Name = "Рабочий" },
                    new LookupItem { Id = 2, Name = "Не рабочий" },
                    new LookupItem { Id = 3, Name = "На обслуживании" },
                ];

                return Task.FromResult(statuses);
            }

            public Task<MonitoringDashboardModel> GetDashboardAsync(
                MonitoringFilterModel filter,
                CancellationToken cancellationToken = default)
            {
                var machines = new List<MonitoringMachineItem>
                {
                    new()
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Name = "903823 - БЦ «Московский»",
                        Location = "ул. Суворова 121",
                        ModelDisplayName = "Necta Kikko Max",
                        StatusId = 1,
                        StatusName = "Рабочий",
                        InstalledAt = new DateOnly(2024, 1, 10),
                        LastServiceAt = new DateOnly(2026, 3, 15),
                        TotalIncome = 112_460m,
                        ProductsCount = 24,
                        LowStockProductsCount = 3,
                        LastSaleDateTime = DateTimeOffset.Now.AddMinutes(-11),
                        IsAttentionRequired = true,
                    },
                    new()
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Name = "903828 - ГП «Магнит»",
                        Location = "пр. Академический 15",
                        ModelDisplayName = "Bianchi BVM 972",
                        StatusId = 1,
                        StatusName = "Рабочий",
                        InstalledAt = new DateOnly(2023, 11, 4),
                        LastServiceAt = new DateOnly(2026, 3, 17),
                        TotalIncome = 98_300m,
                        ProductsCount = 19,
                        LowStockProductsCount = 1,
                        LastSaleDateTime = DateTimeOffset.Now.AddHours(-3),
                        IsAttentionRequired = false,
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        Name = "903825 - ДОСААФ",
                        Location = "ул. Б. Царская 174",
                        ModelDisplayName = "Bianchi BVM 972",
                        StatusId = 2,
                        StatusName = "Не рабочий",
                        InstalledAt = new DateOnly(2024, 2, 6),
                        LastServiceAt = new DateOnly(2026, 3, 1),
                        TotalIncome = 64_210m,
                        ProductsCount = 31,
                        LowStockProductsCount = 5,
                        LastSaleDateTime = DateTimeOffset.Now.AddDays(-1),
                        IsAttentionRequired = true,
                    },
                };

                var summary = new MonitoringSummaryModel
                {
                    TotalMachines = machines.Count,
                    WorkingMachines = machines.Count(machine => machine.StatusName == "Рабочий"),
                    NotWorkingMachines = machines.Count(machine => machine.StatusName != "Рабочий"),
                    AttentionRequiredMachines = machines.Count(machine => machine.IsAttentionRequired),
                    TotalProducts = machines.Sum(machine => machine.ProductsCount),
                    LowStockProducts = machines.Sum(machine => machine.LowStockProductsCount),
                    TotalIncome = machines.Sum(machine => machine.TotalIncome),
                };

                return Task.FromResult(new MonitoringDashboardModel
                {
                    Machines = machines,
                    Summary = summary,
                });
            }

            public Task<string> ExportCsvAsync(
                MonitoringFilterModel filter,
                CancellationToken cancellationToken = default)
            {
                const string csv =
                    "sep=;\n\"Название автомата\";\"Локация\"\n\"903823 - БЦ «Московский»\";\"ул. Суворова 121\"\n";
                return Task.FromResult(csv);
            }
        }
    }
}
