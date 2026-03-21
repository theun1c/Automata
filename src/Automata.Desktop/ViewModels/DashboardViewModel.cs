using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automata.Application.Dashboard.Models;
using Automata.Application.Dashboard.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// Элемент блока "Состояние сети" на главной.
    /// </summary>
    public sealed class NetworkStatusViewModel
    {
        public required string Title { get; init; }
        public required int Count { get; init; }
        public required string Color { get; init; }
    }

    /// <summary>
    /// Сегмент кольцевой диаграммы для доступности/эффективности сети.
    /// </summary>
    public sealed class NetworkRingSegmentViewModel
    {
        public required string GeometryData { get; init; }
        public required string Color { get; init; }
    }

    /// <summary>
    /// Строка блока сводных показателей на главной.
    /// </summary>
    public sealed class DashboardSummaryRowViewModel
    {
        public required string Title { get; init; }
        public required string Value { get; init; }
    }

    /// <summary>
    /// Точка графика динамики продаж (по сумме или по количеству).
    /// </summary>
    public partial class DashboardSalesChartPointViewModel : ObservableObject
    {
        private readonly decimal _amount;
        private readonly int _quantity;

        [ObservableProperty]
        private string valueText = "0";

        [ObservableProperty]
        private double columnHeight;

        public DashboardSalesChartPointViewModel(DateOnly day, decimal amount, int quantity)
        {
            Day = day;
            DayTitle = GetDayTitle(day.DayOfWeek);
            DateTitle = day.ToString("dd.MM", CultureInfo.InvariantCulture);
            DateForeground = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? "#DC2626"
                : "#6B7280";

            _amount = amount;
            _quantity = quantity;
        }

        public DateOnly Day { get; }
        public string DayTitle { get; }
        public string DateTitle { get; }
        public string DateForeground { get; }

        public decimal Amount => _amount;
        public int Quantity => _quantity;

        public void Update(bool isAmountMode, decimal maxAmount, int maxQuantity)
        {
            if (isAmountMode)
            {
                var ratio = maxAmount <= 0m ? 0d : (double)(_amount / maxAmount);
                ColumnHeight = Math.Round(Math.Clamp(ratio, 0, 1) * 160d, 2);
                ValueText = _amount <= 0m ? "0" : _amount.ToString("N0", CultureInfo.InvariantCulture);
                return;
            }

            var quantityRatio = maxQuantity <= 0 ? 0d : (double)_quantity / maxQuantity;
            ColumnHeight = Math.Round(Math.Clamp(quantityRatio, 0, 1) * 160d, 2);
            ValueText = _quantity.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetDayTitle(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Пн",
                DayOfWeek.Tuesday => "Вт",
                DayOfWeek.Wednesday => "Ср",
                DayOfWeek.Thursday => "Чт",
                DayOfWeek.Friday => "Пт",
                DayOfWeek.Saturday => "Сб",
                DayOfWeek.Sunday => "Вс",
                _ => string.Empty,
            };
        }
    }

    public sealed class DashboardActivityItemViewModel
    {
        public required string DateText { get; init; }
        public required string Title { get; init; }
    }

    /// <summary>
    /// ViewModel главного dashboard-экрана desktop-клиента.
    /// Хранит KPI, ленты последних операций и подготовленные данные для графиков.
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IHomeDashboardService _homeDashboardService;

        [ObservableProperty]
        private int totalMachines;

        [ObservableProperty]
        private int workingMachines;

        [ObservableProperty]
        private int notWorkingMachines;

        [ObservableProperty]
        private decimal totalIncome;

        [ObservableProperty]
        private int lowStockProductsCount;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string lastUpdatedText = "-";

        [ObservableProperty]
        private bool isAmountMode = true;

        [ObservableProperty]
        private string salesDynamicsPeriodText = "-";

        private IReadOnlyList<DashboardSalesDynamicsItem> _salesDynamicsSource = Array.Empty<DashboardSalesDynamicsItem>();

        public DashboardViewModel()
            : this(new DesignHomeDashboardService())
        {
        }

        public DashboardViewModel(IHomeDashboardService homeDashboardService)
        {
            _homeDashboardService = homeDashboardService ?? throw new ArgumentNullException(nameof(homeDashboardService));

            // UI-коллекции экрана: диаграммы, таблицы и ленты.
            NetworkStatuses = new ObservableCollection<NetworkStatusViewModel>();
            NetworkRingSegments = new ObservableCollection<NetworkRingSegmentViewModel>();
            EfficiencyRingSegments = new ObservableCollection<NetworkRingSegmentViewModel>();
            SummaryRows = new ObservableCollection<DashboardSummaryRowViewModel>();
            SalesDynamics = new ObservableCollection<DashboardSalesChartPointViewModel>();
            ActivityFeed = new ObservableCollection<DashboardActivityItemViewModel>();

            RecentSales = new ObservableCollection<RecentSaleItem>();
            RecentMaintenance = new ObservableCollection<RecentMaintenanceItem>();
            TopMachines = new ObservableCollection<TopMachineItem>();
            LowStockProducts = new ObservableCollection<LowStockProductItem>();

            _ = LoadAsync();
        }

        public ObservableCollection<NetworkStatusViewModel> NetworkStatuses { get; }
        public ObservableCollection<NetworkRingSegmentViewModel> NetworkRingSegments { get; }
        public ObservableCollection<NetworkRingSegmentViewModel> EfficiencyRingSegments { get; }
        public ObservableCollection<DashboardSummaryRowViewModel> SummaryRows { get; }
        public ObservableCollection<DashboardSalesChartPointViewModel> SalesDynamics { get; }
        public ObservableCollection<DashboardActivityItemViewModel> ActivityFeed { get; }

        public ObservableCollection<RecentSaleItem> RecentSales { get; }
        public ObservableCollection<RecentMaintenanceItem> RecentMaintenance { get; }
        public ObservableCollection<TopMachineItem> TopMachines { get; }
        public ObservableCollection<LowStockProductItem> LowStockProducts { get; }

        public string TotalMachinesText => TotalMachines.ToString(CultureInfo.InvariantCulture);
        public string WorkingMachinesText => WorkingMachines.ToString(CultureInfo.InvariantCulture);
        public string NotWorkingMachinesText => NotWorkingMachines.ToString(CultureInfo.InvariantCulture);
        public string LowStockProductsText => LowStockProductsCount.ToString(CultureInfo.InvariantCulture);
        public string TotalIncomeText => FormatMoney(TotalIncome);

        public string EfficiencyText => TotalMachines == 0
            ? "0%"
            : $"{Math.Round((double)WorkingMachines / TotalMachines * 100, 1):0.0}%";

        public string NetworkStateHint => $"Работающих автоматов - {EfficiencyText}";
        public string NetworkCenterTitle => "Доступность";
        public string NetworkCenterValue => EfficiencyText;
        public string NetworkRingColor => ResolveAvailabilityColor(TotalMachines, WorkingMachines);

        public string AmountModeButtonBackground => IsAmountMode ? "#1EA7E2" : "#F3F4F6";
        public string AmountModeButtonForeground => IsAmountMode ? "#FFFFFF" : "#374151";
        public string QuantityModeButtonBackground => IsAmountMode ? "#F3F4F6" : "#1EA7E2";
        public string QuantityModeButtonForeground => IsAmountMode ? "#374151" : "#FFFFFF";

        public bool HasActivityFeed => ActivityFeed.Count > 0;
        public string ActivityFeedEmptyText => HasActivityFeed ? string.Empty : "Событий пока нет.";

        partial void OnTotalMachinesChanged(int value)
        {
            OnPropertyChanged(nameof(TotalMachinesText));
            OnPropertyChanged(nameof(EfficiencyText));
            OnPropertyChanged(nameof(NetworkStateHint));
            OnPropertyChanged(nameof(NetworkCenterValue));
            OnPropertyChanged(nameof(NetworkRingColor));
        }

        partial void OnWorkingMachinesChanged(int value)
        {
            OnPropertyChanged(nameof(WorkingMachinesText));
            OnPropertyChanged(nameof(EfficiencyText));
            OnPropertyChanged(nameof(NetworkStateHint));
            OnPropertyChanged(nameof(NetworkCenterValue));
            OnPropertyChanged(nameof(NetworkRingColor));
        }

        partial void OnNotWorkingMachinesChanged(int value)
        {
            OnPropertyChanged(nameof(NotWorkingMachinesText));
        }

        partial void OnLowStockProductsCountChanged(int value)
        {
            OnPropertyChanged(nameof(LowStockProductsText));
        }

        partial void OnTotalIncomeChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalIncomeText));
        }

        partial void OnIsAmountModeChanged(bool value)
        {
            OnPropertyChanged(nameof(AmountModeButtonBackground));
            OnPropertyChanged(nameof(AmountModeButtonForeground));
            OnPropertyChanged(nameof(QuantityModeButtonBackground));
            OnPropertyChanged(nameof(QuantityModeButtonForeground));
            UpdateSalesChart();
        }

        [RelayCommand]
        private void ShowAmount()
        {
            IsAmountMode = true;
        }

        [RelayCommand]
        private void ShowQuantity()
        {
            IsAmountMode = false;
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            // Единая точка полной загрузки главного экрана.
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var dashboard = await _homeDashboardService.GetDashboardAsync();
                ApplyDashboard(dashboard);
                LastUpdatedText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            }
            catch (Exception ex)
            {
                // На ошибке сбрасываем коллекции, чтобы не оставлять частично устаревшие данные.
                ErrorMessage = $"Не удалось загрузить данные главной страницы: {ex.Message}";
                ClearCollections();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadAsync();
        }

        private void ApplyDashboard(HomeDashboardModel dashboard)
        {
            // Верхний блок KPI.
            TotalMachines = dashboard.TotalMachines;
            WorkingMachines = dashboard.WorkingMachines;
            NotWorkingMachines = dashboard.NotWorkingMachines;
            TotalIncome = dashboard.MoneyInMachines;
            LowStockProductsCount = dashboard.LowStockProductsCount;

            NetworkStatuses.Clear();
            foreach (var status in dashboard.MachineStatuses)
            {
                NetworkStatuses.Add(new NetworkStatusViewModel
                {
                    Title = status.Name,
                    Count = status.Count,
                    Color = ResolveStatusColor(status.Name),
                });
            }

            BuildNetworkRingSegments();
            BuildEfficiencyRingSegments();

            // Блок "Сводка" справа от диаграммы.
            SummaryRows.Clear();
            SummaryRows.Add(new DashboardSummaryRowViewModel { Title = "Денег в ТА", Value = FormatMoney(dashboard.MoneyInMachines) });
            SummaryRows.Add(new DashboardSummaryRowViewModel { Title = "Сдача в ТА", Value = FormatMoney(dashboard.ChangeInMachines) });
            SummaryRows.Add(new DashboardSummaryRowViewModel { Title = "Выручка сегодня", Value = FormatMoney(dashboard.RevenueToday) });
            SummaryRows.Add(new DashboardSummaryRowViewModel { Title = "Выручка вчера", Value = FormatMoney(dashboard.RevenueYesterday) });
            SummaryRows.Add(new DashboardSummaryRowViewModel { Title = "Инкассировано сегодня", Value = FormatMoney(dashboard.EncashedToday) });
            SummaryRows.Add(new DashboardSummaryRowViewModel { Title = "Инкассировано вчера", Value = FormatMoney(dashboard.EncashedYesterday) });
            SummaryRows.Add(new DashboardSummaryRowViewModel { Title = "Обслужено ТА сегодня", Value = dashboard.ServicedMachinesToday.ToString(CultureInfo.InvariantCulture) });
            SummaryRows.Add(new DashboardSummaryRowViewModel { Title = "Обслужено ТА вчера", Value = dashboard.ServicedMachinesYesterday.ToString(CultureInfo.InvariantCulture) });

            _salesDynamicsSource = dashboard.SalesDynamics;
            SalesDynamicsPeriodText = BuildSalesPeriodText(_salesDynamicsSource);
            RebuildSalesChartPoints();
            UpdateSalesChart();

            // Нижние таблицы и оперативная лента.
            ReplaceCollection(RecentSales, dashboard.RecentSales);
            ReplaceCollection(RecentMaintenance, dashboard.RecentMaintenance);
            ReplaceCollection(TopMachines, dashboard.TopMachines);
            ReplaceCollection(LowStockProducts, dashboard.LowStockProducts);

            BuildActivityFeed();
        }

        private void ClearCollections()
        {
            // Полный reset для ошибок/первой инициализации.
            TotalMachines = 0;
            WorkingMachines = 0;
            NotWorkingMachines = 0;
            TotalIncome = 0m;
            LowStockProductsCount = 0;

            NetworkStatuses.Clear();
            NetworkRingSegments.Clear();
            EfficiencyRingSegments.Clear();
            SummaryRows.Clear();
            SalesDynamics.Clear();
            ActivityFeed.Clear();
            SalesDynamicsPeriodText = "-";
            _salesDynamicsSource = Array.Empty<DashboardSalesDynamicsItem>();

            RecentSales.Clear();
            RecentMaintenance.Clear();
            TopMachines.Clear();
            LowStockProducts.Clear();

            OnPropertyChanged(nameof(HasActivityFeed));
            OnPropertyChanged(nameof(ActivityFeedEmptyText));
        }

        private void RebuildSalesChartPoints()
        {
            SalesDynamics.Clear();

            foreach (var point in _salesDynamicsSource)
            {
                SalesDynamics.Add(new DashboardSalesChartPointViewModel(point.Day, point.Amount, point.Quantity));
            }
        }

        private void UpdateSalesChart()
        {
            var maxAmount = SalesDynamics.Count == 0 ? 0m : SalesDynamics.Max(point => point.Amount);
            var maxQuantity = SalesDynamics.Count == 0 ? 0 : SalesDynamics.Max(point => point.Quantity);

            // Пересчет высоты столбиков при переключении режима "сумма/количество".
            foreach (var point in SalesDynamics)
            {
                point.Update(IsAmountMode, maxAmount, maxQuantity);
            }
        }

        private void BuildActivityFeed()
        {
            // Оперативная лента формируется из продаж, обслуживания и предупреждений по остаткам.
            var items = new List<(DateTimeOffset Date, string Title)>();

            items.AddRange(RecentSales.Take(6).Select(sale =>
                (sale.SaleDate, $"Продажа: {sale.MachineName}, {sale.ProductName}, {sale.SaleAmount:N0} ₽")));

            items.AddRange(RecentMaintenance.Take(6).Select(record =>
                (record.ServiceDate, $"Обслуживание: {record.MachineName}, {record.EngineerName}")));

            var lowStockPreview = LowStockProducts
                .OrderBy(item => item.Quantity - item.MinStock)
                .Take(4)
                .Select(item => $"Низкий остаток: {item.ProductName} ({item.MachineName}) {item.StockStateText}")
                .ToList();

            var feed = items
                .OrderByDescending(item => item.Date)
                .Take(8)
                .Select(item => new DashboardActivityItemViewModel
                {
                    DateText = item.Date.ToLocalTime().ToString("dd.MM.yy", CultureInfo.InvariantCulture),
                    Title = item.Title,
                })
                .ToList();

            foreach (var lowStock in lowStockPreview)
            {
                feed.Add(new DashboardActivityItemViewModel
                {
                    DateText = "Склад",
                    Title = lowStock,
                });
            }

            ActivityFeed.Clear();
            foreach (var item in feed.Take(12))
            {
                ActivityFeed.Add(item);
            }

            OnPropertyChanged(nameof(HasActivityFeed));
            OnPropertyChanged(nameof(ActivityFeedEmptyText));
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static string BuildSalesPeriodText(IReadOnlyList<DashboardSalesDynamicsItem> dynamics)
        {
            if (dynamics.Count == 0)
            {
                return "Данные по продажам отсутствуют.";
            }

            var from = dynamics[0].Day.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            var to = dynamics[^1].Day.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            return $"Данные по продажам с {from} по {to}";
        }

        private static string ResolveStatusColor(string statusName)
        {
            var normalized = statusName.Trim().ToLowerInvariant();

            if (normalized.Contains("не"))
            {
                return "#DC2626";
            }

            if (normalized.Contains("рабоч"))
            {
                return "#16A34A";
            }

            return "#2563EB";
        }

        private void BuildNetworkRingSegments()
        {
            BuildRingSegments(
                NetworkRingSegments,
                center: 85d,
                radius: 61d,
                startAngle: -90d,
                totalDegrees: 360d,
                gapDegrees: 4d);
        }

        private void BuildEfficiencyRingSegments()
        {
            BuildRingSegments(
                EfficiencyRingSegments,
                center: 90d,
                radius: 62d,
                startAngle: 180d,
                totalDegrees: 180d,
                gapDegrees: 3d);
        }

        private void BuildRingSegments(
            ObservableCollection<NetworkRingSegmentViewModel> target,
            double center,
            double radius,
            double startAngle,
            double totalDegrees,
            double gapDegrees)
        {
            // Генератор сегментированных колец для двух диаграмм (доступность/эффективность).
            target.Clear();

            var segments = NetworkStatuses
                .Where(item => item.Count > 0)
                .ToList();

            var total = segments.Sum(item => item.Count);
            if (total <= 0 || segments.Count == 0)
            {
                return;
            }

            var totalGap = gapDegrees * segments.Count;
            var drawableDegrees = Math.Max(20d, totalDegrees - totalGap);
            var currentAngle = startAngle;

            foreach (var segment in segments)
            {
                var sweep = drawableDegrees * segment.Count / total;
                if (sweep <= 0.5d)
                {
                    currentAngle += sweep + gapDegrees;
                    continue;
                }

                var geometryData = BuildArcPath(center, radius, currentAngle, sweep);
                if (!string.IsNullOrWhiteSpace(geometryData))
                {
                    target.Add(new NetworkRingSegmentViewModel
                    {
                        GeometryData = geometryData,
                        Color = segment.Color,
                    });
                }

                currentAngle += sweep + gapDegrees;
            }
        }

        private static string BuildArcPath(double center, double radius, double startAngle, double sweep)
        {
            var normalizedSweep = Math.Clamp(sweep, 0.01d, 359.9d);
            var endAngle = startAngle + normalizedSweep;

            var start = PointOnCircle(center, radius, startAngle);
            var end = PointOnCircle(center, radius, endAngle);
            var largeArc = normalizedSweep > 180d ? 1 : 0;

            return FormattableString.Invariant(
                $"M {start.X:F3},{start.Y:F3} A {radius:F3},{radius:F3} 0 {largeArc} 1 {end.X:F3},{end.Y:F3}");
        }

        private static (double X, double Y) PointOnCircle(double center, double radius, double angleDegrees)
        {
            var radians = angleDegrees * Math.PI / 180d;
            return
            (
                center + radius * Math.Cos(radians),
                center + radius * Math.Sin(radians)
            );
        }

        private static string ResolveAvailabilityColor(int totalMachines, int workingMachines)
        {
            if (totalMachines <= 0)
            {
                return "#9CA3AF";
            }

            var ratio = (double)workingMachines / totalMachines;

            if (ratio >= 0.9d)
            {
                return "#16A34A";
            }

            if (ratio >= 0.75d)
            {
                return "#EAB308";
            }

            return "#DC2626";
        }

        private static string FormatMoney(decimal amount)
        {
            var formatted = amount.ToString("#,0.##", CultureInfo.InvariantCulture).Replace(',', ' ');
            return $"{formatted} ₽";
        }

        private sealed class DesignHomeDashboardService : IHomeDashboardService
        {
            public Task<HomeDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default)
            {
                var start = DateOnly.FromDateTime(DateTime.Now.Date.AddDays(-9));
                var dynamics = Enumerable.Range(0, 10)
                    .Select(i => new DashboardSalesDynamicsItem
                    {
                        Day = start.AddDays(i),
                        Amount = i < 6 ? 0m : 100_000m + (i * 8_000m),
                        Quantity = i < 6 ? 0 : 600 + i * 45,
                    })
                    .ToList();

                var model = new HomeDashboardModel
                {
                    TotalMachines = 108,
                    WorkingMachines = 96,
                    NotWorkingMachines = 12,
                    LowStockProductsCount = 18,
                    MoneyInMachines = 2_459_000m,
                    ChangeInMachines = 486_300m,
                    RevenueToday = 184_200m,
                    RevenueYesterday = 176_900m,
                    EncashedToday = 81_000m,
                    EncashedYesterday = 73_200m,
                    ServicedMachinesToday = 14,
                    ServicedMachinesYesterday = 11,
                    MachineStatuses =
                    [
                        new DashboardMachineStatusItem { Name = "Рабочий", Count = 96 },
                        new DashboardMachineStatusItem { Name = "Не рабочий", Count = 12 },
                    ],
                    SalesDynamics = dynamics,
                    RecentSales =
                    [
                        new RecentSaleItem
                        {
                            SaleDate = DateTimeOffset.Now.AddMinutes(-30),
                            MachineName = "ТА-001",
                            ProductName = "Вода 0.5",
                            Quantity = 1,
                            SaleAmount = 70m,
                            PaymentMethod = "Карта",
                        },
                    ],
                    RecentMaintenance =
                    [
                        new RecentMaintenanceItem
                        {
                            ServiceDate = DateTimeOffset.Now.AddHours(-2),
                            MachineName = "ТА-010",
                            EngineerName = "Иванов Петр",
                            WorkDescription = "Плановая проверка",
                        },
                    ],
                    TopMachines =
                    [
                        new TopMachineItem
                        {
                            MachineName = "ТА-015",
                            Location = "БЦ Альфа",
                            TotalIncome = 180_000m,
                        },
                    ],
                    LowStockProducts =
                    [
                        new LowStockProductItem
                        {
                            ProductName = "Сок",
                            MachineName = "ТА-010",
                            Quantity = 2,
                            MinStock = 6,
                        },
                    ],
                };

                return Task.FromResult(model);
            }
        }
    }
}
