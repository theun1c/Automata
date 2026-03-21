namespace Automata.Application.Auth.Models;

public sealed class AuthenticatedUser
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
}
