using Automata.Application.Inventory.Models;
using Automata.Application.Inventory.Services;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Services;

public sealed class ProductInventoryService : IProductInventoryService
{
    private readonly DbContextOptions<AutomataDbContext> _dbContextOptions;

    public ProductInventoryService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения к БД не задана.", nameof(connectionString));
        }

        _dbContextOptions = new DbContextOptionsBuilder<AutomataDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public ProductInventoryService(DbContextOptions<AutomataDbContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
    }

    public async Task<IReadOnlyList<ProductListItem>> GetProductsAsync(
        string? search,
        Guid? machineId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var query = dbContext.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();

            query = query.Where(product =>
                product.Name.ToLower().Contains(normalizedSearch) ||
                (product.Description != null && product.Description.ToLower().Contains(normalizedSearch)));
        }

        if (machineId.HasValue)
        {
            query = query.Where(product => product.MachineId == machineId.Value);
        }

        return await query
            .OrderBy(product => product.Name)
            .Select(product => new ProductListItem
            {
                Id = product.Id,
                MachineId = product.MachineId,
                MachineName = product.Machine.Name,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                MinStock = product.MinStock,
                AvgDailySales = product.AvgDailySales,
                IsLowStock = product.Quantity <= product.MinStock,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MachineLookupItem>> GetMachinesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        return await dbContext.VendingMachines
            .AsNoTracking()
            .OrderBy(machine => machine.Name)
            .Select(machine => new MachineLookupItem
            {
                Id = machine.Id,
                Name = machine.Name,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateProductAsync(ProductEditModel model, CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        ValidateProductModel(model);

        await using var dbContext = CreateDbContext();

        await EnsureMachineExistsAsync(dbContext, model.MachineId, cancellationToken);

        var entity = new Product
        {
            Id = Guid.NewGuid(),
            MachineId = model.MachineId,
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            Price = model.Price,
            Quantity = model.Quantity,
            MinStock = model.MinStock,
            AvgDailySales = model.AvgDailySales,
        };

        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateProductAsync(ProductEditModel model, CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (!model.Id.HasValue)
        {
            throw new ArgumentException("Для обновления товара требуется идентификатор.", nameof(model));
        }

        ValidateProductModel(model);

        await using var dbContext = CreateDbContext();

        var entity = await dbContext.Products
            .FirstOrDefaultAsync(product => product.Id == model.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Товар не найден.");
        }

        await EnsureMachineExistsAsync(dbContext, model.MachineId, cancellationToken);

        entity.MachineId = model.MachineId;
        entity.Name = model.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        entity.Price = model.Price;
        entity.Quantity = model.Quantity;
        entity.MinStock = model.MinStock;
        entity.AvgDailySales = model.AvgDailySales;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = await dbContext.Products
            .FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        dbContext.Products.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateProductModel(ProductEditModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            throw new InvalidOperationException("Название товара обязательно.");
        }

        if (model.Price < 0 || model.Quantity < 0 || model.MinStock < 0 || model.AvgDailySales < 0)
        {
            throw new InvalidOperationException("Числовые значения товара не могут быть отрицательными.");
        }
    }

    private static async Task EnsureMachineExistsAsync(
        AutomataDbContext dbContext,
        Guid machineId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.VendingMachines
            .AsNoTracking()
            .AnyAsync(machine => machine.Id == machineId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Выбранный автомат не найден.");
        }
    }

    private AutomataDbContext CreateDbContext()
    {
        return new AutomataDbContext(_dbContextOptions);
    }
}
