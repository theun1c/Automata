using Automata.Application.Machines.Models;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Automata.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Automata.Tests;

public class VendingMachineServiceTests
{
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

        var service = new VendingMachineService(options);
        var statuses = await service.GetStatusesAsync();

        Assert.Equal(2, statuses.Count);
        Assert.Equal("Не рабочий", statuses[0].Name);
        Assert.Equal("Рабочий", statuses[1].Name);
    }

    [Fact]
    public async Task GetListAsync_FiltersBySearchAndStatus()
    {
        var options = CreateOptions();

        await using (var db = new AutomataDbContext(options))
        {
            db.MachineStatuses.AddRange(
                new MachineStatus { Id = 1, Name = "Рабочий" },
                new MachineStatus { Id = 2, Name = "Не рабочий" });

            db.MachineModels.AddRange(
                new MachineModel { Id = 1, Brand = "Necta", ModelName = "Kikko Max" },
                new MachineModel { Id = 2, Brand = "Bianchi", ModelName = "BVM 972" });

            db.VendingMachines.AddRange(
                CreateMachineEntity(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Офис Газпром",
                    "Москва, Тверская",
                    1,
                    1,
                    1000m,
                    "620531"),
                CreateMachineEntity(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Поликлиника №7",
                    "Москва, Арбат",
                    2,
                    2,
                    2000m,
                    "620532"));

            await db.SaveChangesAsync();
        }

        var service = new VendingMachineService(options);

        var bySearch = await service.GetListAsync("газпром", null);
        Assert.Single(bySearch);
        Assert.Equal("Necta Kikko Max", bySearch[0].ModelDisplayName);

        var byLocation = await service.GetListAsync("арбат", null);
        Assert.Single(byLocation);
        Assert.Equal("Поликлиника №7", byLocation[0].Name);

        var byStatus = await service.GetListAsync(null, 1);
        Assert.Single(byStatus);
        Assert.Equal("Офис Газпром", byStatus[0].Name);
    }

    [Fact]
    public async Task GetEditorLookupsAsync_ReturnsAllLookupCollections()
    {
        var options = CreateOptions();

        await using (var db = new AutomataDbContext(options))
        {
            SeedEditorLookups(db);
            await db.SaveChangesAsync();
        }

        var service = new VendingMachineService(options);

        var lookups = await service.GetEditorLookupsAsync();

        Assert.Equal(2, lookups.MachineModels.Count);
        Assert.Single(lookups.Modems);
        Assert.Single(lookups.ProductMatrices);
        Assert.Single(lookups.CriticalValueTemplates);
        Assert.Single(lookups.NotificationTemplates);
        Assert.Equal(2, lookups.Users.Count);
    }

    [Fact]
    public async Task CreateMachineAsync_SavesMachineWithNewFields()
    {
        var options = CreateOptions();

        await using (var db = new AutomataDbContext(options))
        {
            SeedEditorLookups(db);
            db.MachineStatuses.Add(new MachineStatus { Id = 1, Name = "Рабочий" });
            await db.SaveChangesAsync();
        }

        var service = new VendingMachineService(options);
        var model = CreateValidEditModel();

        var createdId = await service.CreateMachineAsync(model);

        await using var verifyDb = new AutomataDbContext(options);
        var created = await verifyDb.VendingMachines.AsNoTracking().SingleAsync(machine => machine.Id == createdId);

        Assert.Equal("Новый автомат", created.Name);
        Assert.Equal("ул. Пушкина, д. 10", created.Address);
        Assert.Equal("У входа", created.Place);
        Assert.Equal("ул. Пушкина, д. 10 (У входа)", created.Location);
        Assert.Equal("620599", created.MachineNumber);
        Assert.Equal(1, created.ModemId);
        Assert.Equal(1, created.ProductMatrixId);
        Assert.Equal(1, created.CriticalValueTemplateId);
        Assert.Equal(1, created.NotificationTemplateId);
        Assert.True(created.SupportsCoinAcceptor);
    }

    [Fact]
    public async Task CreateMachineAsync_Throws_WhenNoPaymentSystemsSelected()
    {
        var options = CreateOptions();

        await using (var db = new AutomataDbContext(options))
        {
            SeedEditorLookups(db);
            db.MachineStatuses.Add(new MachineStatus { Id = 1, Name = "Рабочий" });
            await db.SaveChangesAsync();
        }

        var service = new VendingMachineService(options);
        var invalidModel = new VendingMachineEditModel
        {
            Id = null,
            Name = "Новый автомат",
            MachineModelId = 1,
            StatusId = 1,
            InstalledAt = new DateOnly(2025, 1, 1),
            Address = "ул. Пушкина, д. 10",
            Place = "У входа",
            MachineNumber = "620599",
            OperatingMode = "Стандартный",
            WorkingHours = "08:00-22:00",
            TimeZone = "UTC +3",
            ServicePriority = "Средний",
            SupportsCoinAcceptor = false,
            SupportsBillAcceptor = false,
            SupportsCashlessModule = false,
            SupportsQrPayments = false,
            ModemId = 1,
            ProductMatrixId = 1,
            CriticalValueTemplateId = 1,
            NotificationTemplateId = 1,
            ManagerUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            EngineerUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateMachineAsync(invalidModel));

        Assert.Contains("платежную систему", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static VendingMachine CreateMachineEntity(
        Guid id,
        string name,
        string location,
        int modelId,
        int statusId,
        decimal totalIncome,
        string machineNumber)
    {
        return new VendingMachine
        {
            Id = id,
            Name = name,
            Location = location,
            MachineModelId = modelId,
            StatusId = statusId,
            InstalledAt = new DateOnly(2024, 1, 10),
            LastServiceAt = new DateOnly(2024, 2, 10),
            TotalIncome = totalIncome,
            Address = location,
            Place = "Тест",
            MachineNumber = machineNumber,
            OperatingMode = "Стандартный",
            TimeZone = "UTC +3",
            ServicePriority = "Средний",
            SupportsCoinAcceptor = true,
            SupportsBillAcceptor = true,
            SupportsCashlessModule = false,
            SupportsQrPayments = false,
        };
    }

    private static void SeedEditorLookups(AutomataDbContext db)
    {
        db.Roles.AddRange(
            new Role { Id = 1, Name = "Администратор" },
            new Role { Id = 3, Name = "Инженер" });

        db.Users.AddRange(
            new User
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                LastName = "Админов",
                FirstName = "Иван",
                Email = "admin@test.local",
                PasswordHash = "hash",
                RoleId = 1,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new User
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                LastName = "Инженеров",
                FirstName = "Петр",
                Email = "engineer@test.local",
                PasswordHash = "hash",
                RoleId = 3,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        db.MachineModels.AddRange(
            new MachineModel { Id = 1, Brand = "Bianchi", ModelName = "BVM 972" },
            new MachineModel { Id = 2, Brand = "Necta", ModelName = "Kikko ES6" });

        db.Modems.Add(new Modem
        {
            Id = 1,
            ModemNumber = "1824100027",
            IsActive = true,
        });

        db.ProductMatrices.Add(new ProductMatrix { Id = 1, Name = "Стандартная" });
        db.CriticalValueTemplates.Add(new CriticalValueTemplate { Id = 1, Name = "Стандартный" });
        db.NotificationTemplates.Add(new NotificationTemplate { Id = 1, Name = "Стандартный" });
    }

    private static VendingMachineEditModel CreateValidEditModel()
    {
        return new VendingMachineEditModel
        {
            Name = "Новый автомат",
            MachineModelId = 1,
            StatusId = 1,
            InstalledAt = new DateOnly(2025, 1, 1),
            Address = "ул. Пушкина, д. 10",
            Place = "У входа",
            MachineNumber = "620599",
            OperatingMode = "Стандартный",
            WorkingHours = "08:00-22:00",
            TimeZone = "UTC +3",
            ServicePriority = "Средний",
            SupportsCoinAcceptor = true,
            SupportsBillAcceptor = true,
            SupportsCashlessModule = false,
            SupportsQrPayments = false,
            ModemId = 1,
            ProductMatrixId = 1,
            CriticalValueTemplateId = 1,
            NotificationTemplateId = 1,
            ManagerUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            EngineerUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        };
    }

    private static DbContextOptions<AutomataDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AutomataDbContext>()
            .UseInMemoryDatabase($"machines-tests-{Guid.NewGuid()}")
            .Options;
    }
}
