using Automata.Application.Inventory.Models;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Automata.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Automata.Tests;

public class ProductInventoryServiceTests
{
    [Fact]
    public async Task GetProductsAsync_FiltersAndSetsLowStockFlag()
    {
        var options = CreateOptions();

        var machineA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var machineB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        await using (var db = new AutomataDbContext(options))
        {
            db.MachineStatuses.Add(new MachineStatus { Id = 1, Name = "Рабочий" });
            db.MachineModels.Add(new MachineModel { Id = 1, Brand = "Necta", ModelName = "Kikko" });
            db.VendingMachines.AddRange(
                new VendingMachine
                {
                    Id = machineA,
                    Name = "ТА А",
                    Location = "Офис",
                    MachineModelId = 1,
                    StatusId = 1,
                    InstalledAt = new DateOnly(2024, 1, 1),
                    TotalIncome = 0,
                },
                new VendingMachine
                {
                    Id = machineB,
                    Name = "ТА Б",
                    Location = "Склад",
                    MachineModelId = 1,
                    StatusId = 1,
                    InstalledAt = new DateOnly(2024, 1, 1),
                    TotalIncome = 0,
                });

            db.Products.AddRange(
                new Product
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    MachineId = machineA,
                    Name = "Вода 0.5",
                    Description = "Негазированная",
                    Price = 70,
                    Quantity = 3,
                    MinStock = 5,
                    AvgDailySales = 2,
                },
                new Product
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    MachineId = machineB,
                    Name = "Чипсы",
                    Description = "Сыр",
                    Price = 120,
                    Quantity = 20,
                    MinStock = 5,
                    AvgDailySales = 4,
                });

            await db.SaveChangesAsync();
        }

        var service = new ProductInventoryService(options);

        var bySearch = await service.GetProductsAsync("вода", null);
        Assert.Single(bySearch);
        Assert.True(bySearch[0].IsLowStock);

        var byMachine = await service.GetProductsAsync(null, machineB);
        Assert.Single(byMachine);
        Assert.Equal("Чипсы", byMachine[0].Name);
        Assert.False(byMachine[0].IsLowStock);
    }

    [Fact]
    public async Task CrudOperations_WorkAsExpected()
    {
        var options = CreateOptions();
        var machineId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await using (var db = new AutomataDbContext(options))
        {
            db.MachineStatuses.Add(new MachineStatus { Id = 1, Name = "Рабочий" });
            db.MachineModels.Add(new MachineModel { Id = 1, Brand = "Necta", ModelName = "Kikko" });
            db.VendingMachines.Add(new VendingMachine
            {
                Id = machineId,
                Name = "ТА В",
                Location = "Холл",
                MachineModelId = 1,
                StatusId = 1,
                InstalledAt = new DateOnly(2024, 1, 1),
                TotalIncome = 0,
            });
            await db.SaveChangesAsync();
        }

        var service = new ProductInventoryService(options);

        var createdId = await service.CreateProductAsync(new ProductEditModel
        {
            MachineId = machineId,
            Name = "Сок апельсиновый",
            Description = "0.33",
            Price = 95,
            Quantity = 10,
            MinStock = 4,
            AvgDailySales = 3,
        });

        await service.UpdateProductAsync(new ProductEditModel
        {
            Id = createdId,
            MachineId = machineId,
            Name = "Сок апельсиновый premium",
            Description = "0.33",
            Price = 105,
            Quantity = 8,
            MinStock = 4,
            AvgDailySales = 3,
        });

        var productsAfterUpdate = await service.GetProductsAsync("premium", machineId);
        Assert.Single(productsAfterUpdate);
        Assert.Equal(105, productsAfterUpdate[0].Price);

        await service.DeleteProductAsync(createdId);

        var productsAfterDelete = await service.GetProductsAsync(null, machineId);
        Assert.Empty(productsAfterDelete);
    }

    private static DbContextOptions<AutomataDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AutomataDbContext>()
            .UseInMemoryDatabase($"inventory-tests-{Guid.NewGuid()}")
            .Options;
    }
}
