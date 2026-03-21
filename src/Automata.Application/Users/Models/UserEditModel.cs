namespace Automata.Application.Users.Models;

/// <summary>
/// DTO формы создания/редактирования пользователя.
/// </summary>
public sealed class UserEditModel
{
    public Guid? Id { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public int RoleId { get; init; }
}
