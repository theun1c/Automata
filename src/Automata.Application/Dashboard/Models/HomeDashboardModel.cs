namespace Automata.Application.Dashboard.Models;

/// <summary>
/// Агрегированная модель данных главной страницы.
/// Содержит KPI, динамику и списки для виджетов dashboard.
/// </summary>
public sealed class HomeDashboardModel
{
    // KPI верхнего блока.
    public int TotalMachines { get; init; }
    public int WorkingMachines { get; init; }
    public int NotWorkingMachines { get; init; }
    public int LowStockProductsCount { get; init; }

    // Финансовые и операционные метрики.
    public decimal MoneyInMachines { get; init; }
    public decimal ChangeInMachines { get; init; }
    public decimal RevenueToday { get; init; }
    public decimal RevenueYesterday { get; init; }
    public decimal EncashedToday { get; init; }
    public decimal EncashedYesterday { get; init; }
    public int ServicedMachinesToday { get; init; }
    public int ServicedMachinesYesterday { get; init; }

    // Источники таблиц и графиков.
    public IReadOnlyList<DashboardMachineStatusItem> MachineStatuses { get; init; } = Array.Empty<DashboardMachineStatusItem>();
    public IReadOnlyList<DashboardSalesDynamicsItem> SalesDynamics { get; init; } = Array.Empty<DashboardSalesDynamicsItem>();
    public IReadOnlyList<RecentSaleItem> RecentSales { get; init; } = Array.Empty<RecentSaleItem>();
    public IReadOnlyList<RecentMaintenanceItem> RecentMaintenance { get; init; } = Array.Empty<RecentMaintenanceItem>();
    public IReadOnlyList<TopMachineItem> TopMachines { get; init; } = Array.Empty<TopMachineItem>();
    public IReadOnlyList<LowStockProductItem> LowStockProducts { get; init; } = Array.Empty<LowStockProductItem>();
}
