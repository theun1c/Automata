using System.Globalization;
using System.Text;
using Automata.Application.Common;
using Automata.Application.Monitoring.Models;
using Automata.Application.Monitoring.Services;
using Automata.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Services;

/// <summary>
/// Общий read-сервис мониторинга для desktop и web.
/// Возвращает агрегированные данные по автоматам без дублирования логики между UI-слоями.
/// </summary>
public sealed class MonitoringService : IMonitoringService
{
    private readonly DbContextOptions<AutomataDbContext> _dbContextOptions;

    public MonitoringService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения к БД не задана.", nameof(connectionString));
        }

        _dbContextOptions = new DbContextOptionsBuilder<AutomataDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public MonitoringService(DbContextOptions<AutomataDbContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
    }

    public async Task<IReadOnlyList<LookupItem>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        // Справочник статусов используется для фильтра и должен быть стабильным по порядку.
        return await dbContext.MachineStatuses
            .AsNoTracking()
            .OrderBy(status => status.Name)
            .Select(status => new LookupItem
            {
                Id = status.Id,
                Name = status.Name,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<MonitoringDashboardModel> GetDashboardAsync(
        MonitoringFilterModel filter,
        CancellationToken cancellationToken = default)
    {
        filter ??= new MonitoringFilterModel();

        await using var dbContext = CreateDbContext();

        // Базовый query с AsNoTracking, т.к. сценарий полностью read-only.
        var query = dbContext.VendingMachines
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var normalizedSearch = filter.Search.Trim().ToLowerInvariant();

            // Поиск по названию и локации без дополнительных full-text зависимостей.
            query = query.Where(machine =>
                machine.Name.ToLower().Contains(normalizedSearch) ||
                machine.Location.ToLower().Contains(normalizedSearch));
        }

        if (filter.StatusId.HasValue)
        {
            query = query.Where(machine => machine.StatusId == filter.StatusId.Value);
        }

        var machines = await query
            .Select(machine => new MonitoringMachineItem
            {
                Id = machine.Id,
                Name = machine.Name,
                Location = machine.Location,
                ModelDisplayName = machine.MachineModel.Brand + " " + machine.MachineModel.ModelName,
                StatusId = machine.StatusId,
                StatusName = machine.Status.Name,
                InstalledAt = machine.InstalledAt,
                LastServiceAt = machine.LastServiceAt,
                TotalIncome = machine.TotalIncome,
                ProductsCount = machine.Products.Count(),
                LowStockProductsCount = machine.Products.Count(product => product.Quantity <= product.MinStock),
                LastSaleDateTime = machine.Sales.Max(sale => (DateTimeOffset?)sale.SaleDatetime),
                // Автомат требует внимания, когда есть хотя бы одна пустая ячейка.
                IsAttentionRequired = machine.Products.Any(product => product.Quantity == 0),
            })
            .ToListAsync(cancellationToken);

        var sortedMachines = ApplySort(machines, filter.SortBy);
        return new MonitoringDashboardModel
        {
            Machines = sortedMachines,
            Summary = BuildSummary(sortedMachines),
        };
    }

    public async Task<string> ExportCsvAsync(
        MonitoringFilterModel filter,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await GetDashboardAsync(filter, cancellationToken);
        return BuildCsv(dashboard.Machines);
    }

    private static IReadOnlyList<MonitoringMachineItem> ApplySort(
        IReadOnlyList<MonitoringMachineItem> items,
        string? sortBy)
    {
        // Список сортировок поддерживается как по имени, так и по операционным метрикам.
        return (sortBy ?? "name_asc").ToLowerInvariant() switch
        {
            "name_desc" => items.OrderByDescending(item => item.Name).ToList(),
            "status" => items.OrderBy(item => item.StatusName).ThenBy(item => item.Name).ToList(),
            "income_desc" => items.OrderByDescending(item => item.TotalIncome).ThenBy(item => item.Name).ToList(),
            "income_asc" => items.OrderBy(item => item.TotalIncome).ThenBy(item => item.Name).ToList(),
            "low_stock_desc" => items.OrderByDescending(item => item.LowStockProductsCount).ThenBy(item => item.Name).ToList(),
            "low_stock_asc" => items.OrderBy(item => item.LowStockProductsCount).ThenBy(item => item.Name).ToList(),
            "last_sale_desc" => items
                .OrderByDescending(item => item.LastSaleDateTime.HasValue)
                .ThenByDescending(item => item.LastSaleDateTime)
                .ThenBy(item => item.Name)
                .ToList(),
            "last_sale_asc" => items
                .OrderBy(item => item.LastSaleDateTime.HasValue ? 0 : 1)
                .ThenBy(item => item.LastSaleDateTime)
                .ThenBy(item => item.Name)
                .ToList(),
            "attention" => items
                .OrderByDescending(item => item.IsAttentionRequired)
                .ThenByDescending(item => item.LowStockProductsCount)
                .ThenBy(item => item.Name)
                .ToList(),
            _ => items.OrderBy(item => item.Name).ToList(),
        };
    }

    private static MonitoringSummaryModel BuildSummary(IReadOnlyList<MonitoringMachineItem> machines)
    {
        // Рабочий статус вычисляем по названию, т.к. в схеме хранится только справочник статусов.
        var workingMachines = machines.Count(machine => IsWorkingStatus(machine.StatusName));

        return new MonitoringSummaryModel
        {
            TotalMachines = machines.Count,
            WorkingMachines = workingMachines,
            NotWorkingMachines = machines.Count - workingMachines,
            AttentionRequiredMachines = machines.Count(machine => machine.IsAttentionRequired),
            TotalProducts = machines.Sum(machine => machine.ProductsCount),
            LowStockProducts = machines.Sum(machine => machine.LowStockProductsCount),
            TotalIncome = machines.Sum(machine => machine.TotalIncome),
        };
    }

    private static bool IsWorkingStatus(string statusName)
    {
        var normalized = statusName.Trim().ToLowerInvariant();
        return normalized.Contains("рабоч") && !normalized.Contains("не");
    }

    private static string BuildCsv(IReadOnlyList<MonitoringMachineItem> machines)
    {
        var builder = new StringBuilder();
        // "sep=;" нужен для корректного открытия в Excel с разделителем ';' на русской локали.
        builder.AppendLine("sep=;");
        builder.AppendLine(
            "Название автомата;Локация;Модель;Статус;Дата установки;Дата последнего обслуживания;Доход;Количество товаров;Низкий остаток;Последняя продажа;Требует внимания");

        foreach (var machine in machines)
        {
            builder.AppendLine(string.Join(";",
                EscapeCsv(machine.Name),
                EscapeCsv(machine.Location),
                EscapeCsv(machine.ModelDisplayName),
                EscapeCsv(machine.StatusName),
                EscapeCsv(machine.InstalledAt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
                EscapeCsv(machine.LastServiceAt?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-"),
                EscapeCsv(machine.TotalIncome.ToString("N2", CultureInfo.InvariantCulture)),
                EscapeCsv(machine.ProductsCount.ToString(CultureInfo.InvariantCulture)),
                EscapeCsv(machine.LowStockProductsCount.ToString(CultureInfo.InvariantCulture)),
                EscapeCsv(machine.LastSaleDateTime?.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? "-"),
                EscapeCsv(machine.IsAttentionRequired ? "Да" : "Нет")));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private AutomataDbContext CreateDbContext()
    {
        return new AutomataDbContext(_dbContextOptions);
    }
}
