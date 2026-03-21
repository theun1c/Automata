using Automata.Application.Auth.Models;

namespace Automata.Application.Auth.Services;

/// <summary>
/// Контракт авторизации/регистрации desktop-клиента.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Выполняет вход пользователя по email и паролю.
    /// </summary>
    Task<AuthenticatedUser?> SignInAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Регистрирует нового пользователя в системе.
    /// </summary>
    Task<AuthenticatedUser> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}
