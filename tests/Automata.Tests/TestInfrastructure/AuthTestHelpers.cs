using System.Globalization;
using Automata.Application.Auth.Models;
using Automata.Application.Auth.Services;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Automata.Tests;

/// <summary>
/// Общие helper-утилиты для unit-тестов авторизации и регистрации.
/// Позволяют держать тесты компактными и без дублирования setup-кода.
/// </summary>
internal static class AuthTestHelpers
{
    internal static DbContextOptions<AutomataDbContext> CreateInMemoryOptions(string? databaseName = null)
    {
        return new DbContextOptionsBuilder<AutomataDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"auth-tests-{Guid.NewGuid()}")
            .Options;
    }

    internal static async Task SeedRolesAsync(
        DbContextOptions<AutomataDbContext> options,
        bool includeAdmin = true,
        bool includeOperator = true)
    {
        await using var db = new AutomataDbContext(options);

        if (includeAdmin && !await db.Roles.AnyAsync(role => role.Id == 1))
        {
            db.Roles.Add(new Role { Id = 1, Name = "Администратор" });
        }

        if (includeOperator && !await db.Roles.AnyAsync(role => role.Id == 2))
        {
            db.Roles.Add(new Role { Id = 2, Name = "Оператор" });
        }

        await db.SaveChangesAsync();
    }

    internal static async Task AddUserAsync(
        DbContextOptions<AutomataDbContext> options,
        User user)
    {
        await using var db = new AutomataDbContext(options);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    internal static string SolveCaptcha(string challenge)
    {
        var expression = challenge.Replace("= ?", string.Empty, StringComparison.Ordinal).Trim();
        var tokens = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var result = int.Parse(tokens[0], CultureInfo.InvariantCulture);
        for (var index = 1; index < tokens.Length; index += 2)
        {
            var operation = tokens[index];
            var value = int.Parse(tokens[index + 1], CultureInfo.InvariantCulture);

            result = operation switch
            {
                "+" => result + value,
                "-" => result - value,
                _ => throw new InvalidOperationException("Неизвестная операция CAPTCHA."),
            };
        }

        return result.ToString(CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Унифицированный тестовый double для IAuthService.
/// Настраивается delegate-ами под конкретный сценарий теста.
/// </summary>
internal sealed class StubAuthService : IAuthService
{
    private readonly Func<string, string, CancellationToken, Task<AuthenticatedUser?>> _signIn;
    private readonly Func<RegisterUserRequest, CancellationToken, Task<AuthenticatedUser>> _register;

    public StubAuthService(
        Func<string, string, CancellationToken, Task<AuthenticatedUser?>>? signIn = null,
        Func<RegisterUserRequest, CancellationToken, Task<AuthenticatedUser>>? register = null)
    {
        _signIn = signIn ?? ((_, _, _) => Task.FromResult<AuthenticatedUser?>(null));
        _register = register ?? ((request, _) => Task.FromResult(new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            DisplayName = "Тестовый пользователь",
            RoleName = "Оператор",
        }));
    }

    public Task<AuthenticatedUser?> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        return _signIn(email, password, cancellationToken);
    }

    public Task<AuthenticatedUser> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        return _register(request, cancellationToken);
    }
}
