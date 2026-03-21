using Automata.Application.Monitoring.Models;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Automata.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Automata.Tests;

public class MonitoringServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsRowsAndSummary()
    {
        var options = CreateOptions();
        var machine1Id = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var machine2Id = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var product1Id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var product2Id = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var product3Id = Guid.Parse("20000000-0000-0000-0000-000000000003");

        await SeedAsync(options, machine1Id, machine2Id, product1Id, product2Id, product3Id);

        var service = new MonitoringService(options);
        var dashboard = await service.GetDashboardAsync(new MonitoringFilterModel());

        Assert.Equal(2, dashboard.Machines.Count);
        Assert.Equal(2, dashboard.Summary.TotalMachines);
        Assert.Equal(1, dashboard.Summary.WorkingMachines);
        Assert.Equal(1, dashboard.Summary.NotWorkingMachines);
        Assert.Equal(0, dashboard.Summary.AttentionRequiredMachines);
        Assert.Equal(3, dashboard.Summary.TotalProducts);
        Assert.Equal(1, dashboard.Summary.LowStockProducts);
        Assert.Equal(1500m, dashboard.Summary.TotalIncome);

        var officeMachine = dashboard.Machines.Single(machine => machine.Id == machine1Id);
        Assert.Equal(2, officeMachine.ProductsCount);
        Assert.Equal(1, officeMachine.LowStockProductsCount);
        Assert.NotNull(officeMachine.LastSaleDateTime);
        Assert.False(officeMachine.IsAttentionRequired);
        Assert.True(officeMachine.IsRefillRecommended);

        var warehouseMachine = dashboard.Machines.Single(machine => machine.Id == machine2Id);
        Assert.Equal(1, warehouseMachine.ProductsCount);
        Assert.Equal(0, warehouseMachine.LowStockProductsCount);
        Assert.Null(warehouseMachine.LastSaleDateTime);
        Assert.False(warehouseMachine.IsAttentionRequired);
        Assert.False(warehouseMachine.IsRefillRecommended);
    }

    [Fact]
    public async Task GetDashboardAsync_AppliesSearchStatusAndSort()
    {
        var options = CreateOptions();
        var machine1Id = Guid.Parse("10000000-0000-0000-0000-000000000011");
        var machine2Id = Guid.Parse("10000000-0000-0000-0000-000000000012");
        var product1Id = Guid.Parse("20000000-0000-0000-0000-000000000011");
        var product2Id = Guid.Parse("20000000-0000-0000-0000-000000000012");
        var product3Id = Guid.Parse("20000000-0000-0000-0000-000000000013");

        await SeedAsync(options, machine1Id, machine2Id, product1Id, product2Id, product3Id);

        var service = new MonitoringService(options);

        var filtered = await service.GetDashboardAsync(new MonitoringFilterModel
        {
            Search = "офис",
            StatusId = 1,
            SortBy = "income_desc",
        });

        Assert.Single(filtered.Machines);
        Assert.Equal(machine1Id, filtered.Machines[0].Id);
    }

    [Fact]
    public async Task ExportCsvAsync_ReturnsCsvContent()
    {
        var options = CreateOptions();
        var machine1Id = Guid.Parse("10000000-0000-0000-0000-000000000021");
        var machine2Id = Guid.Parse("10000000-0000-0000-0000-000000000022");
        var product1Id = Guid.Parse("20000000-0000-0000-0000-000000000021");
        var product2Id = Guid.Parse("20000000-0000-0000-0000-000000000022");
        var product3Id = Guid.Parse("20000000-0000-0000-0000-000000000023");

        await SeedAsync(options, machine1Id, machine2Id, product1Id, product2Id, product3Id);

        var service = new MonitoringService(options);
        var csv = await service.ExportCsvAsync(new MonitoringFilterModel());

        Assert.Contains("Название автомата", csv);
        Assert.Contains("Офис-ТА", csv);
        Assert.Contains("Склад-ТА", csv);
    }

    [Fact]
    public async Task GetStatusesAsync_ReturnsSortedStatuses()
    {
        var options = CreateOptions();

        await using (var db = new AutomataDbContext(options))
        {
            db.MachineStatuses.AddRange(
                new MachineStatus { Id = 2, Name = "Не рабочий" },
                new MachineStatus { Id = 1, Name = "Рабочий" });
            await db.SaveChangesAsync();
        }

        var service = new MonitoringService(options);
        var statuses = await service.GetStatusesAsync();

        Assert.Equal(2, statuses.Count);
        Assert.Equal("Не рабочий", statuses[0].Name);
        Assert.Equal("Рабочий", statuses[1].Name);
    }

    private static async Task SeedAsync(
        DbContextOptions<AutomataDbContext> options,
        Guid machine1Id,
        Guid machine2Id,
        Guid product1Id,
        Guid product2Id,
        Guid product3Id)
    {
        await using var db = new AutomataDbContext(options);

        db.MachineStatuses.AddRange(
            new MachineStatus { Id = 1, Name = "Рабочий" },
            new MachineStatus { Id = 2, Name = "Не рабочий" });

        db.MachineModels.Add(new MachineModel { Id = 1, Brand = "Necta", ModelName = "Kikko Max" });

        db.VendingMachines.AddRange(
            new VendingMachine
            {
                Id = machine1Id,
                Name = "Офис-ТА",
                Location = "Москва, офис",
                MachineModelId = 1,
                StatusId = 1,
                InstalledAt = new DateOnly(2024, 1, 10),
                LastServiceAt = new DateOnly(2026, 3, 15),
                TotalIncome = 1000m,
            },
            new VendingMachine
            {
                Id = machine2Id,
                Name = "Склад-ТА",
                Location = "Москва, склад",
                MachineModelId = 1,
                StatusId = 2,
                InstalledAt = new DateOnly(2024, 2, 5),
                LastServiceAt = new DateOnly(2026, 3, 10),
                TotalIncome = 500m,
            });

        db.Products.AddRange(
            new Product
            {
                Id = product1Id,
                MachineId = machine1Id,
                Name = "Вода",
                Price = 60m,
                Quantity = 2,
                MinStock = 4,
                AvgDailySales = 2m,
            },
            new Product
            {
                Id = product2Id,
                MachineId = machine1Id,
                Name = "Сок",
                Price = 80m,
                Quantity = 7,
                MinStock = 3,
                AvgDailySales = 2m,
            },
            new Product
            {
                Id = product3Id,
                MachineId = machine2Id,
                Name = "Чипсы",
                Price = 100m,
                Quantity = 9,
                MinStock = 2,
                AvgDailySales = 2m,
            });

        db.Sales.Add(
            new Sale
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                MachineId = machine1Id,
                ProductId = product1Id,
                Quantity = 1,
                SaleAmount = 60m,
                SaleDatetime = DateTimeOffset.UtcNow.AddMinutes(-15),
                PaymentMethod = "Карта",
            });

        await db.SaveChangesAsync();
    }

    private static DbContextOptions<AutomataDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AutomataDbContext>()
            .UseInMemoryDatabase($"monitoring-tests-{Guid.NewGuid()}")
            .Options;
    }
}
