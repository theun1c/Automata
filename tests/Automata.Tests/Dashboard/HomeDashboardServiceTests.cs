using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Automata.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Automata.Tests;

public class HomeDashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsAggregatesAndCollections()
    {
        var options = CreateOptions();
        var utcToday = DateTime.UtcNow.Date;
        var utcYesterday = utcToday.AddDays(-1);
        var todayOffset = new DateTimeOffset(utcToday, TimeSpan.Zero);
        var yesterdayOffset = new DateTimeOffset(utcYesterday, TimeSpan.Zero);

        var machine1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var machine2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var machine3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var user1Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var user2Id = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var product1Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var product2Id = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var product3Id = Guid.Parse("88888888-8888-8888-8888-888888888888");

        await using (var db = new AutomataDbContext(options))
        {
            db.Roles.AddRange(
                new Role { Id = 1, Name = "Администратор" },
                new Role { Id = 2, Name = "Инженер" });

            db.Users.AddRange(
                new User
                {
                    Id = user1Id,
                    LastName = "Иванов",
                    FirstName = "Петр",
                    Email = "ivanov@example.com",
                    PasswordHash = "hash",
                    RoleId = 2,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new User
                {
                    Id = user2Id,
                    LastName = "Сидоров",
                    FirstName = "Илья",
                    Email = "sidorov@example.com",
                    PasswordHash = "hash",
                    RoleId = 2,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

            db.MachineStatuses.AddRange(
                new MachineStatus { Id = 1, Name = "Рабочий" },
                new MachineStatus { Id = 2, Name = "Не рабочий" });

            db.MachineModels.Add(new MachineModel { Id = 1, Brand = "Necta", ModelName = "Kikko" });

            db.VendingMachines.AddRange(
                new VendingMachine
                {
                    Id = machine1Id,
                    Name = "ТА-1",
                    Location = "Офис",
                    MachineModelId = 1,
                    StatusId = 1,
                    InstalledAt = new DateOnly(2024, 1, 1),
                    TotalIncome = 1000m,
                },
                new VendingMachine
                {
                    Id = machine2Id,
                    Name = "ТА-2",
                    Location = "Склад",
                    MachineModelId = 1,
                    StatusId = 2,
                    InstalledAt = new DateOnly(2024, 1, 1),
                    TotalIncome = 300m,
                },
                new VendingMachine
                {
                    Id = machine3Id,
                    Name = "ТА-3",
                    Location = "Холл",
                    MachineModelId = 1,
                    StatusId = 1,
                    InstalledAt = new DateOnly(2024, 1, 1),
                    TotalIncome = 2500m,
                });

            db.Products.AddRange(
                new Product
                {
                    Id = product1Id,
                    MachineId = machine1Id,
                    Name = "Вода",
                    Price = 70m,
                    Quantity = 2,
                    MinStock = 5,
                    AvgDailySales = 2m,
                },
                new Product
                {
                    Id = product2Id,
                    MachineId = machine2Id,
                    Name = "Сок",
                    Price = 90m,
                    Quantity = 5,
                    MinStock = 5,
                    AvgDailySales = 2m,
                },
                new Product
                {
                    Id = product3Id,
                    MachineId = machine3Id,
                    Name = "Чипсы",
                    Price = 120m,
                    Quantity = 8,
                    MinStock = 4,
                    AvgDailySales = 2m,
                });

            db.Sales.AddRange(
                new Sale
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999991"),
                    MachineId = machine1Id,
                    ProductId = product1Id,
                    Quantity = 1,
                    SaleAmount = 70m,
                    SaleDatetime = todayOffset.AddHours(10).AddMinutes(30),
                    PaymentMethod = "Карта",
                },
                new Sale
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999992"),
                    MachineId = machine2Id,
                    ProductId = product2Id,
                    Quantity = 2,
                    SaleAmount = 180m,
                    SaleDatetime = todayOffset.AddHours(11).AddMinutes(30),
                    PaymentMethod = "Наличные",
                },
                new Sale
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999993"),
                    MachineId = machine3Id,
                    ProductId = product3Id,
                    Quantity = 3,
                    SaleAmount = 360m,
                    SaleDatetime = todayOffset.AddHours(12).AddMinutes(30),
                    PaymentMethod = "Карта",
                });

            db.MaintenanceRecords.AddRange(
                new MaintenanceRecord
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                    MachineId = machine1Id,
                    UserId = user1Id,
                    ServiceDate = yesterdayOffset.AddHours(15),
                    WorkDescription = "Проверка датчиков",
                },
                new MaintenanceRecord
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                    MachineId = machine2Id,
                    UserId = user2Id,
                    ServiceDate = todayOffset.AddHours(9),
                    WorkDescription = "Замена купюроприемника",
                });

            await db.SaveChangesAsync();
        }

        var service = new HomeDashboardService(options);

        var dashboard = await service.GetDashboardAsync();

        Assert.Equal(3, dashboard.TotalMachines);
        Assert.Equal(2, dashboard.WorkingMachines);
        Assert.Equal(1, dashboard.NotWorkingMachines);
        Assert.Equal(3800m, dashboard.MoneyInMachines);
        Assert.Equal(2, dashboard.LowStockProductsCount);
        Assert.Equal(180m, dashboard.ChangeInMachines);
        Assert.Equal(610m, dashboard.RevenueToday);
        Assert.Equal(0m, dashboard.RevenueYesterday);
        Assert.Equal(180m, dashboard.EncashedToday);
        Assert.Equal(0m, dashboard.EncashedYesterday);
        Assert.Equal(1, dashboard.ServicedMachinesToday);
        Assert.Equal(1, dashboard.ServicedMachinesYesterday);

        Assert.Equal(2, dashboard.MachineStatuses.Count);
        Assert.Contains(dashboard.MachineStatuses, item => item.Name == "Рабочий" && item.Count == 2);
        Assert.Contains(dashboard.MachineStatuses, item => item.Name == "Не рабочий" && item.Count == 1);

        Assert.Equal(10, dashboard.SalesDynamics.Count);
        Assert.Equal(DateOnly.FromDateTime(utcToday), dashboard.SalesDynamics[^1].Day);
        Assert.Equal(610m, dashboard.SalesDynamics[^1].Amount);
        Assert.Equal(6, dashboard.SalesDynamics[^1].Quantity);

        Assert.Equal(3, dashboard.RecentSales.Count);
        Assert.Equal("ТА-3", dashboard.RecentSales[0].MachineName);

        Assert.Equal(2, dashboard.RecentMaintenance.Count);
        Assert.Equal("ТА-2", dashboard.RecentMaintenance[0].MachineName);

        Assert.Equal(3, dashboard.TopMachines.Count);
        Assert.Equal("ТА-3", dashboard.TopMachines[0].MachineName);

        Assert.Equal(2, dashboard.LowStockProducts.Count);
        Assert.Equal("Вода", dashboard.LowStockProducts[0].ProductName);
    }

    private static DbContextOptions<AutomataDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AutomataDbContext>()
            .UseInMemoryDatabase($"home-dashboard-tests-{Guid.NewGuid()}")
            .Options;
    }
}
