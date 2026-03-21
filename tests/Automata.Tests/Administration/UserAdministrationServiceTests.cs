using Automata.Application.Users.Models;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Automata.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Automata.Tests;

public class UserAdministrationServiceTests
{
    [Fact]
    public async Task GetUsersAsync_AppliesSearchAndRoleFilters()
    {
        var options = CreateOptions();
        var adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var engineerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var machineId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await using (var db = new AutomataDbContext(options))
        {
            db.Roles.AddRange(
                new Role { Id = 1, Name = "Администратор" },
                new Role { Id = 2, Name = "Оператор" },
                new Role { Id = 3, Name = "Инженер" });

            db.Users.AddRange(
                new User
                {
                    Id = adminId,
                    LastName = "Иванов",
                    FirstName = "Петр",
                    Email = "admin@example.com",
                    PasswordHash = "pbkdf2-sha256$100000$A$B",
                    RoleId = 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new User
                {
                    Id = engineerId,
                    LastName = "Сидоров",
                    FirstName = "Илья",
                    Email = "engineer@example.com",
                    PasswordHash = "pbkdf2-sha256$100000$A$B",
                    RoleId = 3,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

            db.MachineStatuses.Add(new MachineStatus { Id = 1, Name = "Рабочий" });
            db.MachineModels.Add(new MachineModel { Id = 1, Brand = "Necta", ModelName = "Kikko" });
            db.VendingMachines.Add(new VendingMachine
            {
                Id = machineId,
                Name = "ТА-1",
                Location = "Офис",
                MachineModelId = 1,
                StatusId = 1,
                InstalledAt = new DateOnly(2024, 1, 1),
                TotalIncome = 0,
            });

            db.MaintenanceRecords.Add(new MaintenanceRecord
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                MachineId = machineId,
                UserId = engineerId,
                ServiceDate = DateTimeOffset.UtcNow,
                WorkDescription = "Проверка",
            });

            await db.SaveChangesAsync();
        }

        var service = new UserAdministrationService(options);

        var bySearch = await service.GetUsersAsync("engineer", null);
        Assert.Single(bySearch);
        Assert.Equal(engineerId, bySearch[0].Id);
        Assert.True(bySearch[0].HasMaintenanceRecords);

        var byRole = await service.GetUsersAsync(null, 1);
        Assert.Single(byRole);
        Assert.Equal(adminId, byRole[0].Id);
        Assert.False(byRole[0].HasMaintenanceRecords);
    }

    [Fact]
    public async Task CreateUpdateChangePasswordAndDelete_WorksForAdminScenario()
    {
        var options = CreateOptions();
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await using (var db = new AutomataDbContext(options))
        {
            db.Roles.AddRange(
                new Role { Id = 1, Name = "Администратор" },
                new Role { Id = 2, Name = "Оператор" });

            db.Users.Add(new User
            {
                Id = adminId,
                LastName = "Админов",
                FirstName = "Админ",
                Email = "admin@automata.local",
                PasswordHash = "pbkdf2-sha256$100000$A$B",
                RoleId = 1,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await db.SaveChangesAsync();
        }

        var service = new UserAdministrationService(options);

        var createdId = await service.CreateUserAsync(
            new UserEditModel
            {
                LastName = "Новиков",
                FirstName = "Олег",
                Email = "NEW.USER@EXAMPLE.COM",
                RoleId = 2,
            },
            "Passw0rd!");

        string createdHash;

        await using (var db = new AutomataDbContext(options))
        {
            var createdUser = await db.Users.SingleAsync(user => user.Id == createdId);
            createdHash = createdUser.PasswordHash;
            Assert.Equal("new.user@example.com", createdUser.Email);
            Assert.StartsWith("pbkdf2-sha256$100000$", createdUser.PasswordHash);
        }

        await service.UpdateUserAsync(
            new UserEditModel
            {
                Id = createdId,
                LastName = "Новиков",
                FirstName = "Олег",
                MiddleName = "Петрович",
                Email = "updated.user@example.com",
                Phone = "+7 900 000-00-00",
                RoleId = 1,
            },
            adminId);

        await service.ChangePasswordAsync(createdId, "BetterP4ss!");

        await using (var db = new AutomataDbContext(options))
        {
            var updated = await db.Users.SingleAsync(user => user.Id == createdId);
            Assert.Equal("updated.user@example.com", updated.Email);
            Assert.Equal(1, updated.RoleId);
            Assert.NotEqual(createdHash, updated.PasswordHash);
            Assert.StartsWith("pbkdf2-sha256$100000$", updated.PasswordHash);
        }

        await service.DeleteUserAsync(createdId, adminId);

        await using (var db = new AutomataDbContext(options))
        {
            var exists = await db.Users.AnyAsync(user => user.Id == createdId);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task DeleteUserAsync_ThrowsWhenUserHasMaintenanceRecords()
    {
        var options = CreateOptions();
        var adminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var engineerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var machineId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        await using (var db = new AutomataDbContext(options))
        {
            db.Roles.AddRange(
                new Role { Id = 1, Name = "Администратор" },
                new Role { Id = 3, Name = "Инженер" });

            db.Users.AddRange(
                new User
                {
                    Id = adminId,
                    LastName = "Админов",
                    FirstName = "Админ",
                    Email = "admin2@automata.local",
                    PasswordHash = "pbkdf2-sha256$100000$A$B",
                    RoleId = 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new User
                {
                    Id = engineerId,
                    LastName = "Инженеров",
                    FirstName = "Игорь",
                    Email = "engineer2@automata.local",
                    PasswordHash = "pbkdf2-sha256$100000$A$B",
                    RoleId = 3,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

            db.MachineStatuses.Add(new MachineStatus { Id = 1, Name = "Рабочий" });
            db.MachineModels.Add(new MachineModel { Id = 1, Brand = "Necta", ModelName = "Kikko" });
            db.VendingMachines.Add(new VendingMachine
            {
                Id = machineId,
                Name = "ТА-2",
                Location = "Склад",
                MachineModelId = 1,
                StatusId = 1,
                InstalledAt = new DateOnly(2024, 1, 1),
                TotalIncome = 0,
            });
            db.MaintenanceRecords.Add(new MaintenanceRecord
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                MachineId = machineId,
                UserId = engineerId,
                ServiceDate = DateTimeOffset.UtcNow,
                WorkDescription = "Диагностика",
            });

            await db.SaveChangesAsync();
        }

        var service = new UserAdministrationService(options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteUserAsync(engineerId, adminId));

        Assert.Contains("связанные записи обслуживания", exception.Message);
    }

    [Fact]
    public async Task SelfProtection_RejectsSelfDeleteAndSelfRoleChange()
    {
        var options = CreateOptions();
        var adminId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        await using (var db = new AutomataDbContext(options))
        {
            db.Roles.AddRange(
                new Role { Id = 1, Name = "Администратор" },
                new Role { Id = 2, Name = "Оператор" });

            db.Users.Add(new User
            {
                Id = adminId,
                LastName = "Главный",
                FirstName = "Админ",
                Email = "self.admin@automata.local",
                PasswordHash = "pbkdf2-sha256$100000$A$B",
                RoleId = 1,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await db.SaveChangesAsync();
        }

        var service = new UserAdministrationService(options);

        var roleException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateUserAsync(
                new UserEditModel
                {
                    Id = adminId,
                    LastName = "Главный",
                    FirstName = "Админ",
                    Email = "self.admin@automata.local",
                    RoleId = 2,
                },
                adminId));
        Assert.Contains("собственную роль", roleException.Message);

        var deleteException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteUserAsync(adminId, adminId));
        Assert.Contains("текущего пользователя", deleteException.Message);
    }

    private static DbContextOptions<AutomataDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AutomataDbContext>()
            .UseInMemoryDatabase($"users-admin-tests-{Guid.NewGuid()}")
            .Options;
    }
}
