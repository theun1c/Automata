using Automata.Application.Common;
using Automata.Application.Users.Models;

namespace Automata.Application.Users.Services;

/// <summary>
/// Контракт администрирования пользователей.
/// </summary>
public interface IUserAdministrationService
{
    /// <summary>
    /// Возвращает список пользователей с учетом поиска и фильтра роли.
    /// </summary>
    Task<IReadOnlyList<UserAdministrationListItem>> GetUsersAsync(
        string? search,
        int? roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает роли для фильтров и формы редактирования.
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает нового пользователя.
    /// </summary>
    Task<Guid> CreateUserAsync(
        UserEditModel model,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет данные пользователя.
    /// </summary>
    Task UpdateUserAsync(
        UserEditModel model,
        Guid actingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Устанавливает новый пароль пользователю.
    /// </summary>
    Task ChangePasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет пользователя с учетом прикладных ограничений.
    /// </summary>
    Task DeleteUserAsync(
        Guid userId,
        Guid actingUserId,
        CancellationToken cancellationToken = default);
}
