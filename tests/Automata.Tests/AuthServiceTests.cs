using Automata.Application.Auth.Models;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Automata.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Automata.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAndSignIn_UsesHashedPasswordAndReturnsRole()
    {
        var options = CreateOptions();

        await using (var db = new AutomataDbContext(options))
        {
            db.Roles.AddRange(
                new Role { Id = 1, Name = "Администратор" },
                new Role { Id = 2, Name = "Оператор" });
            await db.SaveChangesAsync();
        }

        var service = new AuthService(options);

        var registered = await service.RegisterAsync(new RegisterUserRequest
        {
            Email = "new.user@example.com",
            Password = "Passw0rd!",
            FirstName = "Новый",
            LastName = "Пользователь",
        });

        Assert.Equal("Оператор", registered.RoleName);

        var signedIn = await service.SignInAsync("new.user@example.com", "Passw0rd!");
        Assert.NotNull(signedIn);
        Assert.Equal(registered.Id, signedIn!.Id);
        Assert.Equal("Оператор", signedIn.RoleName);

        var wrongPassword = await service.SignInAsync("new.user@example.com", "wrong");
        Assert.Null(wrongPassword);
    }

    [Fact]
    public async Task SignIn_LegacyBcryptHash_UsesConfiguredLegacyPassword()
    {
        var previousLegacyPassword = Environment.GetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD");
        Environment.SetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD", "legacy-pass");

        try
        {
            var options = CreateOptions();
            var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            await using (var db = new AutomataDbContext(options))
            {
                db.Roles.Add(new Role { Id = 1, Name = "Администратор" });
                db.Users.Add(new User
                {
                    Id = userId,
                    Email = "admin@example.com",
                    FirstName = "Админ",
                    LastName = "Тест",
                    PasswordHash = "$2b$12$jS0q5S6d2QpQkF3mWcE.8e1QY7wzjKJ8LQ6s36D5aUZ3eE9x2A8aC",
                    RoleId = 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
                await db.SaveChangesAsync();
            }

            var service = new AuthService(options);

            var shouldFail = await service.SignInAsync("admin@example.com", "wrong");
            Assert.Null(shouldFail);

            var shouldPass = await service.SignInAsync("admin@example.com", "legacy-pass");
            Assert.NotNull(shouldPass);
            Assert.Equal("Администратор", shouldPass!.RoleName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTOMATA_LEGACY_USER_PASSWORD", previousLegacyPassword);
        }
    }

    private static DbContextOptions<AutomataDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AutomataDbContext>()
            .UseInMemoryDatabase($"auth-tests-{Guid.NewGuid()}")
            .Options;
    }
}
