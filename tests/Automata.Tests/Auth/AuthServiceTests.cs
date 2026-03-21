using Automata.Application.Auth.Models;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Automata.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Automata.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_ValidRequest_CreatesOperatorWithHashedPassword_AndCanSignIn()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        var registered = await service.RegisterAsync(new RegisterUserRequest
        {
            Email = "new.user@example.com",
            Password = "Passw0rd!",
            FirstName = "Новый",
            LastName = "Пользователь",
        });

        Assert.Equal("Оператор", registered.RoleName);

        await using var db = new AutomataDbContext(options);
        var savedUser = await db.Users.AsNoTracking().FirstAsync(user => user.Id == registered.Id);

        Assert.StartsWith("pbkdf2-sha256$", savedUser.PasswordHash, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual("Passw0rd!", savedUser.PasswordHash);

        var signedIn = await service.SignInAsync("new.user@example.com", "Passw0rd!");
        Assert.NotNull(signedIn);
        Assert.Equal(registered.Id, signedIn!.Id);
    }

    [Fact]
    public async Task Register_InvalidEmail_ThrowsInvalidOperationException()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Email = "invalid-email",
            Password = "Passw0rd!",
            FirstName = "Тест",
            LastName = "Тестов",
        }));
    }

    [Fact]
    public async Task Register_ShortPassword_ThrowsInvalidOperationException()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Email = "short@example.com",
            Password = "P1!",
            FirstName = "Тест",
            LastName = "Тестов",
        }));
    }

    [Fact]
    public async Task Register_WeakPasswordWithoutSpecialSymbol_ThrowsInvalidOperationException()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Email = "weak@example.com",
            Password = "Password1",
            FirstName = "Тест",
            LastName = "Тестов",
        }));
    }

    [Fact]
    public async Task Register_MissingFirstName_ThrowsInvalidOperationException()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Email = "no-first@example.com",
            Password = "Passw0rd!",
            FirstName = " ",
            LastName = "Тестов",
        }));
    }

    [Fact]
    public async Task Register_MissingLastName_ThrowsInvalidOperationException()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Email = "no-last@example.com",
            Password = "Passw0rd!",
            FirstName = "Тест",
            LastName = " ",
        }));
    }

    [Fact]
    public async Task Register_WhenOperatorRoleMissing_ThrowsInvalidOperationException()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options, includeAdmin: true, includeOperator: false);

        var service = new AuthService(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Email = "operator-missing@example.com",
            Password = "Passw0rd!",
            FirstName = "Тест",
            LastName = "Тестов",
        }));
    }

    [Fact]
    public async Task Register_DuplicateEmailCaseInsensitive_ThrowsInvalidOperationException()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        await service.RegisterAsync(new RegisterUserRequest
        {
            Email = "DUPLICATE@EXAMPLE.COM",
            Password = "Passw0rd!",
            FirstName = "Первый",
            LastName = "Пользователь",
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Email = "duplicate@example.com",
            Password = "Passw0rd!",
            FirstName = "Второй",
            LastName = "Пользователь",
        }));
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowerCase()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        var registered = await service.RegisterAsync(new RegisterUserRequest
        {
            Email = "MiXeD.CaSe@Example.Com",
            Password = "Passw0rd!",
            FirstName = "Тест",
            LastName = "Тестов",
        });

        await using var db = new AutomataDbContext(options);
        var user = await db.Users.AsNoTracking().FirstAsync(item => item.Id == registered.Id);

        Assert.Equal("mixed.case@example.com", user.Email);
    }

    [Fact]
    public async Task SignIn_UnknownEmail_ReturnsNull()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);
        var result = await service.SignInAsync("unknown@example.com", "Passw0rd!");

        Assert.Null(result);
    }

    [Fact]
    public async Task SignIn_WhitespaceCredentials_ReturnsNull()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        Assert.Null(await service.SignInAsync(" ", "Passw0rd!"));
        Assert.Null(await service.SignInAsync("user@example.com", " "));
    }

    [Fact]
    public async Task SignIn_EmailIsCaseInsensitive_ForPbkdf2Users()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        var service = new AuthService(options);

        await service.RegisterAsync(new RegisterUserRequest
        {
            Email = "operator@example.com",
            Password = "Passw0rd!",
            FirstName = "Оператор",
            LastName = "Тест",
        });

        var signedIn = await service.SignInAsync("OPERATOR@EXAMPLE.COM", "Passw0rd!");
        Assert.NotNull(signedIn);
    }

    [Fact]
    public async Task SignIn_MalformedPbkdf2Hash_ReturnsNull()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        await AuthTestHelpers.AddUserAsync(options, new User
        {
            Id = Guid.NewGuid(),
            Email = "broken.hash@example.com",
            FirstName = "Сломанный",
            LastName = "Хеш",
            PasswordHash = "pbkdf2-sha256$bad$base64$hash",
            RoleId = 1,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var service = new AuthService(options);
        var result = await service.SignInAsync("broken.hash@example.com", "Passw0rd!");

        Assert.Null(result);
    }

    [Fact]
    public async Task SignIn_PlainTextHash_AllowsExactPasswordMatch()
    {
        var options = AuthTestHelpers.CreateInMemoryOptions();
        await AuthTestHelpers.SeedRolesAsync(options);

        await AuthTestHelpers.AddUserAsync(options, new User
        {
            Id = Guid.NewGuid(),
            Email = "plain@example.com",
            FirstName = "Простой",
            LastName = "Пароль",
            PasswordHash = "plain-pass-123!",
            RoleId = 1,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var service = new AuthService(options);

        Assert.Null(await service.SignInAsync("plain@example.com", "wrong"));
        Assert.NotNull(await service.SignInAsync("plain@example.com", "plain-pass-123!"));
    }

    [Fact]
    public async Task SignIn_LegacyBcryptHash_UsesConfiguredLegacyPassword()
    {
        var previousLegacyPassword = Environment.GetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD");
        Environment.SetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD", "legacy-pass");

        try
        {
            var options = AuthTestHelpers.CreateInMemoryOptions();
            await AuthTestHelpers.SeedRolesAsync(options);

            await AuthTestHelpers.AddUserAsync(options, new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                FirstName = "Админ",
                LastName = "Тест",
                PasswordHash = "$2b$12$jS0q5S6d2QpQkF3mWcE.8e1QY7wzjKJ8LQ6s36D5aUZ3eE9x2A8aC",
                RoleId = 1,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            var service = new AuthService(options);

            Assert.Null(await service.SignInAsync("admin@example.com", "wrong"));
            Assert.NotNull(await service.SignInAsync("admin@example.com", "legacy-pass"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD", previousLegacyPassword);
        }
    }

    [Fact]
    public async Task SignIn_LegacyBcryptHash_UsesDefaultPasswordWhenEnvNotSet()
    {
        var previousLegacyPassword = Environment.GetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD");
        Environment.SetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD", null);

        try
        {
            var options = AuthTestHelpers.CreateInMemoryOptions();
            await AuthTestHelpers.SeedRolesAsync(options);

            await AuthTestHelpers.AddUserAsync(options, new User
            {
                Id = Guid.NewGuid(),
                Email = "legacy.default@example.com",
                FirstName = "Легаси",
                LastName = "По умолчанию",
                PasswordHash = "$2b$12$jS0q5S6d2QpQkF3mWcE.8e1QY7wzjKJ8LQ6s36D5aUZ3eE9x2A8aC",
                RoleId = 1,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            var service = new AuthService(options);
            var result = await service.SignInAsync("legacy.default@example.com", "Baklan232!!");

            Assert.NotNull(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD", previousLegacyPassword);
        }
    }
}
