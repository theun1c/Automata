using Automata.Application.Dashboard.Models;
using Automata.Application.Dashboard.Services;
using Automata.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Services;

/// <summary>
/// Сервис чтения данных для главной страницы (dashboard).
/// Возвращает сводные метрики и списки без побочных изменений БД.
/// </summary>
public sealed class HomeDashboardService : IHomeDashboardService
{
    private const int RecentSalesLimit = 10;
    private const int RecentMaintenanceLimit = 10;
    private const int TopMachinesLimit = 8;
    private const int LowStockProductsLimit = 12;

    private readonly DbContextOptions<AutomataDbContext> _dbContextOptions;

    public HomeDashboardService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения к БД не задана.", nameof(connectionString));
        }

        _dbContextOptions = new DbContextOptionsBuilder<AutomataDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public HomeDashboardService(DbContextOptions<AutomataDbContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
    }

    public async Task<HomeDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        // Опорная дата берется из последней продажи, чтобы демо-данные оставались "живыми".
        var latestSaleTimestamp = await dbContext.Sales
            .AsNoTracking()
            .OrderByDescending(sale => sale.SaleDatetime)
            .Select(sale => (DateTimeOffset?)sale.SaleDatetime)
            .FirstOrDefaultAsync(cancellationToken);

        var referenceDate = latestSaleTimestamp?.UtcDateTime.Date ?? DateTime.UtcNow.Date;
        var referenceStart = new DateTimeOffset(referenceDate, TimeSpan.Zero);
        var referenceNextStart = referenceStart.AddDays(1);
        var referencePrevStart = referenceStart.AddDays(-1);

        // KPI по статусам автоматов.
        var machineStatuses = await dbContext.MachineStatuses
            .AsNoTracking()
            .OrderBy(status => status.Name)
            .Select(status => new DashboardMachineStatusItem
            {
                Name = status.Name,
                Count = status.VendingMachines.Count,
            })
            .ToListAsync(cancellationToken);

        var totalMachines = machineStatuses.Sum(item => item.Count);
        var workingMachines = GetStatusCount(machineStatuses, "рабочий");
        var notWorkingMachines = GetStatusCount(machineStatuses, "не рабочий", "нерабочий");

        // Денежные KPI и служебная операционная сводка.
        var moneyInMachines = await dbContext.VendingMachines
            .AsNoTracking()
            .SumAsync(machine => (decimal?)machine.TotalIncome, cancellationToken) ?? 0m;

        var lowStockProductsCount = await dbContext.Products
            .AsNoTracking()
            .CountAsync(product => product.Quantity <= product.MinStock, cancellationToken);

        var changeInMachines = await dbContext.Sales
            .AsNoTracking()
            .Where(sale => sale.PaymentMethod.ToLower().Contains("нал"))
            .SumAsync(sale => (decimal?)sale.SaleAmount, cancellationToken) ?? 0m;

        var revenueToday = await dbContext.Sales
            .AsNoTracking()
            .Where(sale => sale.SaleDatetime >= referenceStart && sale.SaleDatetime < referenceNextStart)
            .SumAsync(sale => (decimal?)sale.SaleAmount, cancellationToken) ?? 0m;

        var revenueYesterday = await dbContext.Sales
            .AsNoTracking()
            .Where(sale => sale.SaleDatetime >= referencePrevStart && sale.SaleDatetime < referenceStart)
            .SumAsync(sale => (decimal?)sale.SaleAmount, cancellationToken) ?? 0m;

        var encashedToday = await dbContext.Sales
            .AsNoTracking()
            .Where(sale =>
                sale.SaleDatetime >= referenceStart &&
                sale.SaleDatetime < referenceNextStart &&
                sale.PaymentMethod.ToLower().Contains("нал"))
            .SumAsync(sale => (decimal?)sale.SaleAmount, cancellationToken) ?? 0m;

        var encashedYesterday = await dbContext.Sales
            .AsNoTracking()
            .Where(sale =>
                sale.SaleDatetime >= referencePrevStart &&
                sale.SaleDatetime < referenceStart &&
                sale.PaymentMethod.ToLower().Contains("нал"))
            .SumAsync(sale => (decimal?)sale.SaleAmount, cancellationToken) ?? 0m;

        var servicedMachinesToday = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .Where(record => record.ServiceDate >= referenceStart && record.ServiceDate < referenceNextStart)
            .Select(record => record.MachineId)
            .Distinct()
            .CountAsync(cancellationToken);

        var servicedMachinesYesterday = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .Where(record => record.ServiceDate >= referencePrevStart && record.ServiceDate < referenceStart)
            .Select(record => record.MachineId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Готовим динамику продаж за 10 дней для нижнего графика dashboard.
        var dynamicsStartDate = referenceDate.AddDays(-9);
        var dynamicsStart = new DateTimeOffset(dynamicsStartDate, TimeSpan.Zero);
        var dynamicsEndExclusive = referenceNextStart;

        var dynamicsByDay = await dbContext.Sales
            .AsNoTracking()
            .Where(sale => sale.SaleDatetime >= dynamicsStart && sale.SaleDatetime < dynamicsEndExclusive)
            .GroupBy(sale => sale.SaleDatetime.Date)
            .Select(group => new
            {
                Day = group.Key,
                Amount = group.Sum(item => item.SaleAmount),
                Quantity = group.Sum(item => item.Quantity),
            })
            .ToListAsync(cancellationToken);

        var dynamicsMap = dynamicsByDay.ToDictionary(item => DateOnly.FromDateTime(item.Day));

        var salesDynamics = Enumerable.Range(0, 10)
            .Select(offset =>
            {
                var day = DateOnly.FromDateTime(dynamicsStartDate.AddDays(offset));
                return dynamicsMap.TryGetValue(day, out var point)
                    ? new DashboardSalesDynamicsItem
                    {
                        Day = day,
                        Amount = point.Amount,
                        Quantity = point.Quantity,
                    }
                    : new DashboardSalesDynamicsItem
                    {
                        Day = day,
                        Amount = 0m,
                        Quantity = 0,
                    };
            })
            .ToList();

        // Последние продажи и обслуживание для оперативной ленты.
        var recentSales = await dbContext.Sales
            .AsNoTracking()
            .OrderByDescending(sale => sale.SaleDatetime)
            .Take(RecentSalesLimit)
            .Select(sale => new RecentSaleItem
            {
                SaleDate = sale.SaleDatetime,
                MachineName = sale.Machine.Name,
                ProductName = sale.Product.Name,
                Quantity = sale.Quantity,
                SaleAmount = sale.SaleAmount,
                PaymentMethod = sale.PaymentMethod,
            })
            .ToListAsync(cancellationToken);

        var maintenanceRows = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .OrderByDescending(record => record.ServiceDate)
            .Take(RecentMaintenanceLimit)
            .Select(record => new
            {
                record.ServiceDate,
                MachineName = record.Machine.Name,
                record.WorkDescription,
                record.User.LastName,
                record.User.FirstName,
                record.User.MiddleName,
            })
            .ToListAsync(cancellationToken);

        var recentMaintenance = maintenanceRows
            .Select(record => new RecentMaintenanceItem
            {
                ServiceDate = record.ServiceDate,
                MachineName = record.MachineName,
                EngineerName = BuildDisplayName(record.LastName, record.FirstName, record.MiddleName),
                WorkDescription = record.WorkDescription,
            })
            .ToList();

        // Топы и проблемные остатки для нижних таблиц.
        var topMachines = await dbContext.VendingMachines
            .AsNoTracking()
            .OrderByDescending(machine => machine.TotalIncome)
            .ThenBy(machine => machine.Name)
            .Take(TopMachinesLimit)
            .Select(machine => new TopMachineItem
            {
                MachineName = machine.Name,
                Location = machine.Location,
                TotalIncome = machine.TotalIncome,
            })
            .ToListAsync(cancellationToken);

        var lowStockProducts = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Quantity <= product.MinStock)
            .OrderBy(product => product.Quantity - product.MinStock)
            .ThenBy(product => product.Name)
            .Take(LowStockProductsLimit)
            .Select(product => new LowStockProductItem
            {
                ProductName = product.Name,
                MachineName = product.Machine.Name,
                Quantity = product.Quantity,
                MinStock = product.MinStock,
            })
            .ToListAsync(cancellationToken);

        return new HomeDashboardModel
        {
            TotalMachines = totalMachines,
            WorkingMachines = workingMachines,
            NotWorkingMachines = notWorkingMachines,
            LowStockProductsCount = lowStockProductsCount,
            MoneyInMachines = moneyInMachines,
            ChangeInMachines = changeInMachines,
            RevenueToday = revenueToday,
            RevenueYesterday = revenueYesterday,
            EncashedToday = encashedToday,
            EncashedYesterday = encashedYesterday,
            ServicedMachinesToday = servicedMachinesToday,
            ServicedMachinesYesterday = servicedMachinesYesterday,
            MachineStatuses = machineStatuses,
            SalesDynamics = salesDynamics,
            RecentSales = recentSales,
            RecentMaintenance = recentMaintenance,
            TopMachines = topMachines,
            LowStockProducts = lowStockProducts,
        };
    }

    /// <summary>
    /// Возвращает суммарное количество машин для переданных вариантов имени статуса.
    /// </summary>
    private static int GetStatusCount(
        IEnumerable<DashboardMachineStatusItem> statuses,
        params string[] variants)
    {
        var normalizedVariants = variants
            .Select(variant => variant.Trim().ToLowerInvariant())
            .ToHashSet();

        return statuses
            .Where(item => normalizedVariants.Contains(item.Name.Trim().ToLowerInvariant()))
            .Sum(item => item.Count);
    }

    /// <summary>
    /// Собирает человекочитаемое ФИО для карточек обслуживания.
    /// </summary>
    private static string BuildDisplayName(string lastName, string firstName, string? middleName)
    {
        var parts = new[]
        {
            lastName,
            firstName,
            middleName,
        };

        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    private AutomataDbContext CreateDbContext()
    {
        return new AutomataDbContext(_dbContextOptions);
    }
}
