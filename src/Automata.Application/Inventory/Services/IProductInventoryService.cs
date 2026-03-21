using Automata.Application.Inventory.Models;

namespace Automata.Application.Inventory.Services;

/// <summary>
/// Контракт модуля учета ТМЦ.
/// </summary>
public interface IProductInventoryService
{
    /// <summary>
    /// Возвращает список товаров по фильтрам.
    /// </summary>
    Task<IReadOnlyList<ProductListItem>> GetProductsAsync(
        string? search,
        Guid? machineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает список автоматов для фильтра и editor-формы.
    /// </summary>
    Task<IReadOnlyList<MachineLookupItem>> GetMachinesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает карточку товара.
    /// </summary>
    Task<Guid> CreateProductAsync(ProductEditModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет карточку товара.
    /// </summary>
    Task UpdateProductAsync(ProductEditModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет карточку товара.
    /// </summary>
    Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);
}
