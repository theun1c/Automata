using Automata.Application.Common;
using Automata.Application.Machines.Models;

namespace Automata.Application.Machines.Services;

/// <summary>
/// Контракт управления торговыми автоматами.
/// </summary>
public interface IVendingMachineService
{
    /// <summary>
    /// Возвращает список автоматов с фильтрацией.
    /// </summary>
    Task<IReadOnlyList<VendingMachineListItem>> GetListAsync(
        string? search,
        int? statusId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает справочник статусов автомата.
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает lookup-данные для формы create/edit.
    /// </summary>
    Task<VendingMachineEditorLookups> GetEditorLookupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает карточку автомата для редактирования.
    /// </summary>
    Task<VendingMachineEditModel?> GetMachineForEditAsync(
        Guid machineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает новый торговый автомат.
    /// </summary>
    Task<Guid> CreateMachineAsync(
        VendingMachineEditModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующий торговый автомат.
    /// </summary>
    Task UpdateMachineAsync(
        VendingMachineEditModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет торговый автомат, если нет блокирующих связей.
    /// </summary>
    Task DeleteMachineAsync(
        Guid machineId,
        CancellationToken cancellationToken = default);
}
