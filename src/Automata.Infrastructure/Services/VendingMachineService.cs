using System.Text.RegularExpressions;
using Automata.Application.Common;
using Automata.Application.Machines.Models;
using Automata.Application.Machines.Services;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Services;

/// <summary>
/// Прикладной сервис управления торговыми автоматами:
/// список, справочники editor-формы, создание и редактирование.
/// </summary>
public sealed class VendingMachineService : IVendingMachineService
{
    private static readonly Regex WorkingHoursRegex = new("^\\d{2}:\\d{2}-\\d{2}:\\d{2}$", RegexOptions.Compiled);
    private readonly DbContextOptions<AutomataDbContext> _dbContextOptions;

    public VendingMachineService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения к БД не задана.", nameof(connectionString));
        }

        _dbContextOptions = new DbContextOptionsBuilder<AutomataDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public VendingMachineService(DbContextOptions<AutomataDbContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
    }

    public async Task<IReadOnlyList<VendingMachineListItem>> GetListAsync(
        string? search,
        int? statusId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        // Read-only query для таблицы автоматов.
        var query = dbContext.VendingMachines
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();

            // Поиск по названию и локации.
            query = query.Where(machine =>
                machine.Name.ToLower().Contains(normalizedSearch) ||
                machine.Location.ToLower().Contains(normalizedSearch));
        }

        if (statusId.HasValue)
        {
            query = query.Where(machine => machine.StatusId == statusId.Value);
        }

        return await query
            .OrderBy(machine => machine.Name)
            .Select(machine => new VendingMachineListItem
            {
                Id = machine.Id,
                Name = machine.Name,
                Location = machine.Location,
                ModelDisplayName = machine.MachineModel.Brand + " " + machine.MachineModel.ModelName,
                StatusName = machine.Status.Name,
                InstalledAt = machine.InstalledAt,
                LastServiceAt = machine.LastServiceAt,
                TotalIncome = machine.TotalIncome,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItem>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

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

    public async Task<VendingMachineEditorLookups> GetEditorLookupsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        // Справочники загружаются отдельными запросами:
        // так проще поддерживать и тестировать editor-форму.
        var machineModels = await dbContext.MachineModels
            .AsNoTracking()
            .OrderBy(model => model.Brand)
            .ThenBy(model => model.ModelName)
            .Select(model => new MachineModelLookupItem
            {
                Id = model.Id,
                Brand = model.Brand,
                ModelName = model.ModelName,
            })
            .ToListAsync(cancellationToken);

        var modems = await dbContext.Modems
            .AsNoTracking()
            .OrderByDescending(modem => modem.IsActive)
            .ThenBy(modem => modem.ModemNumber)
            .Select(modem => new LookupItem
            {
                Id = modem.Id,
                Name = modem.IsActive
                    ? modem.ModemNumber
                    : $"{modem.ModemNumber} (не активен)",
            })
            .ToListAsync(cancellationToken);

        var productMatrices = await dbContext.ProductMatrices
            .AsNoTracking()
            .OrderBy(matrix => matrix.Name)
            .Select(matrix => new LookupItem
            {
                Id = matrix.Id,
                Name = matrix.Name,
            })
            .ToListAsync(cancellationToken);

        var criticalTemplates = await dbContext.CriticalValueTemplates
            .AsNoTracking()
            .OrderBy(template => template.Name)
            .Select(template => new LookupItem
            {
                Id = template.Id,
                Name = template.Name,
            })
            .ToListAsync(cancellationToken);

        var notificationTemplates = await dbContext.NotificationTemplates
            .AsNoTracking()
            .OrderBy(template => template.Name)
            .Select(template => new LookupItem
            {
                Id = template.Id,
                Name = template.Name,
            })
            .ToListAsync(cancellationToken);

        var rawUsers = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .Select(user => new
            {
                Id = user.Id,
                user.LastName,
                user.FirstName,
                user.MiddleName,
                RoleId = user.RoleId,
                RoleName = user.Role.Name,
            })
            .ToListAsync(cancellationToken);

        var users = rawUsers
            .Select(user => new UserLookupItem
            {
                Id = user.Id,
                DisplayName = string.Join(' ', new[] { user.LastName, user.FirstName, user.MiddleName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
                RoleId = user.RoleId,
                RoleName = user.RoleName,
            })
            .ToList();

        return new VendingMachineEditorLookups
        {
            MachineModels = machineModels,
            Modems = modems,
            ProductMatrices = productMatrices,
            CriticalValueTemplates = criticalTemplates,
            NotificationTemplates = notificationTemplates,
            Users = users,
        };
    }

    public async Task<VendingMachineEditModel?> GetMachineForEditAsync(
        Guid machineId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var machine = await dbContext.VendingMachines
            .AsNoTracking()
            .Where(item => item.Id == machineId)
            .Select(item => new VendingMachineEditModel
            {
                Id = item.Id,
                Name = item.Name,
                MachineModelId = item.MachineModelId,
                SlaveMachineModelId = item.SlaveMachineModelId,
                StatusId = item.StatusId,
                InstalledAt = item.InstalledAt,
                LastServiceAt = item.LastServiceAt,
                TotalIncome = item.TotalIncome,
                Address = item.Address,
                Place = item.Place,
                Coordinates = item.Coordinates,
                MachineNumber = item.MachineNumber,
                OperatingMode = item.OperatingMode,
                WorkingHours = item.WorkingHours,
                TimeZone = item.TimeZone,
                Notes = item.Notes,
                KitOnlineCashboxId = item.KitOnlineCashboxId,
                ServicePriority = item.ServicePriority,
                SupportsCoinAcceptor = item.SupportsCoinAcceptor,
                SupportsBillAcceptor = item.SupportsBillAcceptor,
                SupportsCashlessModule = item.SupportsCashlessModule,
                SupportsQrPayments = item.SupportsQrPayments,
                ServiceRfidCards = item.ServiceRfidCards,
                CollectionRfidCards = item.CollectionRfidCards,
                LoadingRfidCards = item.LoadingRfidCards,
                ManagerUserId = item.ManagerUserId,
                EngineerUserId = item.EngineerUserId,
                TechnicianOperatorUserId = item.TechnicianOperatorUserId,
                ClientName = item.ClientName,
                ModemId = item.ModemId,
                ProductMatrixId = item.ProductMatrixId,
                CriticalValueTemplateId = item.CriticalValueTemplateId,
                NotificationTemplateId = item.NotificationTemplateId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return machine;
    }

    public async Task<Guid> CreateMachineAsync(
        VendingMachineEditModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        ValidateMachineModel(model);

        await using var dbContext = CreateDbContext();

        await EnsureReferencesExistAsync(dbContext, model, cancellationToken);
        await EnsureMachineNumberIsUniqueAsync(dbContext, model.MachineNumber, null, cancellationToken);

        // Прямой маппинг ViewModel -> Entity для прозрачного сохранения всех полей формы.
        var entity = new VendingMachine
        {
            Id = Guid.NewGuid(),
            Name = model.Name.Trim(),
            Location = BuildLocation(model.Address, model.Place),
            MachineModelId = model.MachineModelId,
            SlaveMachineModelId = model.SlaveMachineModelId,
            StatusId = model.StatusId > 0 ? model.StatusId : 1,
            InstalledAt = model.InstalledAt,
            LastServiceAt = model.LastServiceAt,
            TotalIncome = model.TotalIncome,
            Address = model.Address.Trim(),
            Place = model.Place.Trim(),
            Coordinates = NormalizeNullable(model.Coordinates),
            MachineNumber = model.MachineNumber.Trim(),
            OperatingMode = model.OperatingMode.Trim(),
            WorkingHours = NormalizeNullable(model.WorkingHours),
            TimeZone = model.TimeZone.Trim(),
            Notes = NormalizeNullable(model.Notes),
            KitOnlineCashboxId = NormalizeNullable(model.KitOnlineCashboxId),
            ServicePriority = model.ServicePriority.Trim(),
            SupportsCoinAcceptor = model.SupportsCoinAcceptor,
            SupportsBillAcceptor = model.SupportsBillAcceptor,
            SupportsCashlessModule = model.SupportsCashlessModule,
            SupportsQrPayments = model.SupportsQrPayments,
            ServiceRfidCards = NormalizeNullable(model.ServiceRfidCards),
            CollectionRfidCards = NormalizeNullable(model.CollectionRfidCards),
            LoadingRfidCards = NormalizeNullable(model.LoadingRfidCards),
            ManagerUserId = model.ManagerUserId,
            EngineerUserId = model.EngineerUserId,
            TechnicianOperatorUserId = model.TechnicianOperatorUserId,
            ClientName = NormalizeNullable(model.ClientName),
            ModemId = model.ModemId,
            ProductMatrixId = model.ProductMatrixId,
            CriticalValueTemplateId = model.CriticalValueTemplateId,
            NotificationTemplateId = model.NotificationTemplateId,
        };

        dbContext.VendingMachines.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateMachineAsync(
        VendingMachineEditModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (!model.Id.HasValue)
        {
            throw new ArgumentException("Для обновления автомата требуется идентификатор.", nameof(model));
        }

        ValidateMachineModel(model);

        await using var dbContext = CreateDbContext();

        var entity = await dbContext.VendingMachines
            .FirstOrDefaultAsync(machine => machine.Id == model.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Торговый автомат не найден.");
        }

        await EnsureReferencesExistAsync(dbContext, model, cancellationToken);
        await EnsureMachineNumberIsUniqueAsync(dbContext, model.MachineNumber, model.Id.Value, cancellationToken);

        // Обновляем только разрешенные поля карточки.
        entity.Name = model.Name.Trim();
        entity.Location = BuildLocation(model.Address, model.Place);
        entity.MachineModelId = model.MachineModelId;
        entity.SlaveMachineModelId = model.SlaveMachineModelId;
        entity.StatusId = model.StatusId > 0 ? model.StatusId : entity.StatusId;
        entity.InstalledAt = model.InstalledAt;
        entity.LastServiceAt = model.LastServiceAt;
        entity.TotalIncome = model.TotalIncome;
        entity.Address = model.Address.Trim();
        entity.Place = model.Place.Trim();
        entity.Coordinates = NormalizeNullable(model.Coordinates);
        entity.MachineNumber = model.MachineNumber.Trim();
        entity.OperatingMode = model.OperatingMode.Trim();
        entity.WorkingHours = NormalizeNullable(model.WorkingHours);
        entity.TimeZone = model.TimeZone.Trim();
        entity.Notes = NormalizeNullable(model.Notes);
        entity.KitOnlineCashboxId = NormalizeNullable(model.KitOnlineCashboxId);
        entity.ServicePriority = model.ServicePriority.Trim();
        entity.SupportsCoinAcceptor = model.SupportsCoinAcceptor;
        entity.SupportsBillAcceptor = model.SupportsBillAcceptor;
        entity.SupportsCashlessModule = model.SupportsCashlessModule;
        entity.SupportsQrPayments = model.SupportsQrPayments;
        entity.ServiceRfidCards = NormalizeNullable(model.ServiceRfidCards);
        entity.CollectionRfidCards = NormalizeNullable(model.CollectionRfidCards);
        entity.LoadingRfidCards = NormalizeNullable(model.LoadingRfidCards);
        entity.ManagerUserId = model.ManagerUserId;
        entity.EngineerUserId = model.EngineerUserId;
        entity.TechnicianOperatorUserId = model.TechnicianOperatorUserId;
        entity.ClientName = NormalizeNullable(model.ClientName);
        entity.ModemId = model.ModemId;
        entity.ProductMatrixId = model.ProductMatrixId;
        entity.CriticalValueTemplateId = model.CriticalValueTemplateId;
        entity.NotificationTemplateId = model.NotificationTemplateId;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMachineAsync(
        Guid machineId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        // Не удаляем автомат, если по нему уже есть история операций.
        var hasSales = await dbContext.Sales
            .AsNoTracking()
            .AnyAsync(sale => sale.MachineId == machineId, cancellationToken);
        if (hasSales)
        {
            throw new InvalidOperationException(
                "Нельзя удалить автомат: есть связанные продажи.");
        }

        var hasMaintenanceRecords = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .AnyAsync(record => record.MachineId == machineId, cancellationToken);
        if (hasMaintenanceRecords)
        {
            throw new InvalidOperationException(
                "Нельзя удалить автомат: есть связанные записи обслуживания.");
        }

        var entity = await dbContext.VendingMachines
            .FirstOrDefaultAsync(machine => machine.Id == machineId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        dbContext.VendingMachines.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateMachineModel(VendingMachineEditModel model)
    {
        // Минимальный набор обязательных проверок перед записью.
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            throw new InvalidOperationException("Название автомата обязательно.");
        }

        if (model.MachineModelId <= 0)
        {
            throw new InvalidOperationException("Укажите модель торгового автомата.");
        }

        if (string.IsNullOrWhiteSpace(model.MachineNumber))
        {
            throw new InvalidOperationException("Номер автомата обязателен.");
        }

        if (string.IsNullOrWhiteSpace(model.Address))
        {
            throw new InvalidOperationException("Адрес обязателен.");
        }

        if (string.IsNullOrWhiteSpace(model.Place))
        {
            throw new InvalidOperationException("Место установки обязательно.");
        }

        if (string.IsNullOrWhiteSpace(model.TimeZone))
        {
            throw new InvalidOperationException("Часовой пояс обязателен.");
        }

        if (string.IsNullOrWhiteSpace(model.ServicePriority))
        {
            throw new InvalidOperationException("Приоритет обслуживания обязателен.");
        }

        if (string.IsNullOrWhiteSpace(model.OperatingMode))
        {
            throw new InvalidOperationException("Режим работы обязателен.");
        }

        if (!model.ModemId.HasValue || model.ModemId.Value <= 0)
        {
            throw new InvalidOperationException("Для автомата нужно выбрать модем.");
        }

        if (!(model.SupportsCoinAcceptor || model.SupportsBillAcceptor || model.SupportsCashlessModule || model.SupportsQrPayments))
        {
            throw new InvalidOperationException("Выберите хотя бы одну платежную систему.");
        }

        if (!string.IsNullOrWhiteSpace(model.WorkingHours))
        {
            var normalized = model.WorkingHours.Trim();

            if (!WorkingHoursRegex.IsMatch(normalized))
            {
                throw new InvalidOperationException("Время работы должно быть в формате HH:mm-HH:mm.");
            }

            var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 ||
                !TimeOnly.TryParse(parts[0], out var from) ||
                !TimeOnly.TryParse(parts[1], out var to) ||
                from >= to)
            {
                throw new InvalidOperationException("Время работы указано некорректно.");
            }
        }
    }

    private static async Task EnsureReferencesExistAsync(
        AutomataDbContext dbContext,
        VendingMachineEditModel model,
        CancellationToken cancellationToken)
    {
        // Централизованно валидируем FK, чтобы не дублировать проверки в Create/Update.
        await EnsureMachineModelExistsAsync(dbContext, model.MachineModelId, "Основная модель автомата не найдена.", cancellationToken);

        if (model.SlaveMachineModelId.HasValue)
        {
            await EnsureMachineModelExistsAsync(dbContext, model.SlaveMachineModelId.Value, "Slave-модель автомата не найдена.", cancellationToken);
        }

        await EnsureStatusExistsAsync(dbContext, model.StatusId > 0 ? model.StatusId : 1, cancellationToken);

        if (model.ManagerUserId.HasValue)
        {
            await EnsureUserExistsAsync(dbContext, model.ManagerUserId.Value, "Выбранный менеджер не найден.", cancellationToken);
        }

        if (model.EngineerUserId.HasValue)
        {
            await EnsureUserExistsAsync(dbContext, model.EngineerUserId.Value, "Выбранный инженер не найден.", cancellationToken);
        }

        if (model.TechnicianOperatorUserId.HasValue)
        {
            await EnsureUserExistsAsync(dbContext, model.TechnicianOperatorUserId.Value, "Выбранный техник-оператор не найден.", cancellationToken);
        }

        await EnsureModemExistsAsync(dbContext, model.ModemId!.Value, cancellationToken);

        if (model.ProductMatrixId.HasValue)
        {
            await EnsureProductMatrixExistsAsync(dbContext, model.ProductMatrixId.Value, cancellationToken);
        }

        if (model.CriticalValueTemplateId.HasValue)
        {
            await EnsureCriticalTemplateExistsAsync(dbContext, model.CriticalValueTemplateId.Value, cancellationToken);
        }

        if (model.NotificationTemplateId.HasValue)
        {
            await EnsureNotificationTemplateExistsAsync(dbContext, model.NotificationTemplateId.Value, cancellationToken);
        }
    }

    private static async Task EnsureMachineModelExistsAsync(
        AutomataDbContext dbContext,
        int machineModelId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.MachineModels
            .AsNoTracking()
            .AnyAsync(model => model.Id == machineModelId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static async Task EnsureStatusExistsAsync(
        AutomataDbContext dbContext,
        int statusId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.MachineStatuses
            .AsNoTracking()
            .AnyAsync(status => status.Id == statusId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Статус автомата не найден.");
        }
    }

    private static async Task EnsureUserExistsAsync(
        AutomataDbContext dbContext,
        Guid userId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static async Task EnsureModemExistsAsync(
        AutomataDbContext dbContext,
        int modemId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Modems
            .AsNoTracking()
            .AnyAsync(modem => modem.Id == modemId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Выбранный модем не найден.");
        }
    }

    private static async Task EnsureProductMatrixExistsAsync(
        AutomataDbContext dbContext,
        int matrixId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ProductMatrices
            .AsNoTracking()
            .AnyAsync(matrix => matrix.Id == matrixId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Выбранная товарная матрица не найдена.");
        }
    }

    private static async Task EnsureCriticalTemplateExistsAsync(
        AutomataDbContext dbContext,
        int templateId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.CriticalValueTemplates
            .AsNoTracking()
            .AnyAsync(template => template.Id == templateId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Выбранный шаблон критических значений не найден.");
        }
    }

    private static async Task EnsureNotificationTemplateExistsAsync(
        AutomataDbContext dbContext,
        int templateId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.NotificationTemplates
            .AsNoTracking()
            .AnyAsync(template => template.Id == templateId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Выбранный шаблон уведомлений не найден.");
        }
    }

    private static async Task EnsureMachineNumberIsUniqueAsync(
        AutomataDbContext dbContext,
        string machineNumber,
        Guid? currentMachineId,
        CancellationToken cancellationToken)
    {
        var normalized = machineNumber.Trim();

        var duplicateExists = await dbContext.VendingMachines
            .AsNoTracking()
            .AnyAsync(machine =>
                    machine.MachineNumber == normalized &&
                    (!currentMachineId.HasValue || machine.Id != currentMachineId.Value),
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException("Автомат с таким номером уже существует.");
        }
    }

    private static string BuildLocation(string address, string place)
    {
        var normalizedAddress = address.Trim();
        var normalizedPlace = place.Trim();

        // Локация для списков/дашбордов формируется из адреса и текстового места установки.
        return string.IsNullOrWhiteSpace(normalizedPlace)
            ? normalizedAddress
            : $"{normalizedAddress} ({normalizedPlace})";
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private AutomataDbContext CreateDbContext()
    {
        return new AutomataDbContext(_dbContextOptions);
    }
}
